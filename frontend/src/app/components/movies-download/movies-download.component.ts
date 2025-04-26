import { NgClass, NgFor, NgIf } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { BehaviorSubject, catchError, debounceTime, distinctUntilChanged, filter, forkJoin, map, of, Subscription, switchMap, tap } from 'rxjs';
import { FormSectionPartComponent } from '../form-section-part/form-section-part.component';
import { FormSectionHeaderComponent } from '../form-section-header/form-section-header.component';
import { ApiService } from '../../services/api/api.service';
import { ActivatedRoute, Router } from '@angular/router';
import { SnackbarService } from '../../services/snackbar/snackbar.service';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { FilesizePipe } from '../../pipes/filesize.pipe';
import { NonBreakingSpacesPipe } from '../../pipes/non-breaking-spaces.pipe';
import { Snack } from '../../services/snackbar/snack';
import { SnackLevel } from '../../services/snackbar/snack-level';

@Component({
  selector: 'app-movies-download',
  imports: [NgClass, NgFor, FormSectionPartComponent, FormSectionHeaderComponent, NgIf, FilesizePipe, ReactiveFormsModule, NonBreakingSpacesPipe],
  templateUrl: './movies-download.component.html',
  styleUrl: './movies-download.component.scss'
})
export class MoviesDownloadComponent implements OnInit, OnDestroy {

  constructor(private api: ApiService, public route: ActivatedRoute, public router: Router, public snackbarService: SnackbarService) { }

  ngOnInit(): void {
    this.imdbId = this.route.snapshot.params['imdbId'];
    this.initializeTorrentSearchControlSub();
    this.initializeSelectedTorrentSub();
    this.initializeTorrentMovieMappingSub();
    forkJoin([this.loadStorageLocations(), this.loadDetails()]).pipe(switchMap(() => this.loadPrefillData())).subscribe(() => this.loading = false)
  }

  formErrors: Array<string> | null = null;

  title!: string;
  imdbId!: string;
  posterUrl!: string|null;
  year!: number;
  plot!: string|null;
  loading = true;
  loadDetails() {
    return this.api.getMovieDetails(this.imdbId!).pipe(
      tap((details) => {
        this.title = details.title;
        this.posterUrl = details.posterUrl;
        this.year = details.year;
        this.plot = details.plot;
      })
    )
  }

  storageLocationsMap!: Map<string, any>;
  storageLocationEntries!: Array<Array<any>>;
  loadStorageLocations() {
    return this.api.getStorageLocations()
      .pipe(
        map((data: any) => {
          return data.map((sl: any) => {
            if (sl.displayType == 'LocalDisk') {
              sl.img = 'folder.svg';
              sl.type = 'Local Directory'
              sl.name = sl.displayName
            }
            sl.selected = false;
            return sl
          })
        }),
        tap(data => {
          this.storageLocationsMap = new Map();
          for (let i = 0; i < data.length; i++) {
            this.storageLocationsMap.set(data[i].id, data[i]);
          }
          this.storageLocationEntries = Array.from(this.storageLocationsMap.entries());
        })
      )
  }

  toggleStorageLocationSelected(id: string) {

  }

  // the results of the torrent search
  torrents = new Array<any>();
  torrentSearchControl = new FormControl();
  torrentSearchLoading: boolean | null = null;
  torrentSearchControlSub!: Subscription;
  initializeTorrentSearchControlSub() {
    this.torrentSearchControlSub = this.torrentSearchControl.valueChanges
      .pipe(
        filter((val) => val != null && val.length > 0),
        debounceTime(1000),
        distinctUntilChanged(),
        tap(() => this.torrentSearchLoading = true),
        switchMap(value => this.api.searchMovieTorrents(value)),
        catchError(() => of([])),
        tap(() => this.torrentSearchLoading = false),
        tap(() => this.selectedTorrent$.next(null)),
        tap(() => this.selectedTorrentFiles = []),
        map(data => data.results),
        map(t => t.map((t: any) => {
          t.source = t.indexerName
          t.used = this.torrentMovieMapping$.value != null && this.torrentMovieMapping$.value.url == t.url
          return t
        })))
      .subscribe((results: any) => {
        this.torrents = results
        // if there's a torrent mapped to a movie, and that torrent didn't show up in the search results, add that torrent to the beginning of the results
        if (this.torrentMovieMapping$.value != null && this.torrents.find(t => t.url == this.torrentMovieMapping$.value!.url) == undefined) {
          this.torrents.unshift({
            url: this.torrentMovieMapping$.value.url,
            title: this.torrentMovieMapping$.value.title,
            source: this.torrentMovieMapping$.value.source,
            seeders: this.torrentMovieMapping$.value.seeders,
            peers: this.torrentMovieMapping$.value.peers,
            used: true
          })
        }
      })
  }
  selectedTorrent$ = new BehaviorSubject<{ url: string, source: string, title: string, seeders: number, peers: number } | null>(null);
  selectedTorrentSub!: Subscription;
  initializeSelectedTorrentSub() {
    this.selectedTorrentSub = this.selectedTorrent$
      .pipe(
        distinctUntilChanged(),
        tap(() => this.selectedTorrentFilesLoading = true),
        switchMap(torrent => (torrent == null || torrent.url.length == 0)
          ? of([])
            .pipe(
              tap(() => this.selectedTorrentFilesLoading = null)
            )
          : this.api.ensureAndInspectTorrent(torrent.url, torrent.source)
            .pipe(
              map(data => data.files),
              tap(() => this.selectedTorrentFilesLoading = false)
            )
        )
      ).subscribe(files => {
        this.selectedTorrentFiles = files
      })
  }
  selectedTorrentFilesLoading: boolean | null = null;
  // The files inside the selected torrent
  selectedTorrentFiles = new Array<any>();
  // The torrent mapped to the movie
  torrentMovieMapping$ = new BehaviorSubject<{ url: string, source: string, title: string, seeders: number, peers: number, path: string } | null>(null);
  torrentMovieMappingSub!: Subscription;
  initializeTorrentMovieMappingSub() {
    this.torrentMovieMappingSub = this.torrentMovieMapping$.subscribe(torrent => {
      for (let i = 0; i < this.torrents.length; i++) {
        this.torrents[i].used = torrent != null && torrent.url == this.torrents[i].url;
      }
    })
  }

  loadPrefillData(): any {
    return this.api.checkMovieExists(this.imdbId)
      .pipe(
        switchMap(exists => exists
          ? this.api.getMovieStorageLocations(this.imdbId)
            .pipe(
              tap(storageLocations => {
                for (let i = 0; i < storageLocations.length; i++) {
                  const storageLocation = storageLocations[i];
                  if (this.storageLocationsMap.has(storageLocation)) {
                    this.storageLocationsMap.get(storageLocation).selected = true
                  }
                }
                this.storageLocationEntries = Array.from(this.storageLocationsMap.entries());
              }),
              switchMap(() => this.api.getMovieCinematicCutTorrent(this.imdbId)),
              map(t => t.torrent),
              tap(torrent => {
                if(torrent == null){
                  return;
                }
                this.torrentSearchLoading = false
                this.torrents.unshift({
                  url: torrent.url,
                  title: torrent.name,
                  source: torrent.source,
                  seeders: -1,
                  peers: -1,
                  used: true
                });
                this.selectedTorrent$.next({
                  url: torrent.url,
                  source: torrent.source,
                  title: torrent.name,
                  seeders: -1,
                  peers: -1
                });
                this.selectedTorrentFilesLoading = false
                for (let i = 0; i < torrent.files.length; i++) {
                  const file = torrent.files[i];
                  this.selectedTorrentFiles.push({
                    id: file.id,
                    path: file.path,
                    bytes: file.bytes
                  })
                  if (file.mapped) {
                    this.torrentMovieMapping$.next({
                      url: torrent.url,
                      source: torrent.source,
                      title: torrent.name,
                      seeders: -1,
                      peers: -1,
                      path: file.path
                    })
                  }
                }
              })
            )
          : of([])
        )
      )
  }

  onDownload() {
    if (this.torrentMovieMapping$.value == null){
      return;
    }
    this.loading = true;
    this.api.checkMovieExists(this.imdbId).pipe(
      switchMap(exists => exists
        ? of([])
        : this.api.addMovie(this.imdbId, this.title, this.plot, this.posterUrl, this.year, true)
      ),
      switchMap(() => this.api.setMovieStorageLocations(this.imdbId, Array.from(this.storageLocationsMap.values()).filter(sl => sl.selected).map(sl => sl.id))),
      switchMap(() => this.api.setMovieCinematicCutTorrent(this.imdbId, this.torrentMovieMapping$.value!.url, this.torrentMovieMapping$.value!.source, this.torrentMovieMapping$.value!.path)),
      tap(() => this.snackbarService.snacks.next(new Snack(SnackLevel.SUCCESS, `${this.title} was added`, null, null))),
      tap(() => this.loading = false),
      tap(() => this.router.navigate(['/']))
    ).subscribe()
  }

  ngOnDestroy(): void {
    this.torrentSearchControlSub.unsubscribe();
    this.selectedTorrentSub.unsubscribe();
    this.torrentMovieMappingSub.unsubscribe();
  }
}

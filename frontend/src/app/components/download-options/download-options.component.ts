import { Component, OnDestroy, OnInit } from '@angular/core';
import { ApiService } from '../../services/api/api.service';
import { ActivatedRoute, Router } from '@angular/router';
import { BehaviorSubject, catchError, debounceTime, distinctUntilChanged, filter, forkJoin, map, Observable, of, Subject, Subscription, switchMap, tap } from 'rxjs';
import { KeyValuePipe, NgClass, NgFor, NgIf } from '@angular/common';
import { FormSectionHeaderComponent } from '../form-section-header/form-section-header.component';
import { FormSectionPartComponent } from '../form-section-part/form-section-part.component';
import { SeasonEpisodeFormatPipe } from '../../pipes/season-episode-format.pipe';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { NonBreakingSpacesPipe } from '../../pipes/non-breaking-spaces.pipe';
import { formatSeasonEpisode } from '../../functions/formatSeasonEpisode';
import { FilesizePipe } from '../../pipes/filesize.pipe';

@Component({
  selector: 'app-download-options',
  imports: [NgIf, NgFor, FormSectionHeaderComponent, FormSectionPartComponent, SeasonEpisodeFormatPipe, NgClass, ReactiveFormsModule, NonBreakingSpacesPipe, KeyValuePipe, FilesizePipe],
  templateUrl: './download-options.component.html',
  styleUrl: './download-options.component.scss'
})
export class DownloadOptionsComponent implements OnInit, OnDestroy {

  formErrors: Array<string> | null = null;

  title!: string;
  mediaId!: string;
  mediaType!: string;
  loading = true;

  storageLocations: Array<any> | null = null;

  downloadButtonClick$ = new Subject();
  downloadButtonClickSub!: Subscription;

  storageLocationToggle$ = new Subject<string>();
  storageLocationToggleSub!: Subscription;

  torrentSearch: ((query: string) => Observable<any>) | null = null;
  torrentSearchControl = new FormControl();
  torrentSearchControlSub!: Subscription;
  torrentSearchLoading: boolean | null = null;
  torrentSearchResults: Array<any> = [];
  usedTorrentsMap = new Map<string, number>();

  selectedTorrentUrl$ = new BehaviorSubject<string | null>(null);
  selectedTorrentUrlSub!: Subscription;
  selectedTorrentFilesLoading: boolean | null = null;
  selectedTorrentFiles: Array<any> | null = null;

  // series specific variables
  seasons: Array<any> | null = null;
  wantedEpisodeSelectionChange$: Subject<{ season: number, episode: number, title: string, e: any }> | null = null;
  wantedEpisodeSelectionChangeSub: Subscription | null = null;
  wantedEpisodesMap: Map<string, boolean> | null = null;
  episodesToTorrentFileMap: Map<string, { url: string, path: string } | null> | null = null;
  episodesToImdbIdMap: Map<string, string> | null = null;
  torrentFileToEpisodeMapping$: Subject<{ path: string, event: any }> | null = null;
  torrentFileToEpisodeMappingSub: Subscription | null = null;

  // movie specific variables
  torrentMappedToMovie: { url: string, path: string } | null = null;
  torrentFileToMovieMapping$: Subject<string> | null = null;
  torrentFileToMovieMappingSub: Subscription | null = null;

  constructor(public api: ApiService, public route: ActivatedRoute, public router: Router) { }

  ngOnInit(): void {
    this.mediaId = this.route.snapshot.params['id'];
    this.setupTorrentSearching();
    this.setupSelectedTorrentFileMapping();
    this.setupDownloadButtonClick();
    this.setupStorageLocationToggle();
    forkJoin([this.loadMediaDetails(), this.loadStorageLocations()]).subscribe(() => this.loading = false)
  }

  loadMediaDetails() {
    return this.api.getMediaType(this.mediaId!).pipe(
      tap(type => this.mediaType = type),
      switchMap(type => {
        if (type == 'SERIES') {
          return this.loadSeriesDetails()
        }
        if (type == 'MOVIE') {
          return this.loadMovieDetails();
        }
        throw new Error();
      })
    )
  }

  loadSeriesDetails() {
    return this.api.getSeriesDetails(this.mediaId!).pipe(
      tap(details => {
        this.title = details.title;
        this.seasons = details.seasons;
        this.episodesToImdbIdMap = new Map();
        for (let i = 0; i < this.seasons!.length; i++) {
          const season = this.seasons![i];
          for (let j = 0; j < season.episodes.length; j++) {
            const episode = season.episodes[j];
            this.episodesToImdbIdMap.set(formatSeasonEpisode(season.number, episode.number, episode.title), episode.imdbId);
          }
        }
        this.wantedEpisodesMap = new Map();
        this.wantedEpisodeSelectionChange$ = new Subject();
        this.wantedEpisodeSelectionChangeSub = this.wantedEpisodeSelectionChange$.subscribe(data => {
          const wanted = data.e.srcElement.checked;
          const formatted = formatSeasonEpisode(data.season, data.episode, data.title);
          const mapWanted = this.wantedEpisodesMap!.has(formatted) && this.wantedEpisodesMap!.get(formatted)
          if (!wanted && mapWanted) {
            this.wantedEpisodesMap!.set(formatted, false);
            this.episodesToTorrentFileMap?.delete(formatted);
          }
          else if (wanted && !mapWanted) {
            this.wantedEpisodesMap!.set(formatted, true);
            this.episodesToTorrentFileMap!.set(formatted, null);
          }
        })
        this.torrentSearch = this.api.searchSeriesTorrents.bind(this.api);
        this.episodesToTorrentFileMap = new Map();
        this.torrentFileToEpisodeMapping$ = new Subject();
        this.torrentFileToEpisodeMappingSub = this.torrentFileToEpisodeMapping$.subscribe(mapping => {
          const episode = (mapping.event.target as HTMLSelectElement).value;
          for (const [episode, torrentFile] of this.episodesToTorrentFileMap!.entries()) {
            if (torrentFile == null) {
              continue;
            }
            if (torrentFile!.url == this.selectedTorrentUrl$.value && torrentFile!.path == mapping.path) {
              this.usedTorrentsMap.set(this.selectedTorrentUrl$.value!, this.usedTorrentsMap.get(this.selectedTorrentUrl$.value!)! - 1)
              this.episodesToTorrentFileMap!.set(episode, null);
              break;
            }
          }
          if (episode.length == 0) {
            return;
          }
          this.episodesToTorrentFileMap!.set(episode, { url: this.selectedTorrentUrl$.value!, path: mapping.path });
          this.usedTorrentsMap.set(this.selectedTorrentUrl$.value!, this.usedTorrentsMap.get(this.selectedTorrentUrl$.value!)! + 1)
        })
      }),
    )
  }

  loadMovieDetails() {
    return this.api.getMovieDetails(this.mediaId!).pipe(
      tap((details) => {
        this.title = details.title
        this.torrentSearch = this.api.searchMovieTorrents.bind(this.api)
        this.torrentFileToMovieMapping$ = new Subject();
        this.torrentFileToMovieMappingSub = this.torrentFileToMovieMapping$.subscribe(path => {
          this.torrentMappedToMovie = { url: this.selectedTorrentUrl$.value!, path: path };
          for (const [url, count] of this.usedTorrentsMap) {
            if (count != 0) {
              this.usedTorrentsMap.set(url, 0)
            }
          }
          this.usedTorrentsMap.set(this.selectedTorrentUrl$.value!, 1);
        });
      })
    )
  }

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
        tap(data => this.storageLocations = data)
      )
  }

  setupTorrentSearching() {
    this.torrentSearchControlSub = this.torrentSearchControl.valueChanges.pipe(
      filter((val) => val != null && val.length > 0),
      debounceTime(1000),
      distinctUntilChanged(),
      tap(() => this.torrentSearchLoading = true),
      switchMap(value => this.torrentSearch!(value)),
      catchError(() => of([])),
      tap(() => this.torrentSearchLoading = false),
      tap(() => this.selectedTorrentUrl$.next(null)))
      .subscribe(data => {
        const newTorrentSearchResults = [];
        const newUsedTorrentsMap = new Map<string, number>();
        for (let i = 0; i < data.results.length; i++) {
          const res = data.results[i];
          newTorrentSearchResults.push(res);
          newUsedTorrentsMap.set(res.url, 0);
        }
        for (let i = 0; i < this.torrentSearchResults.length; i++) {
          const res = this.torrentSearchResults[i]
          if (this.usedTorrentsMap.has(res.url) && this.usedTorrentsMap.get(res.url)! > 0) {
            if (!newUsedTorrentsMap.has(res.url)) {
              newTorrentSearchResults.unshift(res);
            }
            newUsedTorrentsMap.set(res.url, this.usedTorrentsMap.get(res.url)!)
          }
        }
        this.torrentSearchResults = newTorrentSearchResults
        this.usedTorrentsMap = newUsedTorrentsMap
      });
  }

  setupSelectedTorrentFileMapping() {
    this.selectedTorrentUrlSub = this.selectedTorrentUrl$.pipe(
      distinctUntilChanged(),
      tap(() => this.selectedTorrentFilesLoading = true),
      switchMap(url => (url == null || url.length < 5)
        ? of([]).pipe(
          tap(() => this.selectedTorrentFilesLoading = null)
        )
        : this.api.inspectTorrent(url).pipe(
          tap(() => this.selectedTorrentFilesLoading = false)
        )
      ),
    ).subscribe(data => {
      this.selectedTorrentFiles = data
    })
  }

  setupDownloadButtonClick() {
    this.downloadButtonClickSub = this.downloadButtonClick$.subscribe(() => {
      this.formErrors = [];
      const storageLocationNotMapped = this.storageLocations!.findIndex(sl => sl.selected) == -1;
      if (storageLocationNotMapped) {
        this.formErrors.push("no storage locations mapped")
      }
      if (this.mediaType == 'SERIES') {
        const noEpisodesWanted = this.episodesToTorrentFileMap!.size == 0;
        if (noEpisodesWanted) {
          this.formErrors.push("no episodes wanted")
        }
        else {
          let noEpisodesMapped = true;
          for (const torrent of this.episodesToTorrentFileMap!.values()) {
            if (torrent != null) {
              noEpisodesMapped = false;
              break;
            }
          }
          if (noEpisodesMapped) {
            this.formErrors.push("must map at least one episode")
          }
        }
      } else if (this.mediaType == 'MOVIE') {
        const notMapped = this.torrentMappedToMovie == null;
        if (notMapped) {
          this.formErrors.push("no torrent file mapped")
        }
      }
      if (this.formErrors.length == 0) {
        this.loading = true;
        this.api.addWantedMedia(this.mediaId).pipe(
          switchMap(() => {
            if (this.mediaType == 'SERIES') {
              const wantedEpisodes = Array.from(this.wantedEpisodesMap!.entries())
                .filter(([_, wanted]) => wanted)
                .map(([key]) => key)
                .map(v => v.slice(0, v.indexOf(' ')))
              return this.api.setSeriesWantedEpisodes(this.mediaId, wantedEpisodes)
            }
            return of(null);
          }),
          switchMap(() => {
            const storageLocations = this.storageLocations!.filter(sl => sl.selected).map(sl => sl.id);
            return this.api.setWantedMediaStorageLocations(this.mediaId, storageLocations);
          }),
          switchMap(() => {
            if (this.mediaType == 'SERIES') {
              const torrents = new Array<{ torrentUrl: string, torrentSource: string, torrentDisplayName: string, episodes: Array<{ imdbId: string, torrentFilePath: string }> }>;
              for (const [episode, torrentFile] of this.episodesToTorrentFileMap!) {
                if (torrentFile == null) {
                  continue;
                }
                let torrent = torrents.find(t => t.torrentUrl == torrentFile.url)
                if (torrent == undefined) {
                  torrent = {
                    torrentUrl: torrentFile.url,
                    torrentSource: torrentFile.url,
                    torrentDisplayName: torrentFile.url,
                    episodes: new Array<any>()
                  }
                  torrents.push(torrent)
                }
                torrent.episodes.push({
                  imdbId: this.episodesToImdbIdMap!.get(episode)!,
                  torrentFilePath: torrentFile.path
                })
              }
              return this.api.setWantedSeriesEpisodeToTorrentMapping(this.mediaId!, torrents);
            }
            if (this.mediaType == 'MOVIE') {
              return this.api.setWantedMovieTorrentMapping(this.mediaId!, this.torrentMappedToMovie!.url, this.torrentMappedToMovie!.url, this.torrentMappedToMovie!.url, this.torrentMappedToMovie!.path)
            }
            throw new Error()
          }),
          tap(() => {
            this.loading = false;
          })
        ).subscribe(() => {
          this.router.navigate(['/'])
        })
      }
    })
  }

  setupStorageLocationToggle() {
    this.storageLocationToggleSub = this.storageLocationToggle$.subscribe(id => {
      const sl = this.storageLocations!.find(sl => sl.id == id);
      sl.selected = !sl.selected;
    })
  }

  ngOnDestroy(): void {
    this.torrentSearchControlSub.unsubscribe();
    this.selectedTorrentUrlSub.unsubscribe();
    this.downloadButtonClickSub.unsubscribe();
    this.storageLocationToggleSub.unsubscribe();
    this.wantedEpisodeSelectionChangeSub?.unsubscribe();
    this.torrentFileToEpisodeMappingSub?.unsubscribe();
    this.torrentFileToMovieMappingSub?.unsubscribe();
  }
}

import { Component, OnDestroy, OnInit } from '@angular/core';
import { BehaviorSubject, catchError, debounceTime, distinctUntilChanged, filter, forkJoin, map, of, Subject, Subscription, switchMap, tap } from 'rxjs';
import { ApiService } from '../../services/api/api.service';
import { FilesizePipe } from '../../pipes/filesize.pipe';
import { NgClass, NgFor, NgIf } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { NonBreakingSpacesPipe } from '../../pipes/non-breaking-spaces.pipe';
import { FormSectionHeaderComponent } from '../form-section-header/form-section-header.component';
import { FormSectionPartComponent } from '../form-section-part/form-section-part.component';
import { SeasonEpisodeFormatPipe } from '../../pipes/season-episode-format.pipe';
import { ActivatedRoute, Router } from '@angular/router';
import { SnackbarService } from '../../services/snackbar/snackbar.service';

@Component({
  selector: 'app-tv-download',
  imports: [FilesizePipe, NgIf, NgFor, NonBreakingSpacesPipe, ReactiveFormsModule, FormSectionHeaderComponent, FormSectionPartComponent, NgClass, SeasonEpisodeFormatPipe],
  templateUrl: './tv-download.component.html',
  styleUrl: './tv-download.component.scss'
})
export class TvDownloadComponent implements OnInit, OnDestroy {

  constructor(public api: ApiService, public route: ActivatedRoute, public router: Router, public snackbarService: SnackbarService) { }

  ngOnInit(): void {
    this.imdbId = this.route.snapshot.params['imdbId'];
    this.initializeTorrentSearchControlSub();
    this.initializeSelectedTorrentSub();
    this.initializeTorrentFileToEpisodeMappingSub();
    forkJoin([this.loadStorageLocations(), this.loadDetails()]).pipe(switchMap(() => this.loadPrefillData())).subscribe(() => this.loading = false)
  }

  formErrors: Array<string> | null = null;

  title!: string;
  imdbId!: string;
  posterUrl!: string | null;
  startYear!: number;
  endYear!: number;
  plot!: string | null;
  loading = true;
  loadDetails() {
    return this.api.getSeriesDetails(this.imdbId)
      .pipe(
        tap(details => {
          this.title = details.title;
          this.posterUrl = details.posterUrl;
          this.startYear = details.startYear;
          this.endYear = details.endYear;
          this.plot = details.plot;
          for (let i = 0; i < details.seasons.length; i++) {
            const season = details.seasons[i];
            for (let j = 0; j < season.episodes.length; j++) {
              const episode = season.episodes[j];
              this.episodes.push({
                season: season.number,
                episode: episode.number,
                title: episode.title,
                imdbId: episode.imdbId,
                wanted: false
              })
            }
          }
        })
      )
  }

  episodes = new Array<{ season: number, episode: number, title: string, imdbId: string, wanted: boolean }>();

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
        tap(() => this.selectedTorrent$.next(null)),
        tap(() => this.selectedTorrentFiles = []),
        switchMap(value => this.api.searchSeriesTorrents(value)),
        catchError(() => of([])),
        tap(() => this.torrentSearchLoading = false),
        map(data => data.results),
        map(t => t.map((t: any) => {
          t.source = t.indexerName
          t.usages = 0
          return t
        })))
      .subscribe((results: any) => {
        this.torrents = results;
        console.log(this.torrents)
        for (const [key, value] of this.torrentToEpisodeMap.entries()) {
          const url = key.slice(0, key.indexOf('_____'));
          let searchResult = this.torrents.find(t => t.url == url)
          if (searchResult == undefined) {
            const torrent = this.episodeToTorrentMap.get(value)!;
            searchResult = {
              url: torrent.url,
              title: torrent.title,
              source: torrent.source,
              seeders: torrent.seeders,
              peers: torrent.peers,
              usages: 0
            }
            this.torrents.unshift(searchResult);
          }
          searchResult.usages++;
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
  // a map from a torrent url and path to an episode imdbId
  // The keys for this map look like `${url}__________${path}`
  torrentToEpisodeMap = new Map<string, string>();
  // a map from an episode imdbId to a torrent and its path
  episodeToTorrentMap = new Map<string, { url: string, source: string, title: string, seeders: number, peers: number, path: string }>();
  torrentFileToEpisodeMapping$ = new Subject<{ url: string, source: string, title: string, seeders: number, peers: number, path: string, event: any, episodeImdbId: string }>();

  torrentFileToEpisodeMappingSub!: Subscription;
  initializeTorrentFileToEpisodeMappingSub() {
    this.torrentFileToEpisodeMappingSub = this.torrentFileToEpisodeMapping$
      .pipe(
        map(mapping => {
          mapping.episodeImdbId = (mapping.event.target as HTMLSelectElement).value;
          return mapping
        })
      )
      .subscribe(mapping => {
        const torrentToEpisodeMapKey = `${mapping.url}_____${mapping.path}`;
        if (this.torrentToEpisodeMap.has(torrentToEpisodeMapKey)) {
          this.episodeToTorrentMap.delete(this.torrentToEpisodeMap.get(torrentToEpisodeMapKey)!)
          this.torrentToEpisodeMap.delete(torrentToEpisodeMapKey);
          this.torrents.find(t => t.url == mapping.url).usages--;
        }
        if (mapping.episodeImdbId.length > 0) {
          this.torrentToEpisodeMap.set(torrentToEpisodeMapKey, mapping.episodeImdbId);
          this.episodeToTorrentMap.set(mapping.episodeImdbId, {
            url: mapping.url,
            source: mapping.source,
            title: mapping.title,
            seeders: mapping.seeders,
            peers: mapping.peers,
            path: mapping.path
          });
          this.torrents.find(t => t.url == mapping.url).usages++;
        }
      })
  }

  loadPrefillData() {
    return this.api.checkSeriesExists(this.imdbId)
      .pipe(
        switchMap(exists => exists
          ? this.api.getSeriesStorageLocations(this.imdbId)
            .pipe(
              tap(storageLocations => {
                for (let i = 0; i < storageLocations.length; i++) {
                  const storageLocation = storageLocations[i];
                  if (this.storageLocationsMap.has(storageLocation)) {
                    this.storageLocationsMap.get(storageLocation).selected = true;
                  }
                }
                this.storageLocationEntries = Array.from(this.storageLocationsMap.entries())
              }),
              switchMap(() => this.api.getSeriesWantedInfo(this.imdbId)),
              map(info => info.seasons),
              tap(seasons => {
                for (let i = 0; i < seasons.length; i++) {
                  const season = seasons[i];
                  for (let j = 0; j < season.episodes.length; j++) {
                    const episode = season.episodes[j];
                    if (episode.wanted) {
                      this.episodes.find(e => e.season == season.number && e.episode == episode.number)!.wanted = true;
                    }
                  }
                }
              }),
              switchMap(() => this.api.getSeriesTorrents(this.imdbId)),
              map(data => data.torrents),
              tap(torrents => {
                if (torrents.length == 0) {
                  return;
                }
                this.torrentSearchLoading = false;
                for (let i = 0; i < torrents.length; i++) {
                  const torrent = torrents[i]
                  this.torrents.unshift({
                    title: torrent.name,
                    url: torrent.url,
                    source: torrent.source,
                    seeders: -1,
                    peers: -1,
                    usages: torrent.files.filter((f: any) => f.imdbId != null).length
                  })
                  for (let j = 0; j < torrent.files.length; j++) {
                    const file = torrent.files[j];
                    if (file.imdbId) {
                      this.torrentToEpisodeMap.set(`${torrent.url}_____${file.path}`, file.imdbId)
                      this.episodeToTorrentMap.set(file.imdbId, { url: torrent.url, source: torrent.source, title: torrent.name, seeders: -1, peers: -1, path: file.path })
                    }
                  }
                }
              })
            )
          : of([])
        )
      )
  }

  onDownload() {
    if (this.episodes.find(e => e.wanted) == undefined) {
      console.log('no episodes wanted');
      return;
    }
    if (this.storageLocationEntries.find(e => e[1].selected) == undefined) {
      console.log('no storage locations selected');
      return;
    }
    this.loading = true;
    this.api.checkSeriesExists(this.imdbId)
      .pipe(
        tap(exists => console.log(`${this.imdbId} exists: ${exists}`)),
        switchMap(exists => {
          const seasons = new Array<{ number: number, episodes: Array<{ number: number, name: string, imdbId: string, wanted: boolean }> }>();
          for (let i = 0; i < this.episodes.length; i++) {
            const episode = this.episodes[i];
            let season = seasons.find(s => s.number == episode.season);
            if (season == undefined) {
              seasons.push(season = {
                number: episode.season,
                episodes: []
              })
            }
            season.episodes.push({
              number: episode.episode,
              name: episode.title,
              imdbId: episode.imdbId,
              wanted: episode.wanted
            })
          }
          return exists
            ? this.api.updateSeriesEpisodes(this.imdbId, seasons)
            : this.api.addSeries(this.imdbId, this.title, this.plot, this.posterUrl, this.startYear, this.endYear, seasons);
        }),
        switchMap(() => {
          const apiTorrents = new Array<{ url: string, mappings: Array<{ path: string, imdbId: string }> }>();
          for (const [imdbId, torrent] of this.episodeToTorrentMap.entries()) {
            let apiTorrent = apiTorrents.find(t => t.url == torrent.url);
            if (apiTorrent == undefined) {
              apiTorrents.push(apiTorrent = {
                url: torrent.url,
                mappings: []
              });
            }
            apiTorrent.mappings.push({
              path: torrent.path,
              imdbId: imdbId
            })
          }
          return this.api.setSeriesTorrents(this.imdbId, apiTorrents)
        }),
        switchMap(() => this.api.setSeriesStorageLocations(this.imdbId, Array.from(this.storageLocationsMap.entries()).filter(sl => sl[1].selected).map(sl => sl[0]))),
        tap(() => this.loading = false),
        tap(() => this.router.navigate(['/']))
      )
      .subscribe()
  }

  ngOnDestroy(): void {
    this.torrentSearchControlSub.unsubscribe();
    this.selectedTorrentSub.unsubscribe();
    this.torrentFileToEpisodeMappingSub.unsubscribe();
  }
}

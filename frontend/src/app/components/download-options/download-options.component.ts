import { Component } from '@angular/core';
import { ApiService } from '../../services/api/api.service';
import { ActivatedRoute } from '@angular/router';
import { BehaviorSubject, catchError, debounceTime, distinctUntilChanged, filter, map, Observable, of, retry, Subject, Subscription, switchMap, tap } from 'rxjs';
import { KeyValuePipe, NgClass, NgFor, NgIf } from '@angular/common';
import { FormSectionHeaderComponent } from '../form-section-header/form-section-header.component';
import { FormSectionPartComponent } from '../form-section-part/form-section-part.component';
import { SeasonEpisodeFormatPipe } from '../../pipes/season-episode-format.pipe';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { NonBreakingSpacesPipe } from '../../pipes/non-breaking-spaces.pipe';
import { formatSeasonEpisode } from '../../functions/formatSeasonEpisode';

@Component({
  selector: 'app-download-options',
  imports: [NgIf, NgFor, FormSectionHeaderComponent, FormSectionPartComponent, SeasonEpisodeFormatPipe, NgClass, ReactiveFormsModule, NonBreakingSpacesPipe, KeyValuePipe],
  templateUrl: './download-options.component.html',
  styleUrl: './download-options.component.scss'
})
export class DownloadOptionsComponent {

  mediaId: string | null = null;
  mediaType: string | null = null;
  loading = true;

  title: string = "Loading";

  seasonsLoading = false;
  seasons: Array<any> | null = null;
  wantedEpisodesMap: Map<string, boolean> | null = null;

  storageLocationsLoading = true;
  storageLocations: Array<any> | null = null;
  storageLocationsSub: Subscription;

  torrentSearchControl = new FormControl();
  torrentSearchControlSub: Subscription | null = null;
  torrentSearchLoading = false;
  displayTorrentSearchResults = false;
  torrentSearchResults: Array<any> = [];
  usedTorrentsMap = new Map<string, number>();

  selectedTorrent$ = new BehaviorSubject<string | null>(null);
  displaySelectedTorrentFiles = false;
  selectedTorrentFilesLoading = false;
  selectedTorrentUrl: string | null = null;
  selectedTorrentFiles: Array<any> | null = null;
  selectedTorrentSub: Subscription;

  episodesToTorrentFileMap: Map<string, { url: string, path: string } | null> | null = null;
  unmappedEpisodes: Array<string> | null = null;
  torrentFileToEpisodeMapping$: Subject<{ path: string, event: any }> | null = null;
  torrentFileToEpisodeMappingSub: Subscription | null = null;

  torrentMappedToMovie: {url: string, path: string}|null = null;
  torrentFileToMovieMapping$: Subject<string>|null = null;
  torrentFileToMovieMappingSub: Subscription|null = null;

  getMediaTypeSub: Subscription;
  getDetailsSub: Subscription | null = null;

  formErrors: Array<string>|null = null;

  constructor(public api: ApiService, public route: ActivatedRoute) {
    this.mediaId = this.route.snapshot.params['id'];
    this.getMediaTypeSub = this.api.getMediaType(this.mediaId!).subscribe(type => {
      this.mediaType = type;
      this.loading = false;
      let torrentSearchFun: (query: string) => Observable<any>;
      if (this.mediaType == 'SERIES') {
        this.seasonsLoading = true;
        this.getDetailsSub = this.api.getSeriesDetails(this.mediaId!).subscribe(data => {
          console.log(data);
          this.title = data.title;
          this.seasonsLoading = false;
          this.seasons = data.seasons;
          this.wantedEpisodesMap = new Map();
          this.episodesToTorrentFileMap = new Map();
          this.unmappedEpisodes = [];
          this.torrentFileToEpisodeMapping$ = new Subject();
          this.torrentFileToEpisodeMappingSub = this.torrentFileToEpisodeMapping$.subscribe(mapping => {
            const episode = (mapping.event.target as HTMLSelectElement).value;
            for (const [episode, torrentFile] of this.episodesToTorrentFileMap!.entries()) {
              if (torrentFile == null) {
                continue;
              }
              if (torrentFile!.url == this.selectedTorrentUrl && torrentFile!.path == mapping.path) {
                this.episodesToTorrentFileMap!.set(episode, null);
                this.unmappedEpisodes!.push(episode)
                break;
              }
            }
            if (episode.length == 0) {
              return;
            }
            this.episodesToTorrentFileMap!.set(episode, { url: this.selectedTorrentUrl!, path: mapping.path });
            const i = this.unmappedEpisodes!.findIndex(e => e == episode)
            if (i != -1) {
              this.unmappedEpisodes!.splice(i, 1);
            }
          })
          console.log(this.seasons);
          console.log(this.episodesToTorrentFileMap);
          torrentSearchFun = this.api.searchSeriesTorrents.bind(this.api);
        });
      } else if (this.mediaType == 'MOVIE') {
        this.getDetailsSub = this.api.getMovieDetails(this.mediaId!).subscribe(data => {
          console.log(data);
          this.title = data.title;
          torrentSearchFun = this.api.searchMovieTorrents.bind(this.api);
        });
        this.torrentFileToMovieMapping$ = new Subject();
        this.torrentFileToMovieMappingSub = this.torrentFileToMovieMapping$.subscribe(path => {
          this.torrentMappedToMovie = {url: this.selectedTorrentUrl!, path: path};
          for(const [url, count] of this.usedTorrentsMap){
            if(count != 0){
              this.usedTorrentsMap.set(url, 0)
            }
          }
          this.usedTorrentsMap.set(this.selectedTorrentUrl!, 1);
          console.log(this.torrentMappedToMovie)
        })
      }
      this.torrentSearchControlSub = this.torrentSearchControl.valueChanges.pipe(
        filter((val) => val != null && val.length > 0),
        debounceTime(1000),
        distinctUntilChanged(),
        tap(() => this.displayTorrentSearchResults = true),
        tap(() => this.torrentSearchLoading = true),
        tap(() => this.usedTorrentsMap = new Map()),
        switchMap(value => torrentSearchFun(value)),
        catchError(() => of([])),
        tap(() => this.torrentSearchLoading = false))
        .subscribe(data => {
          this.torrentSearchResults = data.results;
          for (let i = 0; i < this.torrentSearchResults.length; i++) {
            this.usedTorrentsMap.set(this.torrentSearchResults[i].url, 0);
          }
          console.log(data)
        });
    });
    this.storageLocationsSub = this.api.getStorageLocations()
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
        }
        ))
      .subscribe(data => {
        this.storageLocationsLoading = false;
        this.storageLocations = data
        console.log(data)
      });
    this.selectedTorrentSub = this.selectedTorrent$.pipe(
      filter((val) => val != null && val.length > 0),
      debounceTime(200),
      distinctUntilChanged(),
      tap(() => this.displaySelectedTorrentFiles = true),
      tap(() => this.selectedTorrentFilesLoading = true),
      tap(val => this.selectedTorrentUrl = val),
      switchMap(url => this.api.inspectTorrent(url!)),
      tap(() => this.selectedTorrentFilesLoading = false)
    ).subscribe(data => {
      this.selectedTorrentFiles = data
    })
  }

  onEpisodeSelectionChange(season: number, episode: number, title: string, e: any) {
    const wanted = e.srcElement.checked;
    const formatted = formatSeasonEpisode(season, episode, title);
    const mapWanted = this.wantedEpisodesMap!.has(formatted) && this.wantedEpisodesMap!.get(formatted)
    if (!wanted && mapWanted) {
      this.wantedEpisodesMap!.set(formatted, false);
      this.episodesToTorrentFileMap?.delete(formatted);
      const i = this.unmappedEpisodes!.findIndex(e => e == formatted);
      if (i != -1) {
        this.unmappedEpisodes!.splice(i, 1);
      }
    }
    else if (wanted && !mapWanted) {
      this.wantedEpisodesMap!.set(formatted, true);
      this.episodesToTorrentFileMap!.set(formatted, null);
      this.unmappedEpisodes!.push(formatted)
    }
  }

  onStorageLocationSelectionChange(id: string) {
    const sl = this.storageLocations!.find(sl => sl.id == id);
    sl.selected = !sl.selected;
  }

  onDownload(){
    this.formErrors = [];
    const storageLocationNotMapped = this.storageLocations!.findIndex(sl => sl.selected) == -1;
    if(storageLocationNotMapped){
      this.formErrors.push("no storage locations mapped")
    }
    if(this.mediaType == 'SERIES'){
      const noEpisodesWanted = this.episodesToTorrentFileMap!.size == 0;
      if(noEpisodesWanted){
        this.formErrors.push("no episodes wanted")
      }
      else {
        let noEpisodesMapped = true;
        for(const torrent of this.episodesToTorrentFileMap!.values()){
          if(torrent != null){
            noEpisodesMapped = false;
            break;
          }
        }
        if(noEpisodesMapped){
          this.formErrors.push("must map at least one episode")
        }
      }
    } else if(this.mediaType == 'MOVIE'){
      const notMapped = this.torrentMappedToMovie == null;
      if(notMapped){
        this.formErrors.push("no torrent file mapped")
      }
    }
    if(this.formErrors.length == 0){
      alert("gonna do the api stuff now")
    }
  }
}

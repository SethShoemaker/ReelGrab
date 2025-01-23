import { NgClass, NgFor, NgIf } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-media-download',
  imports: [NgIf, NgFor, NgClass],
  templateUrl: './media-download.component.html',
  styleUrl: './media-download.component.scss'
})
export class MediaDownloadComponent {

  imdbId: string;
  mediaDetailsLoading = true;
  title: string|null = null;
  type: string | null = null;
  posterUrl: string|null = null;
  seasons: Array<any>|null = null;

  storageLocationsLoading = true;
  storageLocations: Array<any>|null = null;

  selectedSeriesEpisodes: Map<number, {allSelected: boolean, episodes: Map<number, {season: number, episode: number, selected: boolean}>}>|null = null;
  selectedStorageLocationId: string|null = null;

  typeSub: Subscription;
  detailsSub: Subscription | null = null;
  storageLocationsSub: Subscription;

  constructor(private http: HttpClient, private route: ActivatedRoute) {
    this.imdbId = this.route.snapshot.paramMap.get('imdbId')!;
    this.typeSub = this.http.get(`/media_index/type?imdbId=${this.imdbId}`).subscribe((data: any) => {
      this.type = data.type;
      if (this.type == 'MOVIE') {
        this.getMovieDetails(this.imdbId);
      } else if (this.type == 'SERIES') {
        this.getSeriesDetails(this.imdbId);
      }
    })
    this.storageLocationsSub = this.http.get('/storage_gateway/storage_locations').subscribe((data: any) => {
      this.storageLocationsLoading = false;
      this.storageLocations = data;
      console.log(this.storageLocations);
    })
  }

  getMovieDetails(imdbId: string) {
    this.detailsSub?.unsubscribe();
    this.detailsSub = this.http.get(`/media_index/movie/details?imdbId=${imdbId}`).subscribe((data: any) => {
      this.mediaDetailsLoading = false
      this.title = data.title;
      this.posterUrl = data.posterUrl ?? 'https://placehold.co/600x400';
    });
  }

  getSeriesDetails(imdbId: string) {
    this.detailsSub?.unsubscribe();
    this.detailsSub = this.http.get(`/media_index/series/details?imdbId=${imdbId}`).subscribe((data: any) => {
      this.mediaDetailsLoading = false;
      this.title = data.title;
      this.posterUrl = data.posterUrl ?? 'https://placehold.co/600x400';
      this.seasons = data.seasons;
      this.selectedSeriesEpisodes = new Map([
        ...data.seasons.map((s: any) => {
          return [
            s.number,
            {
              allSelected: false,
              episodes: new Map([
                ...s.episodes.map((e: any) => {
                  return [
                    e.number,
                    {
                      season: s.number,
                      episode: e.number,
                      selected: false
                    }
                  ]
                })
              ])
            }
          ]
        })
      ])
    });
  }

  toggleSeriesEpisode(season: number, episode: number){
    const seasonObj = this.selectedSeriesEpisodes!.get(season)!;
    const episodeObj = seasonObj.episodes.get(episode)!;
    episodeObj.selected = !episodeObj.selected
    let allSelected = true;
    seasonObj.episodes.forEach((episode: any) => {
      if(!episode.selected){
        allSelected = false;
      }
    })
    seasonObj.allSelected = allSelected;
  }

  toggleSeriesSeason(season: number){
    const seasonObj = this.selectedSeriesEpisodes!.get(season)!;
    let allSelected = true;
    seasonObj.episodes.forEach((episode: any) => {
      if(!episode.selected){
        allSelected = false;
      }
    })
    seasonObj.episodes.forEach((episode: any) => {
      if(allSelected || !episode.selected){
        this.toggleSeriesEpisode(season, episode.episode)
      }
    })
  }

  ngOnDestroy(): void {
    this.detailsSub?.unsubscribe();
    this.typeSub.unsubscribe();
    this.storageLocationsSub.unsubscribe();
  }
}

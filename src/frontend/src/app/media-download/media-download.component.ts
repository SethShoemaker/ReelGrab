import { NgFor, NgIf } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-media-download',
  imports: [NgIf, NgFor],
  templateUrl: './media-download.component.html',
  styleUrl: './media-download.component.scss'
})
export class MediaDownloadComponent {

  imdbId: string;
  loading = true;
  title: string|null = null;
  type: string | null = null;
  posterUrl: string|null = null;
  seasons: Array<any>|null = null;

  typeSub: Subscription;
  detailsSub: Subscription | null = null;

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
  }

  getMovieDetails(imdbId: string) {
    this.detailsSub?.unsubscribe();
    this.detailsSub = this.http.get(`/media_index/movie/details?imdbId=${imdbId}`).subscribe((data: any) => {
      console.log(data)
      this.loading = false
      this.title = data.title;
      this.posterUrl = data.posterUrl ?? 'https://placehold.co/600x400';
    });
  }

  getSeriesDetails(imdbId: string) {
    this.detailsSub?.unsubscribe();
    this.detailsSub = this.http.get(`/media_index/series/details?imdbId=${imdbId}`).subscribe((data: any) => {
      console.log(data)
      this.loading = false;
      this.title = data.title;
      this.posterUrl = data.posterUrl ?? 'https://placehold.co/600x400';
      this.seasons = data.seasons;
    });
  }

  ngOnDestroy(): void {
    this.detailsSub?.unsubscribe();
    this.typeSub.unsubscribe();
  }
}

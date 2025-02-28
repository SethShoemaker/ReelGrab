import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ApiService {

  constructor(private http: HttpClient) { }

  mediaSearch(query: string): Observable<any> {
    return this.http.get(`http://localhost:5242/media_index/search?query=${query}`)
  }

  getMediaType(imdbId: string): Observable<string> {
    return this.http.get(`http://localhost:5242/media_index/type?imdbId=${imdbId}`).pipe(
      map((r: any) => r.type)
    );
  }

  getStorageLocations(): Observable<any> {
    return this.http.get(`http://localhost:5242/storage_gateway/storage_locations`);
  }

  getMovieDetails(movieId: string): Observable<any> {
    return this.http.get(`http://localhost:5242/media_index/movie/details?imdbId=${movieId}`);
  }

  getSeriesDetails(seriesId: string): Observable<any> {
    return this.http.get(`http://localhost:5242/media_index/series/details?imdbId=${seriesId}`);
  }

  searchMovieTorrents(query: string): Observable<any> {
    console.log(`http://localhost:5242/torrent_index/search/movie?query=${query}`)
    return this.http.get(`http://localhost:5242/torrent_index/search/movie?query=${query}`);
  }

  searchSeriesTorrents(query: string): Observable<any> {
    console.log(`http://localhost:5242/torrent_index/search/series?query=${query}`)
    return this.http.get(`http://localhost:5242/torrent_index/search/series?query=${query}`);
  }

  inspectTorrent(torrentUrl: string): Observable<any> {
    return this.http.get(`http://localhost:5242/torrent_index/inspect?url=${encodeURIComponent(torrentUrl)}`);
  }
}

import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable, tap } from 'rxjs';
import { formatSeasonEpisodeNumber } from '../../functions/formatSeasonEpisode';

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

  addWantedMedia(imdbId: string): Observable<any> {
    return this.http.post(`http://localhost:5242/wanted_media?imdbId=${imdbId}`, {}).pipe(
      tap(v => console.log(v))
    );
  }

  setSeriesWantedEpisodes(seriesId: string, episodes: Array<string | {season: number, episode: number}>){
    episodes = episodes.map(e => typeof(e) == 'string' ? e : formatSeasonEpisodeNumber(e.season, e.episode));
    return this.http.post(`http://localhost:5242/wanted_media/series_episodes?imdbId=${seriesId}&episodes=${episodes.join(",")}`, {}).pipe(
      tap(v => console.log(v))
    );
  }

  setWantedMediaStorageLocations(imdbId: string, storageLocationIds: Array<string>): Observable<any> {
    return this.http.post(`http://localhost:5242/wanted_media/storage_locations?imdbId=${imdbId}&storageLocations=${storageLocationIds.join(",")}`, {}).pipe(
      tap(v => console.log(v))
    );
  }

  setWantedSeriesEpisodeToTorrentMapping(seriesId: string, torrents: Array<{torrentUrl: string, torrentSource: string, torrentDisplayName: string, episodes: Array<{imdbId: string, torrentFilePath: string}>}>): Observable<any> {
    return this.http.post(`http://localhost:5242/wanted_media/series_torrents?imdbId=${seriesId}`, torrents).pipe(
      tap(v => console.log(v))
    )
  }

  setWantedMovieTorrentMapping(movieId: string, torrentUrl: string, torrentSource: string, torrentDisplayName: string, torrentFilePath: string): Observable<any> {
    return this.http.post(`http://localhost:5242/wanted_media/movie_torrent?imdbId=${movieId}&torrentUrl=${encodeURIComponent(torrentUrl)}&torrentSource=${encodeURIComponent(torrentSource)}&torrentDisplayName=${encodeURIComponent(torrentDisplayName)}&torrentFilePath=${encodeURIComponent(torrentFilePath)}`, {}).pipe(
      tap(v => console.log(v))
    )
  }
}

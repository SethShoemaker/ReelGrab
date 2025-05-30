import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable, switchMap, tap } from 'rxjs';
import { formatSeasonEpisodeNumber } from '../../functions/formatSeasonEpisode';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ApiService {

  constructor(private http: HttpClient) { }

  mediaSearch(query: string): Observable<any> {
    return this.http.get(`${environment.apiUrl}/api/media/search/movies_and_series?query=${query}`)
  }

  getStorageLocations(): Observable<any> {
    return this.http.get(`${environment.apiUrl}/api/storage/locations`);
  }

  getMovieDetails(movieId: string): Observable<any> {
    return this.http.get(`${environment.apiUrl}/api/media/movies/${movieId}/details`);
  }

  getSeriesDetails(seriesId: string): Observable<any> {
    return this.http.get(`${environment.apiUrl}/api/media/series/${seriesId}/details`);
  }

  searchMovieTorrents(query: string): Observable<any> {
    console.log(`${environment.apiUrl}/api/torrent_index/search/movie?query=${query}`)
    return this.http.get(`${environment.apiUrl}/api/torrent_index/search/movie?query=${query}`);
  }

  searchSeriesTorrents(query: string): Observable<any> {
    console.log(`${environment.apiUrl}/api/torrent_index/search/series?query=${query}`)
    return this.http.get(`${environment.apiUrl}/api/torrent_index/search/series?query=${query}`);
  }

  checkTorrentExists(torrentUrl: string): Observable<any> {
    return this.http.get(`${environment.apiUrl}/api/torrents/exists?url=${encodeURIComponent(torrentUrl)}`)
      .pipe(
        map((res: any) => res.exists)
      )
  }

  createTorrent(torrentUrl: string, torrentSource: string): Observable<any> {
    return this.http.post(`${environment.apiUrl}/api/torrents`, {
      Url: torrentUrl,
      Source: torrentSource
    })
  }

  inspectTorrent(torrentUrl: string): Observable<any> {
    return this.http.get(`${environment.apiUrl}/api/torrents/inspect?url=${encodeURIComponent(torrentUrl)}`);
  }

  checkSeriesExists(imdbId: string) {
    return this.http.get(`${environment.apiUrl}/api/series/${imdbId}/exists`)
      .pipe(
        map((res: any) => res.exists)
      )
  }

  getSeriesStorageLocations(imdbId: string) {
    return this.http.get(`${environment.apiUrl}/api/series/${imdbId}/storage_locations`)
      .pipe(
        map((res: any) => res.storageLocations)
      )
  }

  getSeriesWantedInfo(imdbId: string): Observable<{seasons: Array<{number: number, episodes: Array<{number: number, name: string, imdbId: string, wanted: boolean}>}>}> {
    return this.http.get<{seasons: Array<{number: number, episodes: Array<{number: number, name: string, imdbId: string, wanted: boolean}>}>}>(`${environment.apiUrl}/api/series/${imdbId}/wanted`);
  }

  addSeries(imdbId: string, name: string, description: string|null, poster: string|null, startYear: number, endYear: number|null, seasons: Array<{number: number, episodes: Array<{number: number, name: string, imdbId: string, wanted: boolean}>}>){
    return this.http.post(`${environment.apiUrl}/api/series`, {
      ImdbId: imdbId,
      Name: name,
      Description: description,
      Poster: poster,
      StartYear: startYear,
      EndYear: endYear,
      Seasons: seasons.map(s => ({
        Number: s.number,
        Episodes: s.episodes.map(e => ({
          Number: e.number,
          Name: e.name,
          ImdbId: e.imdbId,
          Wanted: e.wanted
        }))
      }))
    })
  }

  updateSeriesEpisodes(imdbId: string, seasons: Array<{number: number, episodes: Array<{number: number, name: string, imdbId: string, wanted: boolean}>}>): Observable<any> {
    return this.http.patch(`${environment.apiUrl}/api/series/${imdbId}/episodes`, {
      seasons: seasons
    })
  }

  setSeriesTorrents(imdbId: string, torrents: Array<{url: string, mappings: Array<{path: string, imdbId: string}>}>): Observable<any> {
    return this.http.post(`${environment.apiUrl}/api/series/${imdbId}/torrent_mappings`, { torrents: torrents })
  }

  getSeriesTorrents(imdbId: string): Observable<any> {
    return this.http.get(`${environment.apiUrl}/api/series/${imdbId}/torrent_mappings`)
  }

  setSeriesStorageLocations(imdbId: string, storageLocations: Array<string>): Observable<any> {
    return this.http.post(`${environment.apiUrl}/api/series/${imdbId}/storage_locations`, {StorageLocations: storageLocations})
  }

  checkMovieExists(imdbId: string): Observable<boolean> {
    return this.http.get(`${environment.apiUrl}/api/movies/${imdbId}/exists`)
      .pipe(
        map((res: any) => res.exists)
      )
  }

  getMoviesInProgress(): Observable<Array<{id: number, imdbId: string, name: string, storageLocations: Array<string>, progress: number}>> {
    return this.http.get<Array<{id: number, imdbId: string, name: string, storageLocations: Array<string>, progress: number}>>(`${environment.apiUrl}/api/movies/in_progress`);
  }

  getSeriesInProgress(): Observable<Array<{id: number, imdbId: string, name: string, episodes: Array<{season: number, episode: number, imdbId: string, name: string, progress: number}>, storageLocations: Array<string>}>> {
    return this.http.get<Array<{id: number, imdbId: string, name: string, episodes: Array<{season: number, episode: number, imdbId: string, name: string, progress: number}>, storageLocations: Array<string>}>>(`${environment.apiUrl}/api/series/in_progress`)
  }

  getMovieStorageLocations(imdbId: string): Observable<Array<string>> {
    return this.http.get(`${environment.apiUrl}/api/movies/${imdbId}/storage_locations`)
      .pipe(
        map((res: any) => res.storageLocations)
      )
  }

  setMovieStorageLocations(imdbId: string, storageLocations: Array<string>): Observable<any> {
    return this.http.post(`${environment.apiUrl}/api/movies/${imdbId}/storage_locations`, {
      storageLocations: storageLocations
    })
  }

  getMovieCinematicCutTorrent(imdbId: string): Observable<any> {
    return this.http.get(`${environment.apiUrl}/api/movies/${imdbId}/cinematic_cut_torrent`)
  }

  setMovieCinematicCutTorrent(imdbId: string, torrentUrl: string, torrentSource: string, torrentFilePath: string): Observable<any> {
    return this.http.post(`${environment.apiUrl}/api/movies/${imdbId}/cinematic_cut_torrent`, {
      TorrentUrl: torrentUrl,
      TorrentSource: torrentSource,
      TorrentFilePath: torrentFilePath
    })
  }

  addMovie(imdbId: string, name: string, description: string | null, poster: string | null, year: number, wanted: boolean): Observable<any> {
    return this.http.post(`${environment.apiUrl}/api/movies`, {
      ImdbId: imdbId,
      Name: name,
      Description: description,
      Poster: poster,
      Year: year,
      Wanted: wanted
    })
  }

  /**
   * Ensures a torrent exists, creates it if it doesn't exist, then inspects it
   * @param torrentUrl 
   * @param torrentSource 
   * @returns 
   */
  ensureAndInspectTorrent(torrentUrl: string, torrentSource: string): Observable<any> {
    return this.checkTorrentExists(torrentUrl)
      .pipe(
        switchMap(exists => exists
          ? this.inspectTorrent(torrentUrl)
          : this.createTorrent(torrentUrl, torrentSource)
            .pipe(
              switchMap(() => this.inspectTorrent(torrentUrl))
            )
        )
      )
  }

  getMediaIndexConfig(): Observable<object> {
    return this.http.get<object>(`${environment.apiUrl}/api/media/config`).pipe(
      tap(val => console.log(val))
    );
  }

  setMediaIndexConfig(config: object): Observable<object> {
    return this.http.put(`${environment.apiUrl}/api/media/config`, config).pipe(
      tap(val => console.log(val))
    );
  }

  getStorageGatewayConfig(): Observable<object> {
    return this.http.get<object>(`${environment.apiUrl}/api/storage/config`).pipe(
      tap(val => console.log(val))
    );
  }

  setStorageGatewayConfig(config: object): Observable<object> {
    return this.http.put(`${environment.apiUrl}/api/storage/config`, config).pipe(
      tap(val => console.log(val))
    );
  }
}

export type getInProgressMediaResponse = { media: Array<{ imdbId: string, displayName: string, mediaType: string, storageLocations: Array<string>, downloadables: Array<{ imdbId: string, displayName: string, season: number, episode: number, progress: number }> }> };
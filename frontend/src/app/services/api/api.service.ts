import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable, switchMap, tap } from 'rxjs';
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

  checkTorrentExists(torrentUrl: string): Observable<any> {
    return this.http.get(`http://localhost:5242/api/torrents/exists?url=${encodeURIComponent(torrentUrl)}`)
      .pipe(
        map((res: any) => res.exists)
      )
  }

  createTorrent(torrentUrl: string, torrentSource: string): Observable<any> {
    return this.http.post(`http://localhost:5242/api/torrents`, {
      Url: torrentUrl,
      Source: torrentSource
    })
  }

  inspectTorrent(torrentUrl: string): Observable<any> {
    return this.http.get(`http://localhost:5242/api/torrents/inspect?url=${encodeURIComponent(torrentUrl)}`);
  }

  checkMovieExists(imdbId: string): Observable<boolean> {
    return this.http.get(`http://localhost:5242/api/movies/${imdbId}/exists`)
      .pipe(
        map((res: any) => res.exists)
      )
  }

  getMovieStorageLocations(imdbId: string): Observable<Array<string>> {
    return this.http.get(`http://localhost:5242/api/movies/${imdbId}/storage_locations`)
      .pipe(
        map((res: any) => res.storageLocations)
      )
  }

  setMovieStorageLocations(imdbId: string, storageLocations: Array<string>): Observable<any> {
    return this.http.post(`http://localhost:5242/api/movies/${imdbId}/storage_locations`, {
      storageLocations: storageLocations
    })
  }

  getMovieTheatricalReleaseTorrent(imdbId: string): Observable<any> {
    return this.http.get(`http://localhost:5242/api/movies/${imdbId}/theatrical_release_torrent`)
  }

  setMovieTheatricalReleaseTorrent(imdbId: string, torrentUrl: string, torrentSource: string, torrentFilePath: string): Observable<any> {
    return this.http.post(`http://localhost:5242/api/movies/${imdbId}/theatrical_release_torrent`, {
      TorrentUrl: torrentUrl,
      TorrentSource: torrentSource,
      TorrentFilePath: torrentFilePath
    })
  }

  addMovie(imdbId: string, name: string, description: string | null, poster: string | null, year: number, wanted: boolean): Observable<any> {
    return this.http.post(`http://localhost:5242/api/movies`, {
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

  checkWantedMedia(imdbId: string): Observable<boolean> {
    return this.http.get<{ wanted: boolean }>(`http://localhost:5242/wanted_media/check_wanted?imdbId=${encodeURIComponent(imdbId)}`).pipe(
      tap(val => console.log(val)),
      map(val => val.wanted)
    )
  }

  addWantedMedia(imdbId: string): Observable<any> {
    return this.http.post(`http://localhost:5242/wanted_media?imdbId=${imdbId}`, {}).pipe(
      tap(v => console.log(v))
    );
  }

  getWantedSeriesEpisodes(seriesId: string): Observable<{ seasons: Array<{ number: number, episodes: Array<{ number: number, title: string, imdbId: string, wanted: boolean }> }> }> {
    return this.http.get<any>(`http://localhost:5242/wanted_media/series_episodes?imdbId=${seriesId}`).pipe(
      tap(val => console.log(val))
    )
  }

  setWantedSeriesEpisodes(seriesId: string, episodes: Array<string | { season: number, episode: number }>) {
    episodes = episodes.map(e => typeof (e) == 'string' ? e : formatSeasonEpisodeNumber(e.season, e.episode));
    return this.http.post(`http://localhost:5242/wanted_media/series_episodes?imdbId=${seriesId}&episodes=${episodes.join(",")}`, {}).pipe(
      tap(v => console.log(v))
    );
  }

  refreshWantedSeriesEpisodes(seriesId: string): Observable<any> {
    return this.http.get(`http://localhost:5242/wanted_media/refresh_series_episodes?imdbId=${seriesId}`).pipe(
      tap(val => console.log(val))
    )
  }

  getWantedMediaStorageLocations(imdbId: string): Observable<{ storageLocations: Array<string> }> {
    return this.http.get<{ storageLocations: Array<string> }>(`http://localhost:5242/wanted_media/storage_locations?imdbId=${encodeURIComponent(imdbId)}`).pipe(
      tap(val => console.log(val)),
    )
  }

  setWantedMediaStorageLocations(imdbId: string, storageLocationIds: Array<string>): Observable<any> {
    return this.http.post(`http://localhost:5242/wanted_media/storage_locations?imdbId=${imdbId}&storageLocations=${storageLocationIds.join(",")}`, {}).pipe(
      tap(v => console.log(v))
    );
  }

  getWantedSeriesEpisodeToTorrentFileMapping(seriesId: string): Observable<{ seasons: Array<{ number: number, episodes: Array<{ number: number, title: string, imdbId: string, wanted: boolean, mediaTorrent: { torrentUrl: string, source: string, displayName: string, filePath: string } | null }> }> }> {
    return this.http.get<any>(`http://localhost:5242/wanted_media/series_torrents?imdbId=${seriesId}`).pipe(
      tap(val => console.log(val)),
      map(data => ({ seasons: data }))
    )
  }

  setWantedSeriesEpisodeToTorrentMapping(seriesId: string, torrents: Array<{ torrentUrl: string, torrentSource: string, torrentDisplayName: string, episodes: Array<{ imdbId: string, torrentFilePath: string }> }>): Observable<any> {
    return this.http.post(`http://localhost:5242/wanted_media/series_torrents?imdbId=${seriesId}`, torrents).pipe(
      tap(v => console.log(v))
    )
  }

  getWantedMovieToTorrentMapping(movieId: string): Observable<{ torrent: { torrentUrl: string, source: string, dispayName: string, filePath: string } | null }> {
    return this.http.get<{ torrent: { torrentUrl: string, source: string, dispayName: string, filePath: string } | null }>(`http://localhost:5242/wanted_media/movie_torrent?imdbId=${encodeURIComponent(movieId)}`).pipe(
      tap(val => console.log(val))
    )
  }

  setWantedMovieTorrentMapping(movieId: string, torrentUrl: string, torrentSource: string, torrentDisplayName: string, torrentFilePath: string): Observable<any> {
    return this.http.post(`http://localhost:5242/wanted_media/movie_torrent?imdbId=${movieId}&torrentUrl=${encodeURIComponent(torrentUrl)}&torrentSource=${encodeURIComponent(torrentSource)}&torrentDisplayName=${encodeURIComponent(torrentDisplayName)}&torrentFilePath=${encodeURIComponent(torrentFilePath)}`, {}).pipe(
      tap(v => console.log(v))
    )
  }

  getInProgressMedia(): Observable<getInProgressMediaResponse> {
    return this.http.get<any>(`http://localhost:5242/wanted_media/in_progress`)
  }

  getMediaIndexConfig(): Observable<object> {
    return this.http.get<object>(`http://localhost:5242/media_index/config`).pipe(
      tap(val => console.log(val))
    );
  }

  setMediaIndexConfig(config: object): Observable<object> {
    return this.http.post(`http://localhost:5242/media_index/config`, config).pipe(
      tap(val => console.log(val))
    );
  }

  getTorrentIndexConfig(): Observable<object> {
    return this.http.get<object>(`http://localhost:5242/torrent_index/config`).pipe(
      tap(val => console.log(val))
    );
  }

  setTorrentIndexConfig(config: object): Observable<object> {
    return this.http.post(`http://localhost:5242/torrent_index/config`, config).pipe(
      tap(val => console.log(val))
    );
  }

  getStorageGatewayConfig(): Observable<object> {
    return this.http.get<object>(`http://localhost:5242/storage_gateway/config`).pipe(
      tap(val => console.log(val))
    );
  }

  setStorageGatewayConfig(config: object): Observable<object> {
    return this.http.post(`http://localhost:5242/storage_gateway/config`, config).pipe(
      tap(val => console.log(val))
    );
  }
}

export type getInProgressMediaResponse = { media: Array<{ imdbId: string, displayName: string, mediaType: string, storageLocations: Array<string>, downloadables: Array<{ imdbId: string, displayName: string, season: number, episode: number, progress: number }> }> };
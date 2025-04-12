import { Component, OnInit } from '@angular/core';
import { FormSectionHeaderComponent } from '../form-section-header/form-section-header.component';
import { FormSectionPartComponent } from '../form-section-part/form-section-part.component';
import { ApiService } from '../../services/api/api.service';
import { forkJoin, map, Observable } from 'rxjs';
import { AsyncPipe, JsonPipe, NgFor, NgIf } from '@angular/common';
import { formatSeasonEpisode } from '../../functions/formatSeasonEpisode';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-dashboard',
  imports: [FormSectionHeaderComponent, FormSectionPartComponent, JsonPipe, NgFor, AsyncPipe, NgIf, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {

  inProgressMedia$!: Observable<Array<{ name: string, storageLocations: string, progress: number, link: string }>>;
  storageLocations$!: Observable<Array<any>>;

  constructor(public api: ApiService) { }

  ngOnInit(): void {

    this.inProgressMedia$ = forkJoin([
      this.api.getMoviesInProgress().pipe(
        map(movies => movies.map(m => ({
          name: m.name,
          storageLocations: m.storageLocations.join(','),
          progress: m.progress,
          link: `/movies/download/${m.imdbId}`
        })))
      ),
      this.api.getSeriesInProgress().pipe(
        map(serieses => {
          const episodes = new Array<{ name: string, storageLocations: string, progress: number, link: string }>();
          for (let i = 0; i < serieses.length; i++) {
            const series = serieses[i];
            for (let j = 0; j < series.episodes.length; j++) {
              const episode = series.episodes[j];
              episodes.push({
                name: `${series.name} ${formatSeasonEpisode(episode.season, episode.episode, episode.name)}`,
                storageLocations: series.storageLocations.join(','),
                progress: episode.progress,
                link: `/tv/download/${series.imdbId}`
              })
            }
          }
          return episodes
        })
      )
    ]).pipe(
      map(data => [...data[0], ...data[1]])
    )

    this.storageLocations$ = this.api.getStorageLocations().pipe(
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
      })
    )
  }
}

import { Component, OnInit } from '@angular/core';
import { FormSectionHeaderComponent } from '../form-section-header/form-section-header.component';
import { FormSectionPartComponent } from '../form-section-part/form-section-part.component';
import { ApiService } from '../../services/api/api.service';
import { map, Observable } from 'rxjs';
import { AsyncPipe, JsonPipe, NgFor, NgIf } from '@angular/common';
import { formatSeasonEpisode } from '../../functions/formatSeasonEpisode';

@Component({
  selector: 'app-dashboard',
  imports: [FormSectionHeaderComponent, FormSectionPartComponent, JsonPipe, NgFor, AsyncPipe, NgIf],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {

  inProgressMedia$!: Observable<Array<{ name: string, storageLocations: string, progress: number }>>;
  storageLocations$!: Observable<Array<any>>;

  constructor(public api: ApiService) { }

  ngOnInit(): void {
    this.inProgressMedia$ = this.api.getInProgressMedia().pipe(
      map(response => {
        const res = new Array<{ name: string, storageLocations: string, progress: number }>();
        for (let i = 0; i < response.media.length; i++) {
          const media = response.media[i];
          if (media.mediaType == 'MOVIE') {
            res.push({
              name: media.displayName,
              storageLocations: media.storageLocations.join(','),
              progress: media.downloadables[0].progress
            });
            continue;
          }
          if (media.mediaType == 'SERIES') {
            for (let j = 0; j < media.downloadables.length; j++) {
              const episode = media.downloadables[j];
              res.push({
                name: `${media.displayName} ${formatSeasonEpisode(episode.season, episode.episode, episode.displayName)}`,
                storageLocations: media.storageLocations.join(','),
                progress: episode.progress
              });
              continue;
            }
            continue;
          }
          res.push({
            name: `error: unhandled media type ${media.mediaType}`,
            storageLocations: '',
            progress: -1
          });
        }
        return res;
      })
    );

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

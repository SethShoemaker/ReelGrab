import { Routes } from '@angular/router';
import { MediaConfigsComponent } from './media_configs/media_configs.component';
import { MediaSearchComponent } from './media-search/media-search.component';
import { MediaDownloadComponent } from './media-download/media-download.component';
import { StorageConfigsComponent } from './storage-configs/storage-location-configs.component';

export const routes: Routes = [
    { path: 'media_configs', component: MediaConfigsComponent },
    { path: 'media_search', component: MediaSearchComponent },
    { path: 'media_download/:imdbId', component: MediaDownloadComponent },
    { path: 'storage_configs', component: StorageConfigsComponent }
];

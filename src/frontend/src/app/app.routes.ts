import { Routes } from '@angular/router';
import { MediaIndexConfigsComponent } from './media-index-configs/media-index-configs.component';
import { MediaSearchComponent } from './media-search/media-search.component';
import { MediaDownloadComponent } from './media-download/media-download.component';
import { StorageGatewayConfigsComponent } from './storage-gateway-configs/storage-gateway-configs.component';
import { TorrentIndexConfigsComponent } from './torrent-index-configs/torrent-index-configs.component';

export const routes: Routes = [
    { path: 'media_index_configs', component: MediaIndexConfigsComponent },
    { path: 'media_search', component: MediaSearchComponent },
    { path: 'media_download/:imdbId', component: MediaDownloadComponent },
    { path: 'storage_gateway_configs', component: StorageGatewayConfigsComponent },
    { path: 'torrent_index_configs', component: TorrentIndexConfigsComponent }
];

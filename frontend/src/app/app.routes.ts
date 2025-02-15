import { Routes } from '@angular/router';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { MediaSearchComponent } from './components/media-search/media-search.component';
import { StorageComponent } from './components/storage/storage.component';
import { MediaComponent } from './components/media/media.component';
import { TorrentsComponent } from './components/torrents/torrents.component';
import { TorrentClientComponent } from './components/torrent-client/torrent-client.component';

export const routes: Routes = [
    { path: '', component: DashboardComponent },
    { path: 'search', component: MediaSearchComponent },
    { path: 'storage', component: StorageComponent },
    { path: 'media', component: MediaComponent },
    { path: 'torrents', component: TorrentsComponent },
    { path: 'torrent-client', component: TorrentClientComponent }
];

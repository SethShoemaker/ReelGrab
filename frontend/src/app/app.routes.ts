import { Routes } from '@angular/router';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { MediaSearchComponent } from './components/media-search/media-search.component';
import { DownloadOptionsComponent } from './components/download-options/download-options.component';
import { ConfigComponent } from './components/config/config.component';

export const routes: Routes = [
    { path: '', component: DashboardComponent },
    { path: 'search', component: MediaSearchComponent },
    { path: 'download/:id', component: DownloadOptionsComponent },
    { path: 'config', component: ConfigComponent }
];

import { Routes } from '@angular/router';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { MediaSearchComponent } from './components/movies-and-tv-search/media-search.component';
import { ConfigComponent } from './components/config/config.component';
import { MoviesDownloadComponent } from './components/movies-download/movies-download.component';
import { TvDownloadComponent } from './components/tv-download/tv-download.component';

export const routes: Routes = [
    { path: '', component: DashboardComponent },
    { path: 'search', component: MediaSearchComponent },
    { path: 'config', component: ConfigComponent },
    { path: 'movies/download/:imdbId', component: MoviesDownloadComponent },
    { path: 'tv/download/:imdbId', component: TvDownloadComponent }
];

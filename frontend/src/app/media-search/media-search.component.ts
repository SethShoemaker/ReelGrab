import { NgFor, NgIf } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnDestroy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-media-search',
  imports: [FormsModule, NgIf, NgFor, RouterLink],
  templateUrl: './media-search.component.html',
  styleUrl: './media-search.component.scss'
})
export class MediaSearchComponent implements OnDestroy {

  input = '';
  results: Array<any> | null = null;
  activeQuery: string|null = null;

  qSub: Subscription;
  searchSub: Subscription | null = null;

  constructor(private http: HttpClient, private router: Router, private route: ActivatedRoute) {
    if (this.route.snapshot.queryParams['q']) {
      this.input = this.route.snapshot.queryParams['q']
    }
    this.qSub = this.route.queryParams.subscribe((queryParams: any) => {
      const q = queryParams['q'] ?? '';
      if (q.length == 0) {
        this.activeQuery = null;
        this.results = null;
        return;
      } else {
        this.activeQuery = q;
        this.search();
      }
    })
  }

  onSearch() {
    this.router.navigate([], { queryParams: { q: this.input } })
  }

  search(){
    this.searchSub?.unsubscribe();
    this.searchSub = this.http.get(`/media_index/search?query=${this.activeQuery}`).subscribe((val: any) => this.results = val);
  }

  ngOnDestroy(): void {
    this.qSub.unsubscribe();
    this.searchSub?.unsubscribe();
  }
}

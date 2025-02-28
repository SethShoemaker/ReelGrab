import { Component, OnDestroy } from '@angular/core';
import { ApiService } from '../../services/api/api.service';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, Subscription } from 'rxjs';
import { NgFor, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-media-search',
  imports: [ReactiveFormsModule, NgIf, NgFor, RouterLink],
  templateUrl: './media-search.component.html',
  styleUrl: './media-search.component.scss'
})
export class MediaSearchComponent implements OnDestroy {

  searchControl = new FormControl()
  searchControlSub: Subscription;
  searchingSub: Subscription | null = null;
  searchResults: any;
  loading = false;

  constructor(public api: ApiService) {
    this.searchControlSub = this.searchControl.valueChanges
      .pipe(debounceTime(1000))
      .subscribe(value => {
        this.searchingSub?.unsubscribe();
        if (value.length == 0) {
          this.loading = false;
          this.searchResults = [];
          return;
        }
        this.loading = true;
        this.searchingSub = this.api.mediaSearch(value).subscribe(results => {
          this.searchResults = results;
          this.loading = false;
        })
      });
  }

  ngOnDestroy(): void {
    this.searchControlSub.unsubscribe()
    this.searchingSub?.unsubscribe()
  }
}

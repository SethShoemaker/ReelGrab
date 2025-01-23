import { KeyValuePipe, NgFor, NgIf } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, ElementRef } from '@angular/core';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-torrent-index-configs',
  imports: [NgFor, KeyValuePipe, NgIf],
  templateUrl: './torrent-index-configs.component.html',
  styleUrl: './torrent-index-configs.component.scss'
})
export class TorrentIndexConfigsComponent {
  configs: Map<string, string | null> | null = null;

  reloadConfigsSub: Subscription | null = null;
  saveConfigsSub: Subscription | null = null;

  constructor(private http: HttpClient, private eRef: ElementRef) {
    this.reloadConfigs();
  }

  reloadConfigs() {
    this.reloadConfigsSub?.unsubscribe();
    this.reloadConfigsSub = this.http.get('/torrent_index/config').subscribe({
      next: (val: any) => {
        this.configs = val
      }
    })
  }

  onSaveChanges() {
    const inputElems = (this.eRef.nativeElement as Element).querySelectorAll('input.form-control');
    const payload = {};
    for (let i = 0; i < inputElems.length; i++) {
      // @ts-ignore
      payload[inputElems[i].getAttribute("id")!] = (inputElems[i] as HTMLInputElement).value;
    }
    this.saveConfigsSub?.unsubscribe();
    this.saveConfigsSub = this.http.post('/torrent_index/config', payload).subscribe({
      next: (val: any) => {
        if (val.message && val.message == "Error while decoding config") {
          alert("Error while decoding config")
          return
        }
        this.configs = val;
        this.reloadConfigs();
      }
    })
  }

  ngOnDestroy(): void {
    this.reloadConfigsSub?.unsubscribe();
    this.saveConfigsSub?.unsubscribe();
  }
}

import { KeyValuePipe, NgFor, NgIf } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, ElementRef, OnDestroy } from '@angular/core';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-settings',
  imports: [NgIf, NgFor, KeyValuePipe],
  templateUrl: './media-index-configs.component.html',
  styleUrl: './media-index-configs.component.scss'
})
export class MediaIndexConfigsComponent implements OnDestroy {

  configs: Map<string, string|null>|null = null;
  databases: Array<{displayName: string}>|null = null;

  reloadConfigsSub: Subscription|null = null;
  reloadDatabasesSub: Subscription|null = null;
  saveConfigsSub: Subscription|null = null;

  constructor(private http: HttpClient, private eRef: ElementRef) {
    this.reloadConfigs()
    this.reloadDatabases()
  }

  reloadConfigs(){
    this.reloadConfigsSub?.unsubscribe();
    this.reloadConfigsSub = this.http.get('/media_index/config').subscribe({
      next: (val: any) => {
        this.configs = val
      }
    })
  }

  reloadDatabases(){
    this.reloadDatabasesSub?.unsubscribe();
    this.reloadDatabasesSub = this.http.get('/media_index/databases').subscribe({
      next: (val: any) => {
        this.databases = val;
      }
    })
  }

  onSaveChanges(){
    const inputElems = (this.eRef.nativeElement as Element).querySelectorAll('input.form-control');
    const payload = {};
    for (let i = 0; i < inputElems.length; i++) {
      // @ts-ignore
      payload[inputElems[i].getAttribute("id")!] = (inputElems[i] as HTMLInputElement).value;
    }
    this.saveConfigsSub?.unsubscribe();
    this.saveConfigsSub = this.http.post('/media_index/config', payload).subscribe({
      next: (val: any) => {
        if(val.message && val.message == "Error while decoding config"){
          alert("Error while decoding config")
          return
        }
        this.configs = val
        this.reloadDatabases()
      }
    })
  }

  ngOnDestroy(): void {
    this.reloadConfigsSub?.unsubscribe();
    this.reloadDatabasesSub?.unsubscribe();
    this.saveConfigsSub?.unsubscribe();
  }
}

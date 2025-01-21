import { KeyValuePipe, NgFor, NgIf } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, ElementRef, OnInit } from '@angular/core';

@Component({
  selector: 'app-settings',
  imports: [NgIf, NgFor, KeyValuePipe],
  templateUrl: './media_configs.component.html',
  styleUrl: './media_configs.component.scss'
})
export class MediaConfigsComponent implements OnInit {

  constructor(private http: HttpClient, private eRef: ElementRef) { }

  configs: Map<string, string|null>|null = null;
  databases: Array<{displayName: string}>|null = null;

  ngOnInit(): void {
    this.reloadConfigs()
    this.reloadDatabases()
  }

  reloadConfigs(){
    let sub = this.http.get('/media_index/config').subscribe({
      next: (val: any) => {
        this.configs = val
      },
      complete: () => sub.unsubscribe()
    })
  }

  reloadDatabases(){
    let sub = this.http.get('/media_index/databases').subscribe({
      next: (val: any) => {
        this.databases = val;
      },
      complete: () => sub.unsubscribe()
    })
  }

  onSaveChanges(){
    const inputElems = (this.eRef.nativeElement as Element).querySelectorAll('input.form-control');
    const payload = {};
    for (let i = 0; i < inputElems.length; i++) {
      // @ts-ignore
      payload[inputElems[i].getAttribute("id")!] = (inputElems[i] as HTMLInputElement).value;
    }
    let mediaIndexConfig = this.http.post('/media_index/config', payload).subscribe({
      next: (val: any) => {
        if(val.message && val.message == "Error while decoding config"){
          alert("Error while decoding config")
          return
        }
        this.configs = val
        this.reloadDatabases()
      },
      complete: () => mediaIndexConfig.unsubscribe()
    })
  }
}

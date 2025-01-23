import { KeyValuePipe, NgFor, NgIf } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, ElementRef } from '@angular/core';

@Component({
  selector: 'app-storage-location-configs',
  imports: [NgFor, KeyValuePipe, NgIf],
  templateUrl: './storage-location-configs.component.html',
  styleUrl: './storage-location-configs.component.scss'
})
export class StorageConfigsComponent {

  constructor(private http: HttpClient, private eRef: ElementRef) {
    this.reloadConfigs();
    this.reloadStorageLocations();
  }
  
  configs: Map<string, string|null>|null = null;
  storageLocations: Array<{displayName: string}>|null = null;

  reloadConfigs(){
    let sub = this.http.get('/storage_gateway/config').subscribe({
      next: (val: any) => {
        this.configs = val
      },
      complete: () => sub.unsubscribe()
    })
  }

  reloadStorageLocations(){
    let sub = this.http.get('/storage_gateway/storage_locations').subscribe({
      next: (val: any) => {
        this.storageLocations = val;
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
    let sub = this.http.post('/storage_gateway/config', payload).subscribe({
      next: (val: any) => {
        if(val.message && val.message == "Error while decoding config"){
          alert("Error while decoding config")
          return
        }
        this.configs = val;
        this.reloadConfigs();
        this.reloadStorageLocations();
      },
      complete: () => sub.unsubscribe()
    })
  }
}

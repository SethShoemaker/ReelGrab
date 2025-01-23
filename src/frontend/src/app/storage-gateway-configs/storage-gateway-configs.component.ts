import { KeyValuePipe, NgFor, NgIf } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, ElementRef, OnDestroy } from '@angular/core';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-storage-gateway-configs',
  imports: [NgFor, KeyValuePipe, NgIf],
  templateUrl: './storage-gateway-configs.component.html',
  styleUrl: './storage-gateway-configs.component.scss'
})
export class StorageGatewayConfigsComponent implements OnDestroy {

  configs: Map<string, string|null>|null = null;
  storageLocations: Array<{displayName: string}>|null = null;

  reloadConfigsSub: Subscription|null = null;
  reloadStorageLocationsSub: Subscription|null = null;
  saveConfigsSub: Subscription|null = null;

  constructor(private http: HttpClient, private eRef: ElementRef) {
    this.reloadConfigs();
    this.reloadStorageLocations();
  }

  reloadConfigs(){
    this.reloadConfigsSub?.unsubscribe();
    this.reloadConfigsSub = this.http.get('/storage_gateway/config').subscribe({
      next: (val: any) => {
        this.configs = val
      }
    })
  }

  reloadStorageLocations(){
    this.reloadStorageLocationsSub?.unsubscribe();
    this.reloadStorageLocationsSub = this.http.get('/storage_gateway/storage_locations').subscribe({
      next: (val: any) => {
        this.storageLocations = val;
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
    this.saveConfigsSub = this.http.post('/storage_gateway/config', payload).subscribe({
      next: (val: any) => {
        if(val.message && val.message == "Error while decoding config"){
          alert("Error while decoding config")
          return
        }
        this.configs = val;
        this.reloadConfigs();
        this.reloadStorageLocations();
      }
    })
  }

  ngOnDestroy(): void {
    this.reloadConfigsSub?.unsubscribe();
    this.reloadStorageLocationsSub?.unsubscribe();
    this.saveConfigsSub?.unsubscribe();
  }
}

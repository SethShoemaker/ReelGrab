import { Component, ElementRef, OnInit } from '@angular/core';
import { FormSectionHeaderComponent } from '../form-section-header/form-section-header.component';
import { FormSectionPartComponent } from '../form-section-part/form-section-part.component';
import { ApiService } from '../../services/api/api.service';
import { map, merge, Observable, Subject, switchMap } from 'rxjs';
import { AsyncPipe, JsonPipe, NgFor, NgIf } from '@angular/common';

@Component({
  selector: 'app-config',
  imports: [FormSectionHeaderComponent, FormSectionPartComponent, AsyncPipe, NgIf, NgFor, JsonPipe],
  templateUrl: './config.component.html',
  styleUrl: './config.component.scss'
})
export class ConfigComponent implements OnInit {

  constructor(public api: ApiService, public eRef: ElementRef) { }

  ngOnInit(): void {
    this.setupMediaIndexConfig();
    this.setupTorrentIndexConfig();
    this.setupStorageGatewayConfig();
  }

  mediaIndexConfigSubmit$ = new Subject<void>();
  mediaIndexConfig$!: Observable<Array<{ key: string, value: string, label: string, placeholder: string }>>;

  setupMediaIndexConfig() {
    this.mediaIndexConfig$ = merge(
      this.mediaIndexConfigSubmit$.pipe(
        switchMap(() => this.api.setMediaIndexConfig(this.getMediaIndexConfig()))
      ),
      this.api.getMediaIndexConfig()
    ).pipe(
      map(val => Object.entries(val)
        .map(([key, value]) => {
          const formInfo = this.convertMediaIndexConfigKeyToFormInfo(key)
          return {
            key: key,
            value: value,
            label: formInfo.label,
            placeholder: formInfo.placeholder
          }
        })
      )
    )
  }

  getMediaIndexConfig(): object {
    const config: any = {};
    const entries = Array.from(this.eRef.nativeElement.querySelectorAll('input.media-index-config-input')).map((input: any) => ({ key: input.name, value: input.value }))
    for (let i = 0; i < entries.length; i++) {
      const entry = entries[i];
      config[entry.key] = entry.value;
    }
    return config
  }

  convertMediaIndexConfigKeyToFormInfo(configKey: string): { label: string, placeholder: string } {
    if (configKey == "OMDb_API_Key") {
      return {
        label: "OMDb API Key",
        placeholder: "API key"
      }
    }
    return {
      label: configKey,
      placeholder: configKey
    }
  }

  torrentIndexConfigSubmit$ = new Subject<void>();
  torrentIndexConfig$!: Observable<Array<{ key: string, value: string, label: string, placeholder: string }>>;

  setupTorrentIndexConfig() {
    this.torrentIndexConfig$ = merge(
      this.torrentIndexConfigSubmit$.pipe(
        switchMap(() => this.api.setTorrentIndexConfig(this.getTorrentIndexConfig()))
      ),
      this.api.getTorrentIndexConfig()
    ).pipe(
      map(val => Object.entries(val)
        .map(([key, value]) => {
          const formInfo = this.convertTorrentIndexConfigKeyToFormInfo(key)
          return {
            key: key,
            value: value,
            label: formInfo.label,
            placeholder: formInfo.placeholder
          }
        })
      )
    )
  }

  getTorrentIndexConfig(): object {
    const config: any = {};
    const entries = Array.from(this.eRef.nativeElement.querySelectorAll('input.torrent-index-config-input')).map((input: any) => ({ key: input.name, value: input.value }))
    for (let i = 0; i < entries.length; i++) {
      const entry = entries[i];
      config[entry.key] = entry.value;
    }
    return config
  }

  convertTorrentIndexConfigKeyToFormInfo(configKey: string): { label: string, placeholder: string } {
    if (configKey == "API_URL") {
      return {
        label: "Jackett URL",
        placeholder: "URL"
      }
    }
    if(configKey == "API_KEY") {
      return {
        label: "Jackett API key",
        placeholder: "API key"
      }
    }
    return {
      label: configKey,
      placeholder: configKey
    }
  }

  storageGatewayConfigSubmit$ = new Subject<void>();
  storageGatewayConfig$!: Observable<Array<{ key: string, value: string, label: string, placeholder: string }>>;

  setupStorageGatewayConfig() {
    this.storageGatewayConfig$ = merge(
      this.storageGatewayConfigSubmit$.pipe(
        switchMap(() => this.api.setStorageGatewayConfig(this.getStorageGatewayConfig()))
      ),
      this.api.getStorageGatewayConfig()
    ).pipe(
      map(val => Object.entries(val)
        .map(([key, value]) => {
          const formInfo = this.convertStorageGatewayConfigKeyToFormInfo(key)
          return {
            key: key,
            value: value,
            label: formInfo.label,
            placeholder: formInfo.placeholder
          }
        })
      )
    )
  }

  getStorageGatewayConfig(): object {
    const config: any = {};
    const entries = Array.from(this.eRef.nativeElement.querySelectorAll('input.storage-gateway-config-input')).map((input: any) => ({ key: input.name, value: input.value }))
    for (let i = 0; i < entries.length; i++) {
      const entry = entries[i];
      config[entry.key] = entry.value;
    }
    return config
  }

  convertStorageGatewayConfigKeyToFormInfo(configKey: string): { label: string, placeholder: string } {
    if (configKey == "LOCAL_DISK_LOCATIONS") {
      return {
        label: "Local Disk Locations",
        placeholder: "Locations (comma seperated)"
      }
    }
    return {
      label: configKey,
      placeholder: configKey
    }
  }
}

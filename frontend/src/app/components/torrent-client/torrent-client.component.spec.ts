import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TorrentClientComponent } from './torrent-client.component';

describe('TorrentClientComponent', () => {
  let component: TorrentClientComponent;
  let fixture: ComponentFixture<TorrentClientComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TorrentClientComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TorrentClientComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

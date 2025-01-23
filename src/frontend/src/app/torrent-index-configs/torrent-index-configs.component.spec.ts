import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TorrentIndexConfigsComponent } from './torrent-index-configs.component';

describe('TorrentIndexConfigsComponent', () => {
  let component: TorrentIndexConfigsComponent;
  let fixture: ComponentFixture<TorrentIndexConfigsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TorrentIndexConfigsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TorrentIndexConfigsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

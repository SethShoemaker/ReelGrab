import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TvDownloadComponent } from './tv-download.component';

describe('TvDownloadComponent', () => {
  let component: TvDownloadComponent;
  let fixture: ComponentFixture<TvDownloadComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TvDownloadComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TvDownloadComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MoviesDownloadComponent } from './movies-download.component';

describe('MoviesDownloadComponent', () => {
  let component: MoviesDownloadComponent;
  let fixture: ComponentFixture<MoviesDownloadComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MoviesDownloadComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MoviesDownloadComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

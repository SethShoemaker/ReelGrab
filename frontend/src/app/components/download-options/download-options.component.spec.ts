import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DownloadOptionsComponent } from './download-options.component';

describe('DownloadOptionsComponent', () => {
  let component: DownloadOptionsComponent;
  let fixture: ComponentFixture<DownloadOptionsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DownloadOptionsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DownloadOptionsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MediaIndexConfigsComponent } from './media-index-configs.component';

describe('SettingsComponent', () => {
  let component: MediaIndexConfigsComponent;
  let fixture: ComponentFixture<MediaIndexConfigsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MediaIndexConfigsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MediaIndexConfigsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

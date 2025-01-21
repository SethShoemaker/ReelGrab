import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MediaConfigsComponent } from './media_configs.component';

describe('SettingsComponent', () => {
  let component: MediaConfigsComponent;
  let fixture: ComponentFixture<MediaConfigsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MediaConfigsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MediaConfigsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

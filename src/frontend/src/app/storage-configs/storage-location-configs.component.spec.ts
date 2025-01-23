import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StorageConfigsComponent } from './storage-location-configs.component';

describe('StorageLocationConfigsComponent', () => {
  let component: StorageConfigsComponent;
  let fixture: ComponentFixture<StorageConfigsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StorageConfigsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(StorageConfigsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

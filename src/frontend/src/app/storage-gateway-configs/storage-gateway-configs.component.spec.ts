import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StorageGatewayConfigsComponent } from './storage-gateway-configs.component';

describe('StorageLocationConfigsComponent', () => {
  let component: StorageGatewayConfigsComponent;
  let fixture: ComponentFixture<StorageGatewayConfigsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StorageGatewayConfigsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(StorageGatewayConfigsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

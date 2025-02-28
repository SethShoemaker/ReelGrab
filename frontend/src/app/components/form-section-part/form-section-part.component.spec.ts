import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FormSectionPartComponent } from './form-section-part.component';

describe('FormSectionPartComponent', () => {
  let component: FormSectionPartComponent;
  let fixture: ComponentFixture<FormSectionPartComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormSectionPartComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(FormSectionPartComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

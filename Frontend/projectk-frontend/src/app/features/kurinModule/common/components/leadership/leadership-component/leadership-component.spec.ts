import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LeadershipComponent } from './leadership-component';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

describe('LeadershipComponent', () => {
  let component: LeadershipComponent;
  let fixture: ComponentFixture<LeadershipComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LeadershipComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LeadershipComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

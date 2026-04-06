import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { PlanningService } from './planning-service';

describe('PlanningService', () => {
  let service: PlanningService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(PlanningService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});

import { TestBed } from '@angular/core/testing';

import { LeadershipService } from './leadership-service';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

describe('LeadershipService', () => {
  let service: LeadershipService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(LeadershipService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});

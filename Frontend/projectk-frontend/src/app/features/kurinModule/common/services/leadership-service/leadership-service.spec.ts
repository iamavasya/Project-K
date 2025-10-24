import { TestBed } from '@angular/core/testing';

import { LeadershipService } from './leadership-service';

describe('LeadershipService', () => {
  let service: LeadershipService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(LeadershipService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});

import { TestBed } from '@angular/core/testing';

import { KurinService } from './kurin.service';

describe('KurinService', () => {
  let service: KurinService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(KurinService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});

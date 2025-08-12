import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { KurinService } from './kurin.service';

describe('KurinService', () => {
  let service: KurinService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        KurinService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(KurinService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});

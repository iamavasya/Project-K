import { TestBed } from '@angular/core/testing';

import { BreadcrumbService } from './breadcrumb-service';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';

describe('BreadcrumbService', () => {
  let service: BreadcrumbService;

  beforeEach(() => {
    const activatedRouteMock = {
      snapshot: {
        paramMap: convertToParamMap({}),
        queryParamMap: convertToParamMap({}),
        data: {}
      },
      paramMap: of(convertToParamMap({})),
      queryParamMap: of(convertToParamMap({}))
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: ActivatedRoute, useValue: activatedRouteMock }
      ]
    });
    service = TestBed.inject(BreadcrumbService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});

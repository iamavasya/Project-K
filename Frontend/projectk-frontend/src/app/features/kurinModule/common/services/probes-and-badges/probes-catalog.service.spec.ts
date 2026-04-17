import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { GroupedProbeDto } from '../../models/probes-and-badges/groupedProbeDto';
import { ProbeSummaryDto } from '../../models/probes-and-badges/probeSummaryDto';
import { ProbesCatalogService } from './probes-catalog.service';

describe('ProbesCatalogService', () => {
  let service: ProbesCatalogService;
  let httpMock: HttpTestingController;

  const apiUrl = `${environment.apiUrl}/catalog/probes`;

  const probeSummary: ProbeSummaryDto = {
    id: 'probe-1',
    title: 'Перша проба',
    pointsCount: 10,
    sectionsCount: 2
  };

  const groupedProbe: GroupedProbeDto = {
    id: 'probe-1',
    title: 'Перша проба',
    pointsCount: 10,
    sectionsCount: 2,
    sections: [
      {
        id: 'section-a',
        code: 'A',
        title: 'Section A',
        points: [
          {
            id: 'point-1',
            title: 'Point 1'
          }
        ]
      }
    ]
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        ProbesCatalogService
      ]
    });

    service = TestBed.inject(ProbesCatalogService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getAll should call probes catalog endpoint', () => {
    service.getAll().subscribe(response => {
      expect(response).toEqual([probeSummary]);
    });

    const req = httpMock.expectOne(apiUrl);
    expect(req.request.method).toBe('GET');
    req.flush([probeSummary]);
  });

  it('getGroupedById should call grouped probe endpoint', () => {
    service.getGroupedById('probe-1').subscribe(response => {
      expect(response.id).toBe('probe-1');
      expect(response.sections.length).toBe(1);
    });

    const req = httpMock.expectOne(`${apiUrl}/probe-1/grouped`);
    expect(req.request.method).toBe('GET');
    req.flush(groupedProbe);
  });
});
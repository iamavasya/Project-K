import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { BadgeCatalogItemDto } from '../../models/probes-and-badges/badgeCatalogItemDto';
import { BadgesMetadataDto } from '../../models/probes-and-badges/badgesMetadataDto';
import { BadgesCatalogService } from './badges-catalog.service';

describe('BadgesCatalogService', () => {
  let service: BadgesCatalogService;
  let httpMock: HttpTestingController;

  const apiUrl = `${environment.apiUrl}/catalog/badges`;

  const metadata: BadgesMetadataDto = {
    parserVersion: '1.0.0',
    toolAuthor: 'Author',
    parserComment: 'ok',
    parsedAtUtc: '2026-04-01T00:00:00Z',
    sourceUrl: 'https://source',
    fixerEnabled: true,
    fixerMode: 'safe',
    totalBadges: 10
  };

  const badge: BadgeCatalogItemDto = {
    id: 'badge-1',
    title: 'Badge 1',
    imagePath: 'badges/1.png',
    country: 'UA',
    specialization: 'Scout',
    status: 'active',
    level: 1,
    lastUpdated: '2026-04-01',
    seekerRequirements: 'req',
    instructorRequirements: 'inst',
    fixNotes: []
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        BadgesCatalogService
      ]
    });

    service = TestBed.inject(BadgesCatalogService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getMetadata should call metadata endpoint', () => {
    service.getMetadata().subscribe(response => {
      expect(response).toEqual(metadata);
    });

    const req = httpMock.expectOne(`${apiUrl}/meta`);
    expect(req.request.method).toBe('GET');
    req.flush(metadata);
  });

  it('getAll should request badges with default take', () => {
    service.getAll().subscribe(response => {
      expect(response).toEqual([badge]);
    });

    const req = httpMock.expectOne(request => request.url === apiUrl && request.params.get('take') === '200');
    expect(req.request.method).toBe('GET');
    req.flush([badge]);
  });

  it('getAll should clamp take into supported range', () => {
    service.getAll(5000).subscribe();

    const req = httpMock.expectOne(request => request.url === apiUrl && request.params.get('take') === '1000');
    expect(req.request.params.get('take')).toBe('1000');
    req.flush([]);
  });

  it('getById should call badge details endpoint', () => {
    service.getById('badge-1').subscribe(response => {
      expect(response.id).toBe('badge-1');
    });

    const req = httpMock.expectOne(`${apiUrl}/badge-1`);
    expect(req.request.method).toBe('GET');
    req.flush(badge);
  });
});
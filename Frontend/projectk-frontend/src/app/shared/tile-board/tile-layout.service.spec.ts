import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient, withXhr } from '@angular/common/http';
import { TileLayoutService } from './tile-layout.service';
import { environment } from '../../../environments/environment';

describe('TileLayoutService', () => {
  let service: TileLayoutService;
  let httpMock: HttpTestingController;

  const apiUrl = `${environment.apiUrl}/user/me/layouts`;

  const sampleLayouts = [
    { boardKey: 'member-card', tileKeys: ['profile', 'skills', 'probes'], schemaVersion: 1, updatedAtUtc: '2026-07-23T00:00:00Z' }
  ];

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withXhr()), provideHttpClientTesting(), TileLayoutService]
    });
    service = TestBed.inject(TileLayoutService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('getOrder returns the matching board keys', () => {
    const received: (string[] | null)[] = [];
    service.getOrder('member-card').subscribe(result => received.push(result));

    httpMock.expectOne(apiUrl).flush(sampleLayouts);

    expect(received[0]).toEqual(['profile', 'skills', 'probes']);
  });

  it('getOrder returns null when board is not present', () => {
    const received: (string[] | null)[] = [];
    service.getOrder('kurin-panel').subscribe(result => received.push(result));

    httpMock.expectOne(apiUrl).flush(sampleLayouts);

    expect(received[0]).toBeNull();
  });

  it('getOrder reuses the cached response within TTL (single HTTP call)', () => {
    service.getOrder('member-card').subscribe();
    httpMock.expectOne(apiUrl).flush(sampleLayouts);

    service.getOrder('member-card').subscribe();
    httpMock.expectNone(apiUrl);
  });

  it('getOrder mirrors resolved order into localStorage', () => {
    service.getOrder('member-card').subscribe();
    httpMock.expectOne(apiUrl).flush(sampleLayouts);

    expect(service.readCachedOrder('member-card')).toEqual(['profile', 'skills', 'probes']);
  });

  it('saveOrder PUTs the order and invalidates the cache', () => {
    service.getOrder('member-card').subscribe();
    httpMock.expectOne(apiUrl).flush(sampleLayouts);

    service.saveOrder('member-card', ['probes', 'profile', 'skills']).subscribe();
    const put = httpMock.expectOne(`${apiUrl}/member-card`);
    expect(put.request.method).toBe('PUT');
    expect(put.request.body.tileKeys).toEqual(['probes', 'profile', 'skills']);
    put.flush({ boardKey: 'member-card', tileKeys: ['probes', 'profile', 'skills'], schemaVersion: 1, updatedAtUtc: '' });

    service.getOrder('member-card').subscribe();
    httpMock.expectOne(apiUrl).flush(sampleLayouts);
  });

  it('saveOrder writes the order to localStorage immediately (optimistic)', () => {
    service.saveOrder('member-card', ['skills', 'profile']).subscribe();
    expect(service.readCachedOrder('member-card')).toEqual(['skills', 'profile']);
    httpMock.expectOne(`${apiUrl}/member-card`).flush({});
  });

  it('resetOrder DELETEs, clears storage and invalidates the cache', () => {
    service.saveOrder('member-card', ['skills', 'profile']).subscribe();
    httpMock.expectOne(`${apiUrl}/member-card`).flush({});
    expect(service.readCachedOrder('member-card')).not.toBeNull();

    service.resetOrder('member-card').subscribe();
    const del = httpMock.expectOne(`${apiUrl}/member-card`);
    expect(del.request.method).toBe('DELETE');
    del.flush(null);

    expect(service.readCachedOrder('member-card')).toBeNull();
  });
});

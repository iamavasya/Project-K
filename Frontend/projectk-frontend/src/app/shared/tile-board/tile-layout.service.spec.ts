import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TileLayoutService } from './tile-layout.service';
import { environment } from '../../../environments/environment';
import { TileLayout } from './tile-board.models';

describe('TileLayoutService', () => {
  let service: TileLayoutService;
  let httpMock: HttpTestingController;

  const apiUrl = `${environment.apiUrl}/user/me/layouts`;

  const sampleLayouts = [
    { boardKey: 'member-card', tileKeys: ['profile', 'skills', 'probes'], hiddenTileKeys: ['probes'], schemaVersion: 1, updatedAtUtc: '2026-07-23T00:00:00Z' }
  ];

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), TileLayoutService]
    });
    service = TestBed.inject(TileLayoutService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('getLayout returns the matching board, order and hidden apart', () => {
    const received: (TileLayout | null)[] = [];
    service.getLayout('member-card').subscribe(result => received.push(result));

    httpMock.expectOne(apiUrl).flush(sampleLayouts);

    expect(received[0]).toEqual({ tileKeys: ['profile', 'skills', 'probes'], hiddenTileKeys: ['probes'] });
  });

  it('getLayout returns null when board is not present', () => {
    const received: (TileLayout | null)[] = [];
    service.getLayout('kurin-panel').subscribe(result => received.push(result));

    httpMock.expectOne(apiUrl).flush(sampleLayouts);

    expect(received[0]).toBeNull();
  });

  it('getLayout reuses the cached response within TTL (single HTTP call)', () => {
    service.getLayout('member-card').subscribe();
    httpMock.expectOne(apiUrl).flush(sampleLayouts);

    let second: unknown = null;
    service.getLayout('member-card').subscribe(layout => (second = layout));
    httpMock.expectNone(apiUrl);
    expect(second).toEqual({ tileKeys: ['profile', 'skills', 'probes'], hiddenTileKeys: ['probes'] });
  });

  it('getLayout mirrors the resolved layout into localStorage', () => {
    service.getLayout('member-card').subscribe();
    httpMock.expectOne(apiUrl).flush(sampleLayouts);

    expect(service.readCachedLayout('member-card')).toEqual({ tileKeys: ['profile', 'skills', 'probes'], hiddenTileKeys: ['probes'] });
  });

  // Older builds stored the order alone; it still reads, with nothing hidden.
  it('readCachedLayout understands a stored plain order', () => {
    localStorage.setItem('tile-layout:member-card', JSON.stringify(['skills', 'profile']));

    expect(service.readCachedLayout('member-card')).toEqual({ tileKeys: ['skills', 'profile'], hiddenTileKeys: [] });
  });

  it('saveLayout PUTs order and hidden and invalidates the cache', () => {
    service.getLayout('member-card').subscribe();
    httpMock.expectOne(apiUrl).flush(sampleLayouts);

    service.saveLayout('member-card', { tileKeys: ['probes', 'profile', 'skills'], hiddenTileKeys: ['skills'] }).subscribe();
    const put = httpMock.expectOne(`${apiUrl}/member-card`);
    expect(put.request.method).toBe('PUT');
    expect(put.request.body.tileKeys).toEqual(['probes', 'profile', 'skills']);
    expect(put.request.body.hiddenTileKeys).toEqual(['skills']);
    put.flush({ boardKey: 'member-card', tileKeys: ['probes', 'profile', 'skills'], hiddenTileKeys: ['skills'], schemaVersion: 1, updatedAtUtc: '' });

    service.getLayout('member-card').subscribe();
    httpMock.expectOne(apiUrl).flush(sampleLayouts);
  });

  it('saveLayout writes to localStorage immediately (optimistic)', () => {
    service.saveLayout('member-card', { tileKeys: ['skills', 'profile'], hiddenTileKeys: [] }).subscribe();
    expect(service.readCachedLayout('member-card')?.tileKeys).toEqual(['skills', 'profile']);
    httpMock.expectOne(`${apiUrl}/member-card`).flush({});
  });

  it('resetLayout DELETEs, clears storage and invalidates the cache', () => {
    service.saveLayout('member-card', { tileKeys: ['skills', 'profile'], hiddenTileKeys: [] }).subscribe();
    httpMock.expectOne(`${apiUrl}/member-card`).flush({});
    expect(service.readCachedLayout('member-card')).not.toBeNull();

    service.resetLayout('member-card').subscribe();
    const del = httpMock.expectOne(`${apiUrl}/member-card`);
    expect(del.request.method).toBe('DELETE');
    del.flush(null);

    expect(service.readCachedLayout('member-card')).toBeNull();
  });
});

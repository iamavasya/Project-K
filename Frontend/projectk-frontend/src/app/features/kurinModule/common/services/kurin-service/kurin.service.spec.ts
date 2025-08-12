import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { KurinService } from './kurin.service';
import { environment } from '../../environments/environment';
import { KurinDto } from '../../models/kurinDto';

describe('KurinService', () => {
  let service: KurinService;
  let httpMock: HttpTestingController;

  const baseUrl = `${environment.apiUrl}/kurin`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        KurinService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(KurinService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getKurins should call GET /kurins and return data', () => {
    const mockKurins = [
      { kurinKey: 'k1', number: 1 },
      { kurinKey: 'k2', number: 2 }
    ] as KurinDto[];

    let result: KurinDto[] | undefined;
    service.getKurins().subscribe(res => (result = res));

    const req = httpMock.expectOne(`${baseUrl}/kurins`);
    expect(req.request.method).toBe('GET');
    req.flush(mockKurins);

    expect(result).toEqual(mockKurins);
  });

  it('createKurin should POST to /kurin with kurin.number and return created entity', () => {
    const input = { kurinKey: 'k1', number: 3 } as KurinDto;
    const created = { kurinKey: 'k1', number: 3 } as KurinDto;

    let result: KurinDto | undefined;
    service.createKurin(input).subscribe(res => (result = res));

    const req = httpMock.expectOne(`${baseUrl}`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBe(input.number);
    expect(req.request.headers.get('Content-Type')).toBe('application/json');

    req.flush(created);

    expect(result).toEqual(created);
  });

  it('updateKurin should PUT to /kurin/:kurinKey with kurin.number and return updated entity', () => {
    const input = { kurinKey: 'k1', number: 5 } as KurinDto;
    const updated = { kurinKey: 'k1', number: 5 } as KurinDto;

    let result: KurinDto | undefined;
    service.updateKurin(input).subscribe(res => (result = res));

    const req = httpMock.expectOne(`${baseUrl}/${input.kurinKey}`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toBe(input.number);
    expect(req.request.headers.get('Content-Type')).toBe('application/json');

    req.flush(updated);

    expect(result).toEqual(updated);
  });

  it('deleteKurin should call DELETE /kurin/:kurinKey', () => {
    const kurinKey = 'k1';

    let completed = false;
    service.deleteKurin(kurinKey).subscribe(() => (completed = true));

    const req = httpMock.expectOne(`${baseUrl}/${kurinKey}`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);

    expect(completed).toBeTrue();
  });
});

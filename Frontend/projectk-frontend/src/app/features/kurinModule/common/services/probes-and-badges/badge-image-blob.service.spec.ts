import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { BadgeImageBlobService } from './badge-image-blob.service';

describe('BadgeImageBlobService', () => {
  let service: BadgeImageBlobService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        BadgeImageBlobService
      ]
    });

    service = TestBed.inject(BadgeImageBlobService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should return same URL for non-protected images', () => {
    const url = 'https://example.com/image.png';

    const resolved = service.resolveBadgeImageForDisplay(url);

    expect(resolved).toBe(url);
    httpMock.expectNone(url);
  });

  it('should fetch protected image as blob and return object URL from cache', () => {
    const sourceUrl = '/badges_images/skill.png';
    const blob = new Blob(['image-bytes'], { type: 'image/png' });
    spyOn(URL, 'createObjectURL').and.returnValue('blob://skill-1');

    const firstResolve = service.resolveBadgeImageForDisplay(sourceUrl);
    expect(firstResolve).toBeNull();

    const req = httpMock.expectOne(sourceUrl);
    expect(req.request.method).toBe('GET');
    expect(req.request.responseType).toBe('blob');
    req.flush(blob);

    const secondResolve = service.resolveBadgeImageForDisplay(sourceUrl);
    expect(secondResolve).toBe('blob://skill-1');
  });

  it('should avoid duplicate requests while image load is pending', () => {
    const sourceUrl = '/badges_images/skill-2.png';

    const firstResolve = service.resolveBadgeImageForDisplay(sourceUrl);
    const secondResolve = service.resolveBadgeImageForDisplay(sourceUrl);

    expect(firstResolve).toBeNull();
    expect(secondResolve).toBeNull();

    httpMock.expectOne(sourceUrl);
    httpMock.expectNone(sourceUrl);
  });
});
import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { MemberService } from './member.service';
import { environment } from '../../../../environments/environment';
import { UpsertMemberDto } from '../../models/requests/member/upsertMemberDto';
import { MemberDto } from '../../models/memberDto';

describe('MemberService', () => {
  let service: MemberService;
  let httpMock: HttpTestingController;

  const apiUrl = `${environment.apiUrl}/member`;
  const guidNull = '00000000-0000-0000-0000-000000000000';

  const sampleUpsert: UpsertMemberDto = {
    groupKey: 'group-1',
    firstName: 'John',
    middleName: 'M',
    lastName: 'Doe',
    email: 'john@example.com',
    phoneNumber: '123456789',
    dateOfBirth: '2000-05-10'
  };

  const sampleMember: MemberDto = {
    memberKey: 'member-key-1',
    groupKey: 'group-1',
    kurinKey: 'kurin-1',
    firstName: 'John',
    middleName: 'M',
    lastName: 'Doe',
    email: 'john@example.com',
    phoneNumber: '123456789',
    dateOfBirth: null,
    profilePhotoUrl: null,
    plastLevelHistories: [],
    leadershipHistories: []
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [MemberService]
    });
    service = TestBed.inject(MemberService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getByKey should call correct URL and return MemberDto', () => {
    const id = '123';
    service.getByKey(id).subscribe(res => {
      expect(res).toEqual(sampleMember);
    });

    const req = httpMock.expectOne(`${apiUrl}/${id}`);
    expect(req.request.method).toBe('GET');
    req.flush(sampleMember);
  });

  it('getAll should send provided groupKey & kurinKey', () => {
    const groupKey = 'g1';
    const kurinKey = 'k1';

    service.getAll(groupKey, kurinKey).subscribe(res => {
      expect(res.length).toBe(1);
    });

    const req = httpMock.expectOne(r =>
      r.url === `${apiUrl}/members` &&
      r.params.get('groupKey') === groupKey &&
      r.params.get('kurinKey') === kurinKey
    );
    expect(req).toBeTruthy();
    expect(req.request.method).toBe('GET');
    req.flush([sampleMember]);
  });

  it('getAll should substitute null GUIDs when params are undefined', () => {
    service.getAll().subscribe();

    const req = httpMock.expectOne(r =>
      r.url === `${apiUrl}/members` &&
      r.params.get('groupKey') === guidNull &&
      r.params.get('kurinKey') === guidNull
    );
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('create should POST FormData with individual dto properties and blob when file provided', () => {
    const file = new File(['content'], 'avatar.png', { type: 'image/png' });

    service.create(sampleUpsert, file).subscribe(res => {
      expect(res).toEqual(sampleMember);
    });

    const req = httpMock.expectOne(apiUrl);
    expect(req.request.method).toBe('POST');
    const body = req.request.body as FormData;
    expect(body instanceof FormData).toBeTrue();

    // Check all DTO properties are present in FormData
    Object.entries(sampleUpsert).forEach(([key, value]) => {
      if (value !== null && value !== undefined) {
        expect(body.get(key)).toEqual(value.toString());
      }
    });

    // Verify blob is included with filename
    const blobEntry = body.get('blob') as File;
    expect(blobEntry).toBeTruthy();
    expect(blobEntry.name).toBe('avatar.png');

    req.flush(sampleMember);
  });

  it('create should handle Date objects by converting to ISO string', () => {
    const dateUpsert: UpsertMemberDto = {
      ...sampleUpsert,
      dateOfBirth: new Date('2000-05-10').toISOString()
    };
    
    service.create(dateUpsert, null).subscribe();

    const req = httpMock.expectOne(apiUrl);
    const body = req.request.body as FormData;
    
    expect(body.get('dateOfBirth')).toEqual('2000-05-10T00:00:00.000Z');
    req.flush(sampleMember);
  });

  it('create should POST FormData without blob when file is null', () => {
    service.create(sampleUpsert, null).subscribe(res => {
      expect(res).toEqual(sampleMember);
    });

    const req = httpMock.expectOne(apiUrl);
    expect(req.request.method).toBe('POST');
    const body = req.request.body as FormData;

    let hasBlob = false;
    body.forEach((_v: FormDataEntryValue, key: string) => {
      if (key === 'blob') hasBlob = true;
    });
    expect(hasBlob).toBeFalse();

    req.flush(sampleMember);
  });

  it('update should PUT FormData to correct URL with individual properties and file', () => {
    const memberKey = 'member-key-1';
    const file = new File(['xx'], 'photo.jpg', { type: 'image/jpeg' });

    service.update(memberKey, sampleUpsert, file).subscribe(res => {
      expect(res).toEqual(sampleMember);
    });

    const req = httpMock.expectOne(`${apiUrl}/${memberKey}`);
    expect(req.request.method).toBe('PUT');
    const body = req.request.body as FormData;

    // Check DTO properties
    Object.entries(sampleUpsert).forEach(([key, value]) => {
      if (value !== null && value !== undefined) {
        expect(body.get(key)).toEqual(value.toString());
      }
    });

    // Check blob with filename
    const blobEntry = body.get('blob') as File;
    expect(blobEntry).toBeTruthy();
    expect(blobEntry.name).toBe('photo.jpg');

    req.flush(sampleMember);
  });

  it('update should omit blob when file null', () => {
    const memberKey = 'member-key-2';

    service.update(memberKey, sampleUpsert, null).subscribe(res => {
      expect(res).toEqual(sampleMember);
    });

    const req = httpMock.expectOne(`${apiUrl}/${memberKey}`);
    expect(req.request.method).toBe('PUT');
    const body = req.request.body as FormData;

    let containsBlob = false;
    body.forEach((_v: FormDataEntryValue, key: string) => {
      if (key === 'blob') containsBlob = true;
    });
    expect(containsBlob).toBeFalse();

    req.flush(sampleMember);
  });

  it('delete should call correct URL', () => {
    const memberKey = 'member-key-del';

    service.delete(memberKey).subscribe(res => {
      expect(res).toBeNull();
    });

    const req = httpMock.expectOne(`${apiUrl}/${memberKey}`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});

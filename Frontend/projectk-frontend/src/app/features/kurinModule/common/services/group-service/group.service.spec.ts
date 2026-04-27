import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';

import { GroupService } from './group.service';
import { CreateGroupDto } from '../../models/requests/createGroupDto';
import { UpdateGroupDto } from '../../models/requests/updateGroupDto';
import { GroupDto } from '../../models/groupDto';
import { environment } from '../../../../../../environments/environment';
import { MemberLookupDto } from '../../models/requests/member/memberLookupDto';

describe('GroupService', () => {
  let service: GroupService;
  let httpMock: HttpTestingController;

  const baseUrl = `${environment.apiUrl}/group`;

  const mockGroup: GroupDto = {
    groupKey: 'g1',
    name: 'Alpha',
    kurinKey: 'k1',
    kurinNumber: 10
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [GroupService]
    });
    service = TestBed.inject(GroupService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getByKey sends GET to /group/:key', () => {
    service.getByKey('g1').subscribe(res => {
      expect(res).toEqual(mockGroup);
    });

    const req = httpMock.expectOne(`${baseUrl}/g1`);
    expect(req.request.method).toBe('GET');
    req.flush(mockGroup);
  });

  it('getAllByKurinKey sends GET with kurinKey param', () => {
    service.getAllByKurinKey('k1').subscribe(res => {
      expect(res).toEqual([mockGroup]);
    });

    const req = httpMock.expectOne(r =>
      r.url === `${baseUrl}/groups` && r.params.get('kurinKey') === 'k1'
    );
    expect(req.request.method).toBe('GET');
    req.flush([mockGroup]);
  });

  it('create sends POST with body', () => {
    const dto: CreateGroupDto = { name: 'Alpha', kurinKey: 'k1' };

    service.create(dto).subscribe(res => {
      expect(res).toEqual(mockGroup);
    });

    const req = httpMock.expectOne(baseUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(dto);
    req.flush(mockGroup);
  });

  it('update sends PUT with body to /group/:key', () => {
    const updateDto: UpdateGroupDto = { name: 'Alpha Updated' };

    service.update('g1', updateDto).subscribe(res => {
      expect(res).toEqual({ ...mockGroup, name: 'Alpha Updated' });
    });

    const req = httpMock.expectOne(`${baseUrl}/g1`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(updateDto);
    req.flush({ ...mockGroup, name: 'Alpha Updated' });
  });

  it('delete sends DELETE to /group/:key', () => {
    service.delete('g1').subscribe(res => {
      expect(res).toBeNull();
    });

    const req = httpMock.expectOne(`${baseUrl}/g1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('getMentors sends GET to /group/:key/mentors', () => {
    const mentors: MemberLookupDto[] = [
      { memberKey: 'm1', userKey: 'u1', firstName: 'A', middleName: 'B', lastName: 'C' }
    ];

    service.getMentors('g1').subscribe(res => {
      expect(res).toEqual(mentors);
    });

    const req = httpMock.expectOne(`${baseUrl}/g1/mentors`);
    expect(req.request.method).toBe('GET');
    req.flush(mentors);
  });

  it('assignMentor sends POST to /group/:key/mentors/:mentorUserKey', () => {
    service.assignMentor('g1', 'u1').subscribe();

    const req = httpMock.expectOne(`${baseUrl}/g1/mentors/u1`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeNull();
    req.flush({});
  });

  it('revokeMentor sends DELETE to /group/:key/mentors/:mentorUserKey', () => {
    service.revokeMentor('g1', 'u1').subscribe();

    const req = httpMock.expectOne(`${baseUrl}/g1/mentors/u1`);
    expect(req.request.method).toBe('DELETE');
    req.flush({});
  });
});


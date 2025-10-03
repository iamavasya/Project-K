import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { UserService } from './user.service';
import { UserDto } from '../models/userDto';
import { environment } from '../../environments/environment';

describe('UserService', () => {
  let service: UserService;
  let httpMock: HttpTestingController;
  const apiUrl = environment.apiUrl;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        UserService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(UserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getAllUsers', () => {
    const mockUsers: UserDto[] = [
      {
        userId: 'user-1',
        email: 'user1@example.com',
        firstName: 'John',
        lastName: 'Doe',
        role: 'Manager',
        kurinKey: 'kurin-1'
      },
      {
        userId: 'user-2',
        email: 'user2@example.com',
        firstName: 'Jane',
        lastName: 'Smith',
        role: 'Admin',
        kurinKey: null
      },
      {
        userId: 'user-3',
        email: 'user3@example.com',
        firstName: 'Bob',
        lastName: 'Johnson',
        role: 'Mentor',
        kurinKey: 'kurin-2'
      }
    ];

    it('should send GET request to correct endpoint', () => {
      service.getAllUsers().subscribe();

      const req = httpMock.expectOne(`${apiUrl}/user/users`);
      expect(req.request.method).toBe('GET');
      req.flush([]);
    });

    it('should include credentials in request', () => {
      service.getAllUsers().subscribe();

      const req = httpMock.expectOne(`${apiUrl}/user/users`);
      expect(req.request.withCredentials).toBeTrue();
      req.flush([]);
    });

    it('should return array of users', (done) => {
      service.getAllUsers().subscribe(users => {
        expect(users).toEqual(mockUsers);
        expect(users.length).toBe(3);
        done();
      });

      const req = httpMock.expectOne(`${apiUrl}/user/users`);
      req.flush(mockUsers);
    });

    it('should return empty array when no users exist', (done) => {
      service.getAllUsers().subscribe(users => {
        expect(users).toEqual([]);
        expect(users.length).toBe(0);
        done();
      });

      const req = httpMock.expectOne(`${apiUrl}/user/users`);
      req.flush([]);
    });

    it('should handle users with different roles', (done) => {
      service.getAllUsers().subscribe(users => {
        expect(users[0].role).toBe('Manager');
        expect(users[1].role).toBe('Admin');
        expect(users[2].role).toBe('Mentor');
        done();
      });

      const req = httpMock.expectOne(`${apiUrl}/user/users`);
      req.flush(mockUsers);
    });

    it('should handle users with null kurinKey', (done) => {
      service.getAllUsers().subscribe(users => {
        expect(users[1].kurinKey).toBeNull();
        expect(users[0].kurinKey).toBe('kurin-1');
        done();
      });

      const req = httpMock.expectOne(`${apiUrl}/user/users`);
      req.flush(mockUsers);
    });

    it('should handle 401 unauthorized error', (done) => {
      service.getAllUsers().subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          expect(error.status).toBe(401);
          expect(error.statusText).toBe('Unauthorized');
          done();
        }
      });

      const req = httpMock.expectOne(`${apiUrl}/user/users`);
      req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
    });

    it('should handle 403 forbidden error', (done) => {
      service.getAllUsers().subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          expect(error.status).toBe(403);
          expect(error.statusText).toBe('Forbidden');
          done();
        }
      });

      const req = httpMock.expectOne(`${apiUrl}/user/users`);
      req.flush('Forbidden', { status: 403, statusText: 'Forbidden' });
    });

    it('should handle 500 internal server error', (done) => {
      service.getAllUsers().subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          expect(error.status).toBe(500);
          expect(error.statusText).toBe('Internal Server Error');
          done();
        }
      });

      const req = httpMock.expectOne(`${apiUrl}/user/users`);
      req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
    });

    it('should handle network error', (done) => {
      service.getAllUsers().subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          expect(error.error).toBeInstanceOf(ProgressEvent);
          done();
        }
      });

      const req = httpMock.expectOne(`${apiUrl}/user/users`);
      req.error(new ProgressEvent('error'));
    });

    it('should return users with correct data structure', (done) => {
      service.getAllUsers().subscribe(users => {
        const user = users[0];
        expect(user.userId).toBeDefined();
        expect(user.email).toBeDefined();
        expect(user.firstName).toBeDefined();
        expect(user.lastName).toBeDefined();
        expect(user.role).toBeDefined();
        expect('kurinKey' in user).toBeTrue();
        done();
      });

      const req = httpMock.expectOne(`${apiUrl}/user/users`);
      req.flush(mockUsers);
    });

    it('should handle single user in array', (done) => {
      const singleUser: UserDto[] = [{
        userId: 'user-1',
        email: 'single@example.com',
        firstName: 'Single',
        lastName: 'User',
        role: 'Manager',
        kurinKey: 'kurin-1'
      }];

      service.getAllUsers().subscribe(users => {
        expect(users.length).toBe(1);
        expect(users[0].email).toBe('single@example.com');
        done();
      });

      const req = httpMock.expectOne(`${apiUrl}/user/users`);
      req.flush(singleUser);
    });

    it('should handle users with special characters in names', (done) => {
      const specialUsers: UserDto[] = [{
        userId: 'user-1',
        email: 'test@example.com',
        firstName: "O'Brien",
        lastName: 'Müller-Schmidt',
        role: 'Manager',
        kurinKey: null
      }];

      service.getAllUsers().subscribe(users => {
        expect(users[0].firstName).toBe("O'Brien");
        expect(users[0].lastName).toBe('Müller-Schmidt');
        done();
      });

      const req = httpMock.expectOne(`${apiUrl}/user/users`);
      req.flush(specialUsers);
    });

    it('should make only one HTTP request', () => {
      service.getAllUsers().subscribe();

      const requests = httpMock.match(`${apiUrl}/user/users`);
      expect(requests.length).toBe(1);
      requests[0].flush([]);
    });

    it('should handle multiple subscribers to same observable', (done) => {
      const observable = service.getAllUsers();
      let callCount = 0;

      observable.subscribe(users => {
        expect(users).toEqual(mockUsers);
        callCount++;
      });

      observable.subscribe(users => {
        expect(users).toEqual(mockUsers);
        callCount++;
        if (callCount === 2) {
          done();
        }
      });

      // Should make two separate requests for two subscriptions
      const requests = httpMock.match(`${apiUrl}/user/users`);
      expect(requests.length).toBe(2);
      requests[0].flush(mockUsers);
      requests[1].flush(mockUsers);
    });
  });
});
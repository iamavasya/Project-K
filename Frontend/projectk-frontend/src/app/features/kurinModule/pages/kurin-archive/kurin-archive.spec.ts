import type { MockedObject } from 'vitest';
import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { ConfirmationService, MessageService } from '@openng/optimus-ui/api';
import { of, throwError } from 'rxjs';
import { archiveDate, KurinArchiveComponent } from './kurin-archive';
import { KurinArchiveService } from '../../services/kurin-archive-service/kurin-archive.service';
import { KurinFeaturesService } from '../../services/kurin-features-service/kurin-features.service';
import { KurinArchiveDto } from '../../models/kurin-archive.dto';
import { LeadershipRole } from '../../models/enums/leadership-role.enum';
import { MembershipKind } from '../../models/enums/membership-kind.enum';

describe('KurinArchiveComponent', () => {
  let fixture: ComponentFixture<KurinArchiveComponent>;
  let component: KurinArchiveComponent;
  let service: MockedObject<KurinArchiveService>;
  const hasGroups = signal(true);

  function archive(overrides: Partial<KurinArchiveDto> = {}): KurinArchiveDto {
    return {
      offices: [
        {
          historyKey: 'h1', type: 'Kurin', role: LeadershipRole.Pysar, memberKey: 'm1', memberName: 'Ірина Мельник',
          startDate: '2021-09-01', endDate: '2023-08-31', groupKey: null, groupName: null, groupDissolved: false, canCorrect: true, isOwn: false
        },
        {
          historyKey: 'h2', type: 'KV', role: LeadershipRole.Zvyazkovyi, memberKey: 'me', memberName: 'Я',
          startDate: '2024-09-01', endDate: null, groupKey: null, groupName: null, groupDissolved: false, canCorrect: false, isOwn: true
        }
      ],
      memberships: [
        {
          membershipKey: 'ms1', memberKey: 'm2', memberName: 'Марко Бойко', kind: MembershipKind.Youth,
          joinedOn: '2017-09-12', leftOn: '2023-06-30',
          groups: [{ groupKey: 'g1', groupName: 'Беркути', from: null, to: '2025-01-01' }, { groupKey: 'g2', groupName: 'Соколи', from: '2025-01-01', to: '2025-06-30' }],
          canCorrect: true, isOwn: false
        }
      ],
      dissolvedGroups: [{ groupKey: 'g1', name: 'Беркути', dissolvedOn: '2025-01-01' }],
      canDelete: true,
      ...overrides
    };
  }

  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';

  beforeEach(async () => {
    service = {
      get: vi.fn(),
      correctOffice: vi.fn(),
      correctMembership: vi.fn(),
      enterDangerZone: vi.fn(),
      endDangerZone: vi.fn(),
      endDangerZoneOnUnload: vi.fn(),
      deleteOffice: vi.fn(),
      deleteMembership: vi.fn(),
      deleteGroup: vi.fn()
    } as unknown as MockedObject<KurinArchiveService>;
    service.get.mockReturnValue(of(archive()));
    service.endDangerZone.mockReturnValue(of(undefined));
    hasGroups.set(true);

    await TestBed.configureTestingModule({
      imports: [KurinArchiveComponent],
      providers: [
        provideRouter([]),
        MessageService,
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ kurinKey: 'k1' }) } } },
        { provide: KurinArchiveService, useValue: service },
        { provide: KurinFeaturesService, useValue: { hasGroups, branch: signal(null) } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(KurinArchiveComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('reads dates as text, without a time zone to shift them', () => {
    expect(archiveDate('2023-08-31')).toBe('31.08.2023');
    expect(archiveDate(null)).toBe('');
  });

  it('shows former holdings by default and current ones on «Усі»', () => {
    expect(component.offices().map(office => office.historyKey)).toEqual(['h1']);
    component.officeFilter.set('all');
    expect(component.offices().map(office => office.historyKey)).toEqual(['h1', 'h2']);
  });

  it('lists the гуртки a membership passed through', () => {
    expect(text()).toContain('Беркути → Соколи');
  });

  it('has no гуртки column or tile in a senior kurin', () => {
    hasGroups.set(false);
    fixture.detectChanges();
    expect(text()).not.toContain('Розформовані гуртки');
    expect(text()).not.toContain('Беркути → Соколи');
  });

  it('offers deletion only inside the danger zone, and never on one\'s own record', () => {
    const own = archive().offices[1];
    const former = archive().offices[0];
    expect(component.canDeleteOffice(former)).toBe(false);

    service.enterDangerZone.mockReturnValue(of({ ticket: 't', expiresAtUtc: new Date(Date.now() + 600_000).toISOString(), deletionsLeft: 10 }));
    component.openEnter();
    component.proof.set('secret');
    component.enter();

    expect(component.zone()?.ticket).toBe('t');
    expect(component.canDeleteOffice(former)).toBe(true);
    expect(component.canDeleteOffice({ ...own, endDate: '2025-01-01' })).toBe(false);
  });

  it('says what went wrong with the password, and opens nothing', () => {
    service.enterDangerZone.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 400, error: { error: 'WrongCredentials' } })));
    component.openEnter();
    component.proof.set('wrong');
    component.enter();

    expect(component.zone()).toBeNull();
    expect(component.proofError()).toBe('Неправильний пароль');
  });

  it('ends the zone when the page goes away', () => {
    service.enterDangerZone.mockReturnValue(of({ ticket: 't', expiresAtUtc: new Date(Date.now() + 600_000).toISOString(), deletionsLeft: 10 }));
    component.openEnter();
    component.proof.set('secret');
    component.enter();

    fixture.destroy();

    expect(service.endDangerZone).toHaveBeenCalledWith('k1', 'Left');
  });

  it('closes the zone once its last deletion is spent', () => {
    service.enterDangerZone.mockReturnValue(of({ ticket: 't', expiresAtUtc: new Date(Date.now() + 600_000).toISOString(), deletionsLeft: 1 }));
    service.deleteOffice.mockReturnValue(of({ deletionsLeft: 0 }));
    component.openEnter();
    component.proof.set('secret');
    component.enter();

    // The confirmation is the library's; accept it straight away.
    const confirmation = fixture.debugElement.injector.get(ConfirmationService);
    vi.spyOn(confirmation, 'confirm').mockImplementation(config => {
      config.accept?.();
      return confirmation;
    });
    component.confirmDeleteOffice(archive().offices[0]);

    expect(service.deleteOffice).toHaveBeenCalledWith('k1', 'h1', 't');
    expect(component.zone()).toBeNull();
  });

  it('says why a membership with dues stays', () => {
    service.enterDangerZone.mockReturnValue(of({ ticket: 't', expiresAtUtc: new Date(Date.now() + 600_000).toISOString(), deletionsLeft: 1 }));
    service.deleteMembership.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 409, error: { error: 'ArchiveMembershipHasDues' } })));
    component.openEnter();
    component.proof.set('secret');
    component.enter();

    const confirmation = fixture.debugElement.injector.get(ConfirmationService);
    vi.spyOn(confirmation, 'confirm').mockImplementation(config => {
      config.accept?.();
      return confirmation;
    });
    const add = vi.spyOn(TestBed.inject(MessageService), 'add');
    component.confirmDeleteMembership(archive().memberships[0]);

    expect(add).toHaveBeenCalledWith({ severity: 'error', summary: 'За членством є внески' });
    expect(component.zone()).not.toBeNull();
  });
});

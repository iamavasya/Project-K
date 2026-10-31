import type { MockedObject } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { ConfirmationService, MessageService } from '@openng/optimus-ui/api';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../authModule/services/auth-service/auth.service';
import { FormerMemberStandingDto, MembershipCandidateDto, MembershipRequestDto, MembershipService } from '../../services/membership-service/membership.service';
import { TransfersComponent } from './transfers';

describe('TransfersComponent', () => {
  let memberships: MockedObject<MembershipService>;
  let messages: MessageService;

  const kurin = (number: number) => ({ kurinKey: `k${number}`, number, namedAfter: null });
  const request = (over: Partial<MembershipRequestDto>): MembershipRequestDto => ({
    membershipRequestKey: 'r1',
    kind: 'Transfer',
    status: 'Pending',
    memberKey: 'm1',
    memberName: 'Назар Коваль',
    fromKurin: kurin(1),
    toKurin: kurin(15),
    membershipKind: 'Youth',
    createdAtUtc: '2026-10-30T10:00:00Z',
    expiresAtUtc: '2026-11-13T10:00:00Z',
    decidedAtUtc: null,
    owedToLeave: 0,
    ...over
  });
  const candidate = (currentKurins: ReturnType<typeof kurin>[]): MembershipCandidateDto => ({
    member: { memberKey: 'm1', publicId: 'PL-4F7A2-9D4A1', firstName: 'Назар', lastName: 'Коваль', fullName: 'Назар Коваль' },
    alreadyInThisKurin: false,
    currentKurins
  });

  const formerPerson = (memberKey: string, standsIn: ReturnType<typeof kurin>[]): FormerMemberStandingDto => ({
    former: { memberKey, firstName: 'Віталій', lastName: 'Бойко', groupName: 'Соколи', joinedAtUtc: '2023-09-01', leftAtUtc: '2026-10-01' },
    standsIn
  });

  function create(incoming: MembershipRequestDto[] = [], outgoing: MembershipRequestDto[] = [], former: FormerMemberStandingDto[] = []) {
    memberships = {
      requests: vi.fn(),
      candidate: vi.fn(),
      createRequest: vi.fn(),
      approveRequest: vi.fn(),
      declineRequest: vi.fn(),
      cancelRequest: vi.fn(),
      formerStanding: vi.fn()
    } as MockedObject<MembershipService>;
    memberships.requests.mockReturnValue(of({ incoming, outgoing }));
    memberships.formerStanding.mockReturnValue(of(former));
    memberships.createRequest.mockReturnValue(of({ membershipRequestKey: 'r9', joined: false }));

    TestBed.configureTestingModule({
      imports: [TransfersComponent],
      providers: [
        provideNoopAnimations(),
        provideRouter([]),
        MessageService,
        { provide: MembershipService, useValue: memberships },
        { provide: AuthService, useValue: { getAuthState: () => of({ kurinKey: 'k15' }) } }
      ]
    });
    const fixture = TestBed.createComponent(TransfersComponent);
    messages = TestBed.inject(MessageService);
    fixture.detectChanges();
    return fixture;
  }

  const text = (fixture: ReturnType<typeof create>) => (fixture.nativeElement as HTMLElement).textContent ?? '';

  it('lists the requests to answer and those sent, pending first', () => {
    const fixture = create(
      [request({ membershipRequestKey: 'old', status: 'Declined' }), request({})],
      [request({ membershipRequestKey: 'out', fromKurin: kurin(7), toKurin: kurin(15) })]);

    expect(fixture.componentInstance.incoming()[0].status).toBe('Pending');
    expect(fixture.componentInstance.pendingIncoming()).toBe(1);
    expect(text(fixture)).toContain('к. ч. 15 просить перевести до себе');
    expect(text(fixture)).toContain('Перевести з к. ч. 7');
  });

  it('will not offer to approve a transfer while the person owes', () => {
    const fixture = create([request({ owedToLeave: 460 })]);

    expect(text(fixture)).toContain('Борг за минулі квартали');
    const approve = (fixture.nativeElement as HTMLElement).querySelector('.transfers__actions button') as HTMLButtonElement;
    expect(approve.disabled).toBe(true);
  });

  it('finds a person by code and asks the kurin they stand in', () => {
    const fixture = create();
    const component = fixture.componentInstance;
    memberships.candidate.mockReturnValue(of(candidate([kurin(1)])));

    component.code.set('pl-4f7a2-9d4a1');
    component.find();
    component.kind.set('AlsoJoin');
    component.send();

    expect(memberships.candidate).toHaveBeenCalledWith('k15', 'PL-4F7A2-9D4A1');
    expect(memberships.createRequest).toHaveBeenCalledWith('k15', {
      publicId: 'PL-4F7A2-9D4A1', kind: 'AlsoJoin', membershipKind: 'Youth', fromKurinKey: 'k1'
    });
  });

  it('takes in at once someone who stands nowhere', () => {
    const fixture = create();
    const component = fixture.componentInstance;
    memberships.candidate.mockReturnValue(of(candidate([])));
    memberships.createRequest.mockReturnValue(of({ membershipRequestKey: null, joined: true }));
    const toast = vi.spyOn(messages, 'add');

    component.code.set('PL-4F7A2-9D4A1');
    component.find();
    fixture.detectChanges();

    expect(text(fixture)).toContain('Прийняти до куреня');
    component.send();
    expect(toast).toHaveBeenCalledWith(expect.objectContaining({ summary: 'Людину прийнято' }));
  });

  it('needs the kurin to ask when the person stands in several', () => {
    const fixture = create();
    const component = fixture.componentInstance;
    memberships.candidate.mockReturnValue(of(candidate([kurin(1), kurin(7)])));

    component.code.set('PL-4F7A2-9D4A1');
    component.find();
    expect(component.canSend()).toBe(false);

    component.fromKurinKey.set('k7');
    expect(component.canSend()).toBe(true);
  });

  it('says so when the code is nobody\'s', () => {
    const fixture = create();
    const component = fixture.componentInstance;
    memberships.candidate.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 404 })));

    component.code.set('PL-00000-00000');
    component.find();

    expect(component.lookupError()).toBe('Такого коду немає. Перевір його з людиною.');
  });

  it('approves after the dialog, with a red button for «перевести»', () => {
    const fixture = create([request({})]);
    const confirmation = fixture.debugElement.injector.get(ConfirmationService);
    const confirm = vi.spyOn(confirmation, 'confirm').mockImplementation(options => {
      options.accept?.();
      return confirmation;
    });
    memberships.approveRequest.mockReturnValue(of(undefined));

    fixture.componentInstance.confirmApprove(request({}));

    expect(confirm.mock.calls[0][0].acceptButtonProps).toEqual({ label: 'Перевести', severity: 'danger' });
    expect(memberships.approveRequest).toHaveBeenCalledWith('k15', 'r1');
  });

  it('lists former members with where they stand now, and marks those already asked for', () => {
    const fixture = create([], [request({ memberKey: 'asked', fromKurin: kurin(1) })],
      [formerPerson('nowhere', []), formerPerson('asked', [kurin(1)])]);

    expect(text(fixture)).toContain('Колишні члени');
    expect(text(fixture)).toContain('Ніде');
    expect(text(fixture)).toContain('Запит чекає');
    expect(fixture.componentInstance.askedFor().has('asked')).toBe(true);
  });

  it('takes back at once a former member who stands nowhere', () => {
    const fixture = create([], [], [formerPerson('nowhere', [])]);
    const component = fixture.componentInstance;
    memberships.createRequest.mockReturnValue(of({ membershipRequestKey: null, joined: true }));

    component.openReturn(component.former()[0]);
    component.returnMembershipKind.set('Staff');
    component.sendReturn();

    expect(memberships.createRequest).toHaveBeenCalledWith('k15', {
      memberKey: 'nowhere', kind: 'Transfer', membershipKind: 'Staff', fromKurinKey: null
    });
    expect(component.returning()).toBeNull();
  });

  it('asks the kurin a former member stands in, «ще й до нас» when chosen', () => {
    const fixture = create([], [], [formerPerson('elsewhere', [kurin(1), kurin(7)])]);
    const component = fixture.componentInstance;

    component.openReturn(component.former()[0]);
    component.returnKind.set('AlsoJoin');
    expect(component.canReturn()).toBe(false);
    component.returnFrom.set('k7');
    component.sendReturn();

    expect(memberships.createRequest).toHaveBeenCalledWith('k15', {
      memberKey: 'elsewhere', kind: 'AlsoJoin', membershipKind: 'Youth', fromKurinKey: 'k7'
    });
  });
});

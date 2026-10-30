import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { MockedObject } from 'vitest';
import { ConfirmationService } from '@openng/optimus-ui/api';
import { MemberDto } from '../../models/member.dto';
import { ThirdProbeDto } from '../../models/probes-and-badges/probe-project.dto';
import { BadgeImageBlobService } from '../../services/probes-and-badges/badge-image-blob.service';
import { MemberProgressService } from '../../services/probes-and-badges/member-progress.service';
import { thirdProbeTab } from '../../functions/third-probe.function.spec';
import { ThirdProbeComponent } from './third-probe';

describe('ThirdProbeComponent', () => {
  let fixture: ComponentFixture<ThirdProbeComponent>;
  let progress: MockedObject<MemberProgressService>;

  async function render(tab: ThirdProbeDto, member: Partial<MemberDto> | null = null): Promise<HTMLElement> {
    progress.getThirdProbe.mockReturnValue(of(tab));
    fixture = TestBed.createComponent(ThirdProbeComponent);
    fixture.componentRef.setInput('memberKey', 'm-1');
    fixture.componentRef.setInput('probeId', 'probe-3');
    fixture.componentRef.setInput('member', member as MemberDto | null);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(async () => {
    progress = {
      getThirdProbe: vi.fn(),
      saveThirdProbeFile: vi.fn(),
      confirmThirdProbePart: vi.fn(),
      uploadCampCertificate: vi.fn(),
      deleteCampCertificate: vi.fn()
    } as MockedObject<MemberProgressService>;

    await TestBed.configureTestingModule({
      imports: [ThirdProbeComponent],
      providers: [
        { provide: MemberProgressService, useValue: progress },
        { provide: BadgeImageBlobService, useValue: { resolveBadgeImageForDisplay: (url: string | null) => url } },
        ConfirmationService
      ]
    }).compileComponents();
  });

  it('shows nothing for a probe that is not the third', async () => {
    const page = await render(thirdProbeTab({ isProjectProbe: false, file: null }));

    expect(page.querySelector('[data-testid="third-probe-checklist"]')).toBeNull();
  });

  it('lays out the countdown, the checklist and every part of the file', async () => {
    const page = await render(thirdProbeTab({
      file: { ...thirdProbeTab().file!, campName: 'КВТ «Говерла»', campKind: 'Kvt' },
      badges: [
        { badgeId: 'run', title: 'Легка атлетика', imagePath: null, specialization: 'Спортивна', confirmedAtUtc: '2026-04-01T00:00:00Z', duringProbe: true },
        { badgeId: 'swim', title: 'Плавання', imagePath: null, specialization: 'Спортивна', confirmedAtUtc: '2025-04-01T00:00:00Z', duringProbe: false }
      ]
    }), { dateOfBirth: new Date(2010, 5, 3), leadershipHistories: [] });

    expect(page.querySelector('[data-testid="third-probe-countdown"]')).not.toBeNull();
    expect(page.textContent).toContain('Що вже є');
    expect(page.textContent).toContain('КВТ «Говерла»');
    expect(page.textContent).toContain('коли закрили другу пробу');
    expect(page.textContent).toContain('Легка атлетика');
    expect(page.textContent).not.toContain('Плавання');
    expect(page.textContent).toContain('Усі вмілості юнацтва (2)');
  });

  it('marks the вмілість for тіловиховання by writing the file whole', async () => {
    await render(thirdProbeTab({
      file: { ...thirdProbeTab().file!, mentors: 'Звʼязковий', resume: 'Мій шлях' },
      badges: [{ badgeId: 'run', title: 'Біг', imagePath: null, specialization: null, confirmedAtUtc: null, duringProbe: true }]
    }));
    progress.saveThirdProbeFile.mockReturnValue(of(thirdProbeTab()));

    fixture.componentInstance.choosePhysical('run');

    expect(progress.saveThirdProbeFile).toHaveBeenCalledWith('m-1', 'probe-3', expect.objectContaining({
      physicalBadgeId: 'run', mentors: 'Звʼязковий', resume: 'Мій шлях', openedOn: null
    }));
  });

  it('refuses a посвідка that is too big before sending it', async () => {
    await render(thirdProbeTab());
    const big = new File([new Uint8Array(3 * 1024 * 1024)], 'scan.pdf', { type: 'application/pdf' });
    const input = document.createElement('input');
    Object.defineProperty(input, 'files', { value: [big] });

    fixture.componentInstance.onCertificatePicked({ target: input } as unknown as Event);

    expect(progress.uploadCampCertificate).not.toHaveBeenCalled();
    expect(fixture.componentInstance.errorMessage()).toBe('Посвідка завелика: до 2 МБ.');
  });

  it('lets a leader confirm the camp, and nobody else', async () => {
    const tab = thirdProbeTab({ file: { ...thirdProbeTab().file!, campName: 'КВТ' } });
    expect((await render(tab)).textContent).not.toContain('Зарахувати табір');

    fixture.destroy();
    expect((await render({ ...tab, canSign: true })).textContent).toContain('Зарахувати табір');
  });
});

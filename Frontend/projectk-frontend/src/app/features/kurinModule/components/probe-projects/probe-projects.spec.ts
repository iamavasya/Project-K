import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { MockedObject } from 'vitest';
import { ConfirmationService } from '@openng/optimus-ui/api';
import { MemberProgressService } from '../../services/probes-and-badges/member-progress.service';
import { ProbeProjectDto, ProbeProjectsDto } from '../../models/probes-and-badges/probe-project.dto';
import { ProbeProjectsComponent } from './probe-projects';

const IMAGE_KEY = '3f2504e0-4f89-11d3-9a0c-0305e82c3301';

function project(overrides: Partial<ProbeProjectDto> = {}): ProbeProjectDto {
  return {
    projectKey: 'p-1',
    title: 'Мандрівка Карпатами',
    isIndividual: true,
    description: `**Мета**: зійти на Говерлу\n![зображення](project-image:${IMAGE_KEY})`,
    directionMentorName: 'Олена Коваль',
    directionMentorContact: '+380501112233',
    updatedAtUtc: '2026-10-30T10:00:00Z',
    updatedByName: 'Тарас Шевчук',
    images: [{ imageKey: IMAGE_KEY, url: 'https://blob/probe-project-images/a.jpg?sig' }],
    ...overrides
  };
}

function answer(overrides: Partial<ProbeProjectsDto> = {}): ProbeProjectsDto {
  return {
    isProjectProbe: true,
    canEdit: true,
    projects: [project()],
    limits: { maxProjects: 5, maxImagesPerProject: 5, maxImageBytes: 1048576, maxDescriptionLength: 20000 },
    ...overrides
  };
}

describe('ProbeProjectsComponent', () => {
  let fixture: ComponentFixture<ProbeProjectsComponent>;
  let progress: MockedObject<MemberProgressService>;

  async function render(data: ProbeProjectsDto, readOnly = false): Promise<HTMLElement> {
    progress.getProbeProjects.mockReturnValue(of(data));
    fixture = TestBed.createComponent(ProbeProjectsComponent);
    fixture.componentRef.setInput('memberKey', 'm-1');
    fixture.componentRef.setInput('probeId', 'upyu-2026-3');
    fixture.componentRef.setInput('readOnly', readOnly);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(async () => {
    progress = {
      getProbeProjects: vi.fn(),
      addProbeProject: vi.fn(),
      updateProbeProject: vi.fn(),
      deleteProbeProject: vi.fn(),
      uploadProbeProjectImage: vi.fn()
    } as MockedObject<MemberProgressService>;

    await TestBed.configureTestingModule({
      imports: [ProbeProjectsComponent],
      providers: [
        { provide: MemberProgressService, useValue: progress },
        ConfirmationService
      ]
    }).compileComponents();
  });

  it('shows each project with its mentor, text and own picture', async () => {
    const page = await render(answer());

    expect(page.textContent).toContain('Мандрівка Карпатами');
    expect(page.textContent).toContain('Індивідуальний');
    expect(page.textContent).toContain('Олена Коваль');
    expect(page.querySelector('.project-text .font-semibold')?.textContent).toBe('Мета');
    expect(page.querySelector<HTMLImageElement>('.project-text__image')?.src).toContain('probe-project-images/a.jpg');
  });

  it('shows nothing for a probe that is not the third', async () => {
    const page = await render(answer({ isProjectProbe: false, projects: [] }));

    expect(page.querySelector('[data-testid="probe-projects"]')).toBeNull();
  });

  it('offers writing only to whom the server allows, and never on an archive', async () => {
    expect((await render(answer({ canEdit: false }))).textContent).not.toContain('Редагувати');

    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [ProbeProjectsComponent],
      providers: [{ provide: MemberProgressService, useValue: progress }, ConfirmationService]
    }).compileComponents();
    expect((await render(answer(), true)).textContent).not.toContain('Редагувати');
  });

  it('stops offering a new project at the limit', async () => {
    const five = Array.from({ length: 5 }, (_, i) => project({ projectKey: `p-${i}`, isIndividual: i === 0 }));
    const page = await render(answer({ projects: five }));

    expect(page.textContent).toContain('5 з 5');
    expect(page.textContent).not.toContain('Додати проєкт');
  });

  it('offers the first project from the empty state', async () => {
    const page = await render(answer({ projects: [] }));

    expect(page.textContent).toContain('Тут будуть проєкти');
    expect(page.textContent).toContain('Додати проєкт');
  });

  it('says in words why the server refused a save', async () => {
    await render(answer({ projects: [] }));
    const component = fixture.componentInstance;
    progress.addProbeProject.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 400, error: { error: 'TooManyProjects' } })));

    component.openNew();
    component.title.set('Шостий');
    component.save();

    expect(component.errorMessage()).toBe('Проєктів уже стільки, скільки передбачає проба.');
  });
});

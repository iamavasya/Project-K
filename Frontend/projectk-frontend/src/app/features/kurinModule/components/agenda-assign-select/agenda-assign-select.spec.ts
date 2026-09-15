import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AgendaAssignSelectComponent } from './agenda-assign-select';
import { AgendaService } from '../../services/agenda-service/agenda.service';
import { AgendaAssignTargets } from '../../models/agenda';

describe('AgendaAssignSelectComponent', () => {
  let fixture: ComponentFixture<AgendaAssignSelectComponent>;
  let agendaService: jasmine.SpyObj<AgendaService>;

  /** What the backend hands a Гуртковий: the kurin is off limits, his гурток and its people are not. */
  const targetsForGroupProvid: AgendaAssignTargets = {
    canTargetKurin: false,
    kurinKey: 'k1',
    kurinLabel: 'Весь курінь',
    kurinLeaderships: [{ leadershipKey: 'l-kv', label: 'КВ', canTarget: false }],
    groups: [
      {
        groupKey: 'g1', name: 'Соколи', canTargetGroup: true,
        leadership: { leadershipKey: 'l-g1', label: 'Провід гуртка', canTarget: true },
        members: [{ memberKey: 'm1', fullName: 'Марта Коваль' }]
      },
      { groupKey: 'g2', name: 'Ведмеді', canTargetGroup: false, leadership: null, members: [] }
    ]
  };

  beforeEach(async () => {
    agendaService = jasmine.createSpyObj<AgendaService>('AgendaService', ['getAssignTargets']);
    agendaService.getAssignTargets.and.returnValue(of(targetsForGroupProvid));

    await TestBed.configureTestingModule({
      imports: [AgendaAssignSelectComponent],
      providers: [{ provide: AgendaService, useValue: agendaService }]
    }).compileComponents();

    fixture = TestBed.createComponent(AgendaAssignSelectComponent);
    fixture.componentRef.setInput('kurinKey', 'k1');
    fixture.detectChanges();
  });

  // A node the person cannot pick, with nothing pickable under it, is not drawn at all: no
  // «весь курінь» to stare at when the choice is not theirs.
  it('draws only what can be picked, or what holds something that can', () => {
    const nodes = (fixture.componentInstance as unknown as { nodes: () => { key?: string; selectable?: boolean; children?: unknown[] }[] }).nodes();

    expect(nodes.map(node => node.key)).toEqual(['group:g1']);
    expect(nodes[0].selectable).toBeTrue();
    expect((nodes[0].children ?? []).map(child => (child as { key?: string }).key)).toEqual(['leadership:l-g1', 'member:m1']);
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ScoreAlgorithm, ScoreSource } from '../../../scoreModule/models/score.enums';
import { MyScoreDto } from '../../models/me.dto';
import { MyScoreTileComponent } from './my-score-tile';

function score(overrides: Partial<MyScoreDto> = {}): MyScoreDto {
  return {
    kurin: { kurinKey: 'k1', kurinNumber: 1, namedAfter: null, isCurrent: true },
    groupKey: 'g1',
    groupName: 'Соколи',
    periodLabel: '26–27',
    total: 12,
    bySource: { [ScoreSource.Attendance]: 15, [ScoreSource.Warning]: -3, [ScoreSource.Free]: 0 },
    groupPlace: 2,
    groupCount: 4,
    groupScore: 7.5,
    algorithm: ScoreAlgorithm.Average,
    ...overrides
  };
}

describe('MyScoreTileComponent', () => {
  let fixture: ComponentFixture<MyScoreTileComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [MyScoreTileComponent], providers: [provideRouter([])] }).compileComponents();
    fixture = TestBed.createComponent(MyScoreTileComponent);
  });

  it('breaks the points down by source in the table order, leaving out what is zero', () => {
    fixture.componentRef.setInput('scores', [score()]);
    fixture.detectChanges();

    const row = fixture.componentInstance.rows()[0];
    expect(row.parts.map(p => [p.label, p.points])).toEqual([['Присутність', 15], ['Перестороги', -3]]);
    expect(row.rankClass).toBe('lil-rank--podium');
    expect(row.tableLink).toEqual(['/kurin', 'k1', 'score']);
    expect(fixture.componentInstance.signed(-3)).toBe('-3');
    expect(fixture.componentInstance.signed(15)).toBe('+15');
  });

  it('offers the table only in the kurin the token acts in', () => {
    fixture.componentRef.setInput('scores', [score({ kurin: { kurinKey: 'k2', kurinNumber: 2, namedAfter: null, isCurrent: false }, groupPlace: 1 })]);
    fixture.detectChanges();

    const row = fixture.componentInstance.rows()[0];
    expect(row.tableLink).toBeNull();
    expect(row.rankClass).toBe('lil-rank--first');
  });
});

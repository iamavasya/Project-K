import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { MyGrowthDto } from '../../models/me.dto';
import { MyProbeTileComponent } from './my-probe-tile';

function growth(probe: MyGrowthDto['probe']): MyGrowthDto {
  return { memberKey: 'm1', hasYouthProgram: true, probe, badges: { onReview: [], inWork: [], confirmed: [], confirmedCount: 0 } };
}

describe('MyProbeTileComponent', () => {
  let fixture: ComponentFixture<MyProbeTileComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [MyProbeTileComponent], providers: [provideRouter([])] }).compileComponents();
    fixture = TestBed.createComponent(MyProbeTileComponent);
  });

  it('shows how far the проба has come and the next points to sign', () => {
    fixture.componentRef.setInput('growth', growth({
      probeId: 'p1', title: 'Перша проба', status: 'InProgress', signedPoints: 3, totalPoints: 12,
      nextPoints: [{ pointId: 'x', sectionCode: 'А.2', title: 'Знати Пластовий закон' }]
    }));
    fixture.detectChanges();

    expect(fixture.componentInstance.percent()).toBe(25);
    expect(fixture.componentInstance.statusLabel()).toBe('в роботі');
    expect(fixture.componentInstance.probeLink()).toEqual(['/member', 'm1', 'probe', 'p1']);
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('.probe-tile__code')?.textContent?.trim()).toBe('А.2');
    expect(el.querySelector('.probe-tile__label')?.textContent?.trim()).toBe('Далі');
  });

  it('with every проба verified there is nothing left to show', () => {
    fixture.componentRef.setInput('growth', growth(null));
    fixture.detectChanges();

    expect(fixture.componentInstance.percent()).toBe(0);
    expect(fixture.componentInstance.probeLink()).toBeNull();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Проби складені');
  });
});

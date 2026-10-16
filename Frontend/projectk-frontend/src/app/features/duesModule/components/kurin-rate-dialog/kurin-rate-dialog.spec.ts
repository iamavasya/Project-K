import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { DuesRecipient } from '../../models/dues.enums';
import { KurinDuesRateDto, PlastYearDto, QuarterDto, SetKurinDuesRateRequest } from '../../models/group-dues.dto';
import { KurinRateDialogComponent } from './kurin-rate-dialog';

describe('KurinRateDialogComponent', () => {
  const q = (year: number, number: number): QuarterDto => ({ year, number });
  const years: PlastYearDto[] = [{ startYear: 2026, label: '2026–2027', quarters: [q(2026, 4), q(2027, 1), q(2027, 2), q(2027, 3)] }];
  const rate: KurinDuesRateDto = {
    fromQuarter: q(2026, 4),
    articles: [
      { name: 'Станиця', recipient: DuesRecipient.Stanytsia, amount: 240, reducedAmount: 180 },
      { name: 'Табірний фонд', recipient: DuesRecipient.Kurin, amount: 50, reducedAmount: 50 }
    ],
    stanytsiaFull: 240,
    stanytsiaReduced: 180,
    kurinShare: 50,
    kurinReduced: 50
  };

  function open(rates: KurinDuesRateDto[] = [rate]) {
    TestBed.configureTestingModule({ imports: [KurinRateDialogComponent], providers: [provideNoopAnimations()] });
    const fixture = TestBed.createComponent(KurinRateDialogComponent);
    fixture.componentRef.setInput('rates', rates);
    fixture.componentRef.setInput('years', years);
    fixture.componentRef.setInput('currentQuarter', q(2026, 4));
    fixture.componentRef.setInput('currency', 'CZK');
    fixture.componentRef.setInput('visible', true);
    fixture.detectChanges();
    return fixture.componentInstance;
  }

  it('starts from the статті in force now', () => {
    const dialog = open();

    expect(dialog.articles().map(a => a.name)).toEqual(['Станиця', 'Табірний фонд']);
    expect(dialog.totals()).toEqual({ full: 290, reduced: 230 });
  });

  it('offers a станиця and a kurin стаття to a kurin with no rates yet', () => {
    const dialog = open([]);

    expect(dialog.articles().map(a => [a.name, a.recipient])).toEqual([
      ['Станиця', DuesRecipient.Stanytsia],
      ['Курінь', DuesRecipient.Kurin]
    ]);
    expect(dialog.valid()).toBeFalse();
  });

  it('sends every стаття, the full amount standing in for a пільга left empty', () => {
    const dialog = open();
    let sent: SetKurinDuesRateRequest | undefined;
    dialog.save.subscribe(r => (sent = r));

    dialog.addArticle();
    const added = dialog.articles().at(-1)!;
    dialog.setArticle(added.id, { name: ' Крайова вкладка ', recipient: DuesRecipient.Stanytsia, amount: 60 });
    dialog.submit();

    expect(sent?.fromQuarter).toEqual(q(2026, 4));
    expect(sent?.articles.at(-1)).toEqual({ name: 'Крайова вкладка', recipient: DuesRecipient.Stanytsia, amount: 60, reducedAmount: 60 });
    expect(sent?.articles.length).toBe(3);
  });

  it('will not save two статті of one name or a пільга above the full amount', () => {
    const dialog = open();
    const [first, second] = dialog.articles();

    dialog.setArticle(second.id, { name: 'станиця' });
    expect(dialog.errors()).toContain('Дві статті не можуть називатися однаково.');

    dialog.setArticle(second.id, { name: 'Табірний фонд' });
    dialog.setArticle(first.id, { reducedAmount: 300 });
    expect(dialog.errors()).toContain('Пільгова сума не може бути вищою за повну.');
    expect(dialog.valid()).toBeFalse();

    dialog.removeArticle(first.id);
    expect(dialog.valid()).toBeTrue();
  });
});

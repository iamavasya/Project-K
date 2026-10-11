import {
  EntryDraft,
  amountLabel,
  balanceLabel,
  duesMoney,
  entryProblems,
  entryRequest,
  fromQuarterOptions,
  kurinRateRequest,
  kurinRatesText,
  quarterLabel,
  quarterOfDate,
  rateAt,
} from './dues-format';

const draft = (overrides: Partial<EntryDraft>): EntryDraft => ({
  kind: 'Contribution',
  method: 'Cash',
  amount: '150',
  occurredOn: '2026-10-04',
  membershipKey: 'ms1',
  collectedByMemberKey: null,
  note: '',
  ...overrides,
});

describe('dues format', () => {
  it('signs money the way the web history does', () => {
    expect(duesMoney(-300)).toBe('−300 ₴');
    expect(amountLabel({ kind: 'Contribution', amount: 150 })).toBe('+150 ₴');
    expect(amountLabel({ kind: 'Expense', amount: 40 })).toBe('−40 ₴');
    expect(amountLabel({ kind: 'Correction', amount: -20 })).toBe('−20 ₴');
    expect(amountLabel({ kind: 'Exchange', amount: 100 })).toBe('100 ₴');
    expect(balanceLabel(0)).toBe('сплачено');
    expect(balanceLabel(20)).toBe('+20 ₴');
  });

  it('places dates and rates in quarters', () => {
    expect(quarterOfDate('2026-10-04')).toEqual({ year: 2026, number: 4 });
    expect(quarterLabel({ year: 2026, number: 2 })).toBe('II кв. 2026');
    const rates = [
      { fromQuarter: { year: 2025, number: 3 }, groupShare: 10 },
      { fromQuarter: { year: 2026, number: 1 }, groupShare: 20 },
      { fromQuarter: { year: 2027, number: 1 }, groupShare: 30 },
    ];
    expect(rateAt(rates, { year: 2026, number: 4 })?.groupShare).toBe(20);
    expect(rateAt(rates, { year: 2025, number: 1 })).toBeNull();
  });

  it('offers the quarters on the table and the next one', () => {
    const years = [{ startYear: 2026, label: '2026/27', quarters: [{ year: 2026, number: 3 }, { year: 2026, number: 4 }] }];
    expect(fromQuarterOptions(years, { year: 2026, number: 4 })).toEqual([
      { year: 2026, number: 3 },
      { year: 2026, number: 4 },
      { year: 2027, number: 1 },
    ]);
  });

  it('checks an entry as the web form does', () => {
    expect(entryProblems(draft({}))).toEqual({});
    expect(entryProblems(draft({ amount: '' })).amount).toBe('Сума потрібна.');
    expect(entryProblems(draft({ amount: '-5' })).amount).toBe('Сума має бути більшою за нуль.');
    expect(entryProblems(draft({ kind: 'Correction', amount: '-5' })).amount).toBeUndefined();
    expect(entryProblems(draft({ membershipKey: null })).person).toBe('Чия це вкладка?');
    expect(entryProblems(draft({ kind: 'Expense', membershipKey: null })).person).toBeUndefined();
  });

  it('builds the request: no person on a box operation, the other side of an exchange', () => {
    expect(entryRequest(draft({ kind: 'Expense', amount: '12,5', note: '  мотузка ' }))).toEqual({
      kind: 'Expense',
      method: 'Cash',
      counterMethod: null,
      amount: 12.5,
      occurredOn: '2026-10-04',
      membershipKey: null,
      collectedByMemberKey: null,
      note: 'мотузка',
    });
    expect(entryRequest(draft({ kind: 'Exchange', method: 'Card' })).counterMethod).toBe('Cash');
  });

  it('says the kurin parts in force, or that there are none yet', () => {
    const rate = { fromQuarter: { year: 2026, number: 3 }, stanytsiaFull: 100, stanytsiaReduced: 50, kurinShare: 30 };
    expect(kurinRatesText(rate)).toBe(
      'Станиця 100 ₴ (пільгова 50 ₴) · курінь 30 ₴ за квартал з кожного юнака. Гурткову частину кожен гурток задає собі.',
    );
    expect(kurinRatesText({ ...rate, stanytsiaReduced: 100 })).toContain('Станиця 100 ₴ · курінь 30 ₴');
    expect(kurinRatesText(null)).toBe('Ставки ще не задано — гуртки нічого не нараховують, доки їх немає.');
  });

  it('checks the kurin rates as the web dialog does', () => {
    const from = { year: 2026, number: 4 };
    expect(kurinRateRequest({ full: '120', reduced: '', share: '30,5' }, from)).toEqual({
      fromQuarter: from,
      stanytsiaFull: 120,
      stanytsiaReduced: 120,
      kurinShare: 30.5,
    });
    expect(kurinRateRequest({ full: '120', reduced: '60', share: '0' }, from)?.stanytsiaReduced).toBe(60);
    expect(kurinRateRequest({ full: '', reduced: '', share: '30' }, from)).toBeNull();
    expect(kurinRateRequest({ full: '120', reduced: '', share: '' }, from)).toBeNull();
    expect(kurinRateRequest({ full: '100', reduced: '150', share: '30' }, from)).toBeNull();
    expect(kurinRateRequest({ full: '-1', reduced: '', share: '30' }, from)).toBeNull();
    expect(kurinRateRequest({ full: 'abc', reduced: '', share: '30' }, from)).toBeNull();
  });
});

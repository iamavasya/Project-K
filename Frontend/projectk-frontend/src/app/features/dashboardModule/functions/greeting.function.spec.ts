import { greeting, initials, todayLabel } from './greeting.function';

describe('dashboard greeting', () => {
  it('greets by the hour', () => {
    expect(greeting(new Date(2026, 9, 7, 3))).toBe('Доброї ночі');
    expect(greeting(new Date(2026, 9, 7, 9))).toBe('Доброго ранку');
    expect(greeting(new Date(2026, 9, 7, 14))).toBe('Доброго дня');
    expect(greeting(new Date(2026, 9, 7, 20))).toBe('Доброго вечора');
  });

  it('writes the day in Ukrainian', () => {
    expect(todayLabel(new Date(2026, 9, 7))).toBe('середа, 7 жовтня 2026');
  });

  it('takes initials, and copes with a missing name', () => {
    expect(initials('Оксана', 'Паливода')).toBe('ОП');
    expect(initials('', null)).toBe('');
  });
});

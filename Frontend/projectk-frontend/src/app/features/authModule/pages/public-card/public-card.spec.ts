import type { MockedObject } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { CardService, PublicCardDto } from '../../services/card-service/card.service';
import { PublicCardComponent } from './public-card';

describe('PublicCardComponent', () => {
  let cards: MockedObject<CardService>;
  const card: PublicCardDto = {
    fullName: 'Назар Коваль', photoUrl: null, kurins: [{ number: 15, namedAfter: null, groupName: 'Соколи' }],
    level: 'пл. уч.', awards: [{ level: 4, dateAcquired: '2026-05-01T00:00:00Z' }], badges: null, probes: [],
    checkedAtUtc: '2026-10-31T12:32:00Z'
  };

  function create(mode: 'check' | 'link') {
    TestBed.configureTestingModule({
      imports: [PublicCardComponent],
      providers: [
        provideRouter([]),
        { provide: CardService, useValue: cards },
        { provide: ActivatedRoute, useValue: { snapshot: { data: { mode }, paramMap: convertToParamMap({ token: 'tok' }) } } }
      ]
    });
    const fixture = TestBed.createComponent(PublicCardComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    cards = { checked: vi.fn(), linked: vi.fn() } as MockedObject<CardService>;
  });

  it('shows a checked card with the parts it carries and when it was checked', () => {
    cards.checked.mockReturnValue(of(card));
    const text = (create('check').nativeElement as HTMLElement).textContent ?? '';

    expect(cards.checked).toHaveBeenCalledWith('tok');
    expect(text).toContain('Назар Коваль');
    expect(text).toContain('Четверте відзначення');
    expect(text).toContain('Перевірено Лілейкою');
    expect(text).not.toContain('Вмілості');
  });

  it('reads a permanent link from its own address and says when it is gone', () => {
    cards.linked.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 404 })));
    const text = (create('link').nativeElement as HTMLElement).textContent ?? '';

    expect(cards.linked).toHaveBeenCalledWith('tok');
    expect(text).toContain('Картку не знайдено');
    expect(text).toContain('Посилання вимкнене');
  });
});

import { TestBed } from '@angular/core/testing';
import { PublicCardDto } from '../../services/card-service/card.service';
import { SharedCardComponent } from './shared-card';

describe('SharedCardComponent', () => {
  const base: PublicCardDto = {
    fullName: 'Андрій Козак',
    photoUrl: null,
    kurins: null,
    level: null,
    awards: [],
    badges: null,
    probes: null,
    checkedAtUtc: null
  };

  function render(card: PublicCardDto): HTMLElement {
    TestBed.configureTestingModule({ imports: [SharedCardComponent] });
    const fixture = TestBed.createComponent(SharedCardComponent);
    fixture.componentRef.setInput('card', card);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  const headings = (host: HTMLElement) =>
    Array.from(host.querySelectorAll('[data-testid="award-ulad"]')).map(h => h.textContent?.trim());

  it('heads the відзначення of the whole way by ulad, the latest line first', () => {
    const host = render({
      ...base,
      awards: [
        { level: 2, title: '2-е відзначення', imageUrl: null, dateAcquired: '2019-05-01', ulad: 'UPYu' },
        { level: null, title: 'Грамота заслуги', imageUrl: null, dateAcquired: '2025-05-01', ulad: 'USP' }
      ]
    });

    expect(headings(host)).toEqual(['УСП', 'УПЮ']);
    expect(host.textContent).toContain('Друге відзначення');
    expect(host.textContent).toContain('Грамота заслуги');
  });

  it('needs no heading for one line', () => {
    const host = render({
      ...base,
      awards: [{ level: null, title: 'Грамота заслуги', imageUrl: null, dateAcquired: '2025-05-01', ulad: 'USP' }]
    });

    expect(headings(host)).toEqual([]);
  });
});

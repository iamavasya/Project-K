import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { MessageService } from '@openng/optimus-ui/api';
import { MemberSeniorBookTileComponent } from './member-senior-book-tile';
import { bookSections, recordLine } from './senior-book.function';
import { SeniorRecordService } from '../../../../services/senior-record-service/senior-record.service';
import { MemberSeniorRecordDto, SeniorCatalogItem } from '../../../../models/senior-record.dto';

const catalog: SeniorCatalogItem[] = [
  { id: 'usp-starshyi-plastun', kind: 'Degree', title: 'Старший пластун', group: 'usp', ulad: 'usp', order: 1, imageUrl: 'https://catalog.test/sp.svg' },
  { id: 'usp-hramota-zasluhy', kind: 'Award', title: 'Грамота заслуги', group: 'usp-vidznachennia', ulad: 'usp', order: 0, imageUrl: '' },
  { id: 'lsh-absolvent', kind: 'Training', title: 'Лісова Школа — абсольвент', group: 'lsh', ulad: 'usp', order: 1, imageUrl: 'https://catalog.test/lsh.svg' }
];

const record = (key: string, catalogId: string, achievedOn: string | null, number: string | null = null): MemberSeniorRecordDto =>
  ({ memberSeniorRecordKey: key, catalogId, achievedOn, number, note: null });

describe('senior book', () => {
  it('ступені, відзначення й вишколи окремо, новіше вгорі; невідоме каталогу лишається під своїм id', () => {
    const sections = bookSections([
      record('a', 'lsh-absolvent', '1999-08-20', 'ЛШ 41'),
      record('b', 'usp-hramota-zasluhy', '2012-10-20'),
      record('c', 'usp-starshyi-plastun', '1997-11-15'),
      record('d', 'zabute', '2020-01-01'),
      record('e', 'usp-hramota-zasluhy', '2019-03-01')
    ], catalog);

    expect(sections.map(s => s.title)).toEqual(['Ступені', 'Відзначення', 'Вишколи']);
    expect(sections[1].rows.map(r => r.title)).toEqual(['zabute', 'Грамота заслуги', 'Грамота заслуги']);
    expect(sections[1].rows.map(r => r.record.memberSeniorRecordKey)).toEqual(['d', 'e', 'b']);
    expect(sections[2].rows[0].imageUrl).toBe('https://catalog.test/lsh.svg');
  });

  it('рядок під назвою — номер і дата, що є', () => {
    expect(recordLine(record('a', 'lsh-absolvent', '1999-08-20', 'ЛШ 41'))).toBe('ЛШ 41 · 20.08.1999');
    expect(recordLine(record('b', 'usp-hramota-zasluhy', null))).toBe('');
  });
});

describe('MemberSeniorBookTileComponent', () => {
  let fixture: ComponentFixture<MemberSeniorBookTileComponent>;
  let component: MemberSeniorBookTileComponent;
  let service: { catalog: ReturnType<typeof vi.fn>; add: ReturnType<typeof vi.fn>; update: ReturnType<typeof vi.fn>; remove: ReturnType<typeof vi.fn> };
  let messages: { add: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    service = { catalog: vi.fn(() => of(catalog)), add: vi.fn(), update: vi.fn(), remove: vi.fn() };
    messages = { add: vi.fn() };
    TestBed.configureTestingModule({
      imports: [MemberSeniorBookTileComponent],
      providers: [
        { provide: SeniorRecordService, useValue: service },
        { provide: MessageService, useValue: messages }
      ]
    });
    fixture = TestBed.createComponent(MemberSeniorBookTileComponent);
    component = fixture.componentInstance;
  });

  const render = (records: MemberSeniorRecordDto[], editable: boolean) => {
    fixture.componentRef.setInput('records', records);
    fixture.componentRef.setInput('editable', editable);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  };

  it('чужу книжечку лише читають: без «Додати», і рядок не відкривається', () => {
    const element = render([record('a', 'lsh-absolvent', '1999-08-20', 'ЛШ 41')], false);

    expect(element.querySelector('[data-testid="senior-book-add"]')).toBeNull();
    expect(element.textContent).toContain('Лісова Школа — абсольвент');
    expect(element.textContent).toContain('ЛШ 41 · 20.08.1999');
    component.open(component.records()[0]);
    expect(component.dialogVisible).toBe(false);
  });

  it('власник додає: номер іде лише з вишколом, і новий рядок стає першим', () => {
    const existing = record('a', 'usp-hramota-zasluhy', '2012-10-20');
    const element = render([existing], true);
    const saved = record('n', 'usp-starshyi-plastun', '1997-11-15');
    service.add.mockReturnValue(of(saved));
    const emitted: MemberSeniorRecordDto[][] = [];
    component.recordsChange.subscribe(records => emitted.push(records));

    expect(element.querySelector('[data-testid="senior-book-add"]')).not.toBeNull();
    component.open(null);
    component.catalogId = 'usp-starshyi-plastun';
    component.number = 'не для ступеня';
    component.achievedOn = new Date(1997, 10, 15);
    component.save();

    expect(service.add).toHaveBeenCalledWith({ catalogId: 'usp-starshyi-plastun', number: null, achievedOn: '1997-11-15', note: null });
    expect(emitted[0]).toEqual([saved, existing]);
    expect(component.dialogVisible).toBe(false);
  });

  it('власник змінює й видаляє свій рядок', () => {
    const mine = record('a', 'lsh-absolvent', '1999-08-20', 'ЛШ 41');
    render([mine], true);
    const changed = { ...mine, number: 'ЛШ 42' };
    service.update.mockReturnValue(of(changed));
    service.remove.mockReturnValue(of(undefined));
    const emitted: MemberSeniorRecordDto[][] = [];
    component.recordsChange.subscribe(records => emitted.push(records));

    component.open(mine);
    component.number = 'ЛШ 42';
    component.save();
    expect(service.update).toHaveBeenCalledWith('a', expect.objectContaining({ catalogId: 'lsh-absolvent', number: 'ЛШ 42' }));
    expect(emitted[0]).toEqual([changed]);

    component.open(mine);
    component.remove();
    expect(service.remove).toHaveBeenCalledWith('a');
    expect(emitted[1]).toEqual([]);
  });

  it('другий той самий ступінь — сервер відмовляє, і людина бачить чому', () => {
    render([], true);
    service.add.mockReturnValue(throwError(() => ({ status: 409 })));

    component.open(null);
    component.catalogId = 'usp-starshyi-plastun';
    component.save();

    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ summary: 'Цей ступінь уже є' }));
    expect(component.dialogVisible).toBe(true);
  });
});

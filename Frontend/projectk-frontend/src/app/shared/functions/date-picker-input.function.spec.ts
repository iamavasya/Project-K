import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { DatePickerModule } from '@openng/optimus-ui/datepicker';

import { acceptDatePickerInputWithoutKeydown } from './date-picker-input.function';

@Component({
  imports: [FormsModule, DatePickerModule],
  template: `<p-datepicker [(ngModel)]="value" dateFormat="yy-mm-dd" />`
})
class HostComponent {
  value: Date | null = null;
}

describe('acceptDatePickerInputWithoutKeydown', () => {
  beforeAll(() => acceptDatePickerInputWithoutKeydown());

  it('should take a date a phone keyboard inserts without a keydown', async () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    input.value = '2015-09-01';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const value = fixture.componentInstance.value;
    expect(value).toEqual(new Date(2015, 8, 1));
  });
});

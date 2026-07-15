import { formatDate } from '@angular/common';
import { Pipe, PipeTransform } from '@angular/core';
import { parseUtcDateTime } from '../functions/utcDateTime.function';

@Pipe({
  name: 'localUtcDate',
  standalone: true
})
export class LocalUtcDatePipe implements PipeTransform {
  transform(
    value: string | Date | null | undefined,
    dateFormat = 'medium',
    locale = 'uk-UA'
  ): string {
    if (!value) {
      return '';
    }

    const date = parseUtcDateTime(value);
    return date ? formatDate(date, dateFormat, locale) : String(value);
  }
}


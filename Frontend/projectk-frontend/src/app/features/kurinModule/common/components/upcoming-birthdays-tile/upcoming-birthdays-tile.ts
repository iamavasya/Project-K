import { DatePipe } from '@angular/common';
import { Component, OnChanges, OnInit, ChangeDetectionStrategy, input } from '@angular/core';
import { TagModule } from '@openng/optimus-ui/tag';
import { MemberLookupDto } from '../../models/requests/member/memberLookupDto';
import { buildUpcomingBirthdays, UpcomingBirthdayItem } from '../../functions/upcomingBirthdays.function';

@Component({
  selector: 'app-upcoming-birthdays-tile',
  imports: [TagModule, DatePipe],
  templateUrl: './upcoming-birthdays-tile.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './upcoming-birthdays-tile.css'
})
export class UpcomingBirthdaysTileComponent implements OnInit, OnChanges {
  readonly members = input<MemberLookupDto[]>([]);
  readonly daysAhead = input(30);
  readonly title = input('Найближчі дні народження');

  readonly previewLimit = 5;
  upcomingBirthdays: UpcomingBirthdayItem[] = [];

  ngOnInit(): void {
    this.refreshUpcomingBirthdays();
  }

  ngOnChanges(): void {
    this.refreshUpcomingBirthdays();
  }

  get previewBirthdays(): UpcomingBirthdayItem[] {
    return this.upcomingBirthdays.slice(0, this.previewLimit);
  }

  get remainingBirthdaysCount(): number {
    return Math.max(this.upcomingBirthdays.length - this.previewLimit, 0);
  }

  buildUpcomingBirthdays(referenceDate: Date = new Date()): UpcomingBirthdayItem[] {
    return buildUpcomingBirthdays(this.members(), this.daysAhead(), referenceDate);
  }

  private refreshUpcomingBirthdays(): void {
    this.upcomingBirthdays = this.buildUpcomingBirthdays();
  }
}
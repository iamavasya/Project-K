import { CommonModule } from '@angular/common';
import { Component, Input, OnChanges, OnInit } from '@angular/core';
import { TagModule } from 'primeng/tag';
import { MemberLookupDto } from '../../models/requests/member/memberLookupDto';
import { buildUpcomingBirthdays, UpcomingBirthdayItem } from '../../functions/upcomingBirthdays.function';

@Component({
  selector: 'app-upcoming-birthdays-tile',
  imports: [CommonModule, TagModule],
  templateUrl: './upcoming-birthdays-tile.html',
  styleUrl: './upcoming-birthdays-tile.css'
})
export class UpcomingBirthdaysTileComponent implements OnInit, OnChanges {
  @Input() members: MemberLookupDto[] = [];
  @Input() daysAhead = 30;
  @Input() title = 'Найближчі дні народження';

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
    return buildUpcomingBirthdays(this.members, this.daysAhead, referenceDate);
  }

  private refreshUpcomingBirthdays(): void {
    this.upcomingBirthdays = this.buildUpcomingBirthdays();
  }
}
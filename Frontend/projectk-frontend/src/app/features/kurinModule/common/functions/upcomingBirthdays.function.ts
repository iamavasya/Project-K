import { MemberLookupDto } from '../models/requests/member/memberLookupDto';

export interface UpcomingBirthdayItem {
  member: MemberLookupDto;
  nextBirthdayDate: Date;
  daysUntilBirthday: number;
}

function toStartOfDay(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}

function resolveNextBirthdayDate(dateOfBirth: string | Date | null | undefined, today: Date): Date | null {
  if (!dateOfBirth) {
    return null;
  }

  const parsedDate = dateOfBirth instanceof Date ? dateOfBirth : new Date(dateOfBirth);
  if (Number.isNaN(parsedDate.getTime())) {
    return null;
  }

  let nextBirthdayDate = new Date(today.getFullYear(), parsedDate.getMonth(), parsedDate.getDate());
  if (nextBirthdayDate < today) {
    nextBirthdayDate = new Date(today.getFullYear() + 1, parsedDate.getMonth(), parsedDate.getDate());
  }

  return nextBirthdayDate;
}

function calculateDaysBetween(startDate: Date, endDate: Date): number {
  const millisecondsInDay = 1000 * 60 * 60 * 24;
  return Math.floor((endDate.getTime() - startDate.getTime()) / millisecondsInDay);
}

export function buildUpcomingBirthdays(
  members: MemberLookupDto[],
  daysAhead: number,
  referenceDate: Date = new Date()
): UpcomingBirthdayItem[] {
  const today = toStartOfDay(referenceDate);

  return members
    .map(member => {
      const nextBirthdayDate = resolveNextBirthdayDate(member.dateOfBirth, today);
      if (!nextBirthdayDate) {
        return null;
      }

      const daysUntilBirthday = calculateDaysBetween(today, nextBirthdayDate);
      return {
        member,
        nextBirthdayDate,
        daysUntilBirthday
      } satisfies UpcomingBirthdayItem;
    })
    .filter((item): item is UpcomingBirthdayItem => item !== null)
    .filter(item => item.daysUntilBirthday >= 0 && item.daysUntilBirthday <= daysAhead)
    .sort((a, b) => {
      if (a.daysUntilBirthday !== b.daysUntilBirthday) {
        return a.daysUntilBirthday - b.daysUntilBirthday;
      }

      return `${a.member.lastName} ${a.member.firstName}`.localeCompare(`${b.member.lastName} ${b.member.firstName}`);
    });
}
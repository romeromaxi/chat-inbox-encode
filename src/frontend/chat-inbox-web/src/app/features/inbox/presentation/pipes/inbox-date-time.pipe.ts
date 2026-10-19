import { Pipe, PipeTransform } from '@angular/core';

const DATE_TIME_FORMATTER = new Intl.DateTimeFormat('en-GB', {
  timeZone: 'America/Argentina/Buenos_Aires',
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23',
});

@Pipe({ name: 'inboxDateTime' })
export class InboxDateTimePipe implements PipeTransform {
  transform(value: string): string {
    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
      return '';
    }

    const parts = Object.fromEntries(
      DATE_TIME_FORMATTER.formatToParts(date).map(({ type, value: partValue }) => [
        type,
        partValue,
      ]),
    );

    return `${parts['day']}/${parts['month']}/${parts['year']} ${parts['hour']}:${parts['minute']}`;
  }
}

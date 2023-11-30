import { Pipe, PipeTransform } from '@angular/core';
import { DatePipe } from '@angular/common';

const ISO_8601: string = 'yyyy-MM-ddTHH:mm:ss';
const DefaultLocale: string = 'en-US'

@Pipe({ name: 'utctolocal' })
export class UtcToLocalPipe implements PipeTransform {
    transform(utcDate: any) {
        // The input utcDate is coming from the endpoint like this: 2020-06-15T04:11:56
        // Below the format is converted to ISO 8601: yyyy-MM-ddTHH:mm:ss.000Z => 2020-06-15T04:11:56.000Z
        const datePipe = new DatePipe(DefaultLocale);
        const utcDateString = datePipe.transform(utcDate, ISO_8601) + '.000Z';
        // Finally convert from ISO_8601 to local date
        return new Date(utcDateString);
    }
}

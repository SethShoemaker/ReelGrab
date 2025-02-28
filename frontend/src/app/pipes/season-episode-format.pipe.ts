import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'seasonEpisodeFormat'
})
export class SeasonEpisodeFormatPipe implements PipeTransform {

  transform(value: number): string {
    if (value === null || value === undefined) return '';
    return value < 10 ? `0${value}` : value.toString();
  }

}

import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'nonBreakingSpaces'
})
export class NonBreakingSpacesPipe implements PipeTransform {

  transform(value: string): string {
    return value.replace(/ /g, '\u00A0');
  }

}

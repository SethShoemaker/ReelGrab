import { Pipe, PipeTransform } from '@angular/core';
import {filesize} from "filesize";

@Pipe({
  name: 'filesize'
})
export class FilesizePipe implements PipeTransform {

  transform(value: number): unknown {
    return filesize(value)
  }

}

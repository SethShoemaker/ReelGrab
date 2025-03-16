import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import { Snack } from './snack';

@Injectable({
  providedIn: 'root'
})
export class SnackbarService {

  snacks = new Subject<Snack>()

  constructor() { }
}

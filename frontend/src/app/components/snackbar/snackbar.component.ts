import { Component, OnDestroy, OnInit } from '@angular/core';
import { Snack } from '../../services/snackbar/snack';
import { map, Subscription } from 'rxjs';
import { SnackbarService } from '../../services/snackbar/snackbar.service';
import { NgFor, NgIf } from '@angular/common';
import { SnackLevel } from '../../services/snackbar/snack-level';

@Component({
  selector: 'app-snackbar',
  imports: [NgFor, NgIf],
  templateUrl: './snackbar.component.html',
  styleUrl: './snackbar.component.scss'
})
export class SnackbarComponent implements OnInit, OnDestroy {

  snacks = new Array<Snack & { src: string, class: string }>();
  snackSub!: Subscription;

  constructor(public snackbarService: SnackbarService) { }

  ngOnInit(): void {
    this.snackSub = this.snackbarService.snacks.pipe(
      map(snack => {
        const item: Snack & { src: string, class: string } = {
          level: snack.level,
          title: snack.title,
          body: snack.body,
          actions: snack.actions,
          src: "",
          class: ""
        };
        switch (true) {
          case item.level == SnackLevel.INFO:
            item.src = "info.svg";
            item.class = "info";
            break;
          case item.level == SnackLevel.SUCCESS:
            item.src = "success.svg";
            item.class = "success";
            break;
          case item.level == SnackLevel.WARNING:
            item.src = "warning.svg";
            item.class = "warning";
            break;
          case item.level == SnackLevel.ERROR:
            item.src = "error.svg";
            item.class = "error";
            break;
          default:
            throw new Error(`unhandled snack level ${item.level}`);
        }
        return item;
      })
    ).subscribe(snack => {
      this.snacks.push(snack)
      setTimeout(() => {
        this.snacks.shift()
      }, 7000);
    });
  }

  ngOnDestroy(): void {
    this.snackSub.unsubscribe();
  }

}

import { SnackAction } from "./snack-action";
import { SnackLevel } from "./snack-level";

export class Snack {
    constructor(public level: SnackLevel, public title: string, public body: string|null = null, public actions: Array<SnackAction>|null = null) { }
}
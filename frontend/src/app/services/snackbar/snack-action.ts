export class SnackAction {
    constructor(public body: string, public action: () => any) {}
}
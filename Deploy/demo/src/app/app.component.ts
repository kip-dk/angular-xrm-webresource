import { Component } from '@angular/core';
import { XrmService, Entity } from './shared/xrm.service'


@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
    errorMessage: string = null;
    accounts: Entity[] = [];

    constructor(private xrmService: XrmService) {

    }

    ngOnInit() {
        this.xrmService.search("accounts", "name", null)
            .subscribe(entities => this.accounts = entities, error => this.errorMessage = <any>error);
    }


  title = 'app works!';
}

import { NgModule } from "@angular/core";
import { PreventDoubleClickDirective } from "./prevent-double-click.directive";
import { DisableControlDirective } from './disable-control.directive';

@NgModule({
    // imports: [
    //   BrowserModule,
    //   FormsModule,
    //   RouterModule
    // ],
    declarations: [
        PreventDoubleClickDirective,
        DisableControlDirective
    ],
    exports: [
        PreventDoubleClickDirective,
        DisableControlDirective
    ]
  })
  export class CustomDirectivesModule { }
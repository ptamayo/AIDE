import { Component, ChangeDetectorRef, LOCALE_ID, Inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  title = 'bootstrap-template';
  constructor(private ref: ChangeDetectorRef, public translateService: TranslateService, @Inject(LOCALE_ID) public locale: string) {
    locale = locale.split('-')[0];
    translateService.addLangs(['en', 'es']);
    translateService.setDefaultLang(locale);
    localStorage.setItem('locale', locale);
    ref.detach();
    setInterval(() => {
        this.ref.detectChanges();
    }, 500);
 }
}

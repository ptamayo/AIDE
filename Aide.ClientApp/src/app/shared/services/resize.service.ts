import { Injectable } from '@angular/core';

import { distinctUntilChanged } from 'rxjs/operators';
import { ScreenSize } from 'src/app/enums/screen-size.enum';
import { Observable, Subject } from 'rxjs';
import { environment } from 'src/environments/environment';

@Injectable()
export class ResizeService {

  get onResize$(): Observable<ScreenSize> {
    return this.resizeSubject.asObservable().pipe(distinctUntilChanged());
  }

  private resizeSubject: Subject<ScreenSize>;

  constructor() {
    this.resizeSubject = new Subject();
  }

  onResize(size: ScreenSize) {
    this.resizeSubject.next(size);
    environment.screenSize = size; // *** Need revisit
  }

}
import { Directive, EventEmitter, HostListener, Input, Output } from '@angular/core';
import { Subject, Subscription } from 'rxjs';
import { throttleTime } from 'rxjs/operators';

@Directive({
  selector: '[appPreventDoubleClick]'
})
export class PreventDoubleClickDirective {
  @Input()
  throttleTime = 500;

  @Output()
  throttledClick = new EventEmitter();

  private clicks = new Subject();
  private subscription: Subscription;
  
  constructor() { }

  ngOnInit() {
    this.subscription = this.clicks.pipe(
      throttleTime(this.throttleTime)
    ).subscribe(e => this.emitThrottledClick(e));
  }

  emitThrottledClick(e) {
    this.throttledClick.emit(e);
  }

  ngOnDestroy() {
    this.subscription.unsubscribe();
  }

  @HostListener('click', ['$event'])
  clickEvent(event) {
    event.preventDefault();
    event.stopPropagation();
    this.clicks.next(event);
  }
}

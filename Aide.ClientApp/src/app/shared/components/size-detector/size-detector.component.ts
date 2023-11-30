import { Component, AfterViewInit, ElementRef, HostListener } from '@angular/core';
import { ScreenSize } from 'src/app/enums/screen-size.enum';
import { ResizeService } from '../../services/resize.service';

@Component({
  selector: 'app-size-detector',
  templateUrl: './size-detector.component.html',
  styleUrls: ['./size-detector.component.css']
})
export class SizeDetectorComponent implements AfterViewInit {
  prefix = 'is-';
  sizes = [
    {
      id: ScreenSize.XS, name: 'xs',
      css: `d-block d-sm-none`
    },
    {
      id: ScreenSize.SM, name: 'sm',
      css: `d-none d-sm-block d-md-none`
    },
    {
      id: ScreenSize.MD, name: 'md',
      css: `d-none d-md-block d-lg-none`
    },
    {
      id: ScreenSize.LG, name: 'lg',
      css: `d-none d-lg-block d-xl-none`
    },
    {
      id: ScreenSize.XL, name: 'xl',
      css: `d-none d-xl-block`
    },
  ];

  constructor(private elementRef: ElementRef, private resizeSvc: ResizeService) { }

  @HostListener("window:resize", [])
  public onResize() {
    this.detectScreenSize();
  }

  ngAfterViewInit() {
    this.detectScreenSize();
  }

  private detectScreenSize() {
    const currentSize = this.sizes.find(x => {
      const el = this.elementRef.nativeElement.querySelector(`.${this.prefix}${x.id}`);
      const isVisible = window.getComputedStyle(el, "").display != 'none';
      return isVisible;
    });
    this.resizeSvc.onResize(currentSize.id);
  }

}

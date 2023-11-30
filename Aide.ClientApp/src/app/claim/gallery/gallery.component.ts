import { Component, Input, Output, EventEmitter } from '@angular/core';
import { NgxGalleryAnimation, NgxGalleryImage, NgxGalleryOptions } from 'ngx-gallery-9';

@Component({
  selector: 'app-gallery',
  templateUrl: './gallery.component.html',
  styleUrls: ['./gallery.component.css']
})
export class GalleryComponent {
  public _claimId: number;
  public _showImageList: boolean = false;
  public _galleryImages: NgxGalleryImage[];
  public galleryOptions: NgxGalleryOptions[] = [
    {
      previewZoom: true, 
      previewRotate: true,
      imageDescription: true,
      width: "600px",
      height: "400px",
      arrowPrevIcon: "fa fa-chevron-left",
      arrowNextIcon: "fa fa-chevron-right",
      // imagePercent: 80,
      thumbnailsColumns: 4,
      thumbnailsRows: 1,
      thumbnailsPercent: 40,
      imageAnimation: NgxGalleryAnimation.Slide,
      imageSwipe: true,
      thumbnailsSwipe: true,
      previewSwipe: true,
      previewCloseOnClick: true,
      previewCloseOnEsc: true,
      // previewFullscreen: true, 
      // previewForceFullscreen: true
    },
    {
      breakpoint: 800,
      width: "300px",
      height: "300px"
    }
  ];

  @Input()
  public set claimId(value: number) {
    this._claimId = value;
  }

  @Input()
  public set galleryImages(value: NgxGalleryImage[]) {
    this._galleryImages = value;
  }

  @Input()
  public set showImageList(value: boolean) {
    this._showImageList = value;
  }

  @Output()
  showImageListChange: EventEmitter<boolean> = new EventEmitter<boolean>();
  
  constructor() { }

  onShowImageListChange(value: boolean) {
    this.showImageListChange.emit(value);
  }
}

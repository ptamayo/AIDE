import { Component, OnInit, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { FormBuilder, FormGroup, Validators, FormControl } from '@angular/forms';
import { FileUploadService } from '../../services/file-upload.service';
import { HttpEvent, HttpEventType } from '@angular/common/http';
import { catchError } from 'rxjs/operators';
import { of } from 'rxjs';
import { Media } from 'src/app/models/media';
import { DocumentOrientationId } from 'src/app/enums/document-orientation-id';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-upload-document-dialog',
  templateUrl: './upload-document-dialog.component.html',
  styleUrls: ['./upload-document-dialog.component.css']
})
export class UploadDocumentDialogComponent implements OnInit {
  progress: number = 0;
  btnIsDisabled: boolean = false;

  form: FormGroup;
  image = new FormControl('', [Validators.required]);
  title: string;
  claimProbatoryDocumentId: number;
  acceptedFileExtensions: string;
  public orientation: DocumentOrientationId;
  isFileExtensionAllowed: boolean = true;

  constructor(
    private fb: FormBuilder,
    public fileUploadService: FileUploadService,
    private dialogRef: MatDialogRef<UploadDocumentDialogComponent>,
    @Inject(MAT_DIALOG_DATA) data) { 
      this.title = data.title;
      this.claimProbatoryDocumentId = data.claimProbatoryDocumentId;
      this.acceptedFileExtensions = data.acceptedFileExtensions;
      this.orientation = data.orientation;
    }

  ngOnInit() {
    this.progress = 0;
    this.btnIsDisabled = false;
    this.form = this.fb.group({
      image: this.image
    });
  }

  getVisualGuideline(o: string): string {
    if (o === 'Portrait') {
      if (this.orientation == DocumentOrientationId.Portrait) {
        return `${environment.urlAssets}/phone-portrait-yes.png`;
      } else if (this.orientation == DocumentOrientationId.Landscape) {
        return `${environment.urlAssets}/phone-portrait-no.png`;
      }
    } else if (o === 'Landscape') {
      if (this.orientation == DocumentOrientationId.Portrait) {
        return `${environment.urlAssets}/phone-landscape-no.png`;
      } else if (this.orientation == DocumentOrientationId.Landscape) {
        return `${environment.urlAssets}/phone-landscape-yes.png`;
      }
    }
    return '#';
  }

  getVisualGuidelineHeaight(): string {
    if (screen.availHeight <= 375) {
      return '70';
    } else if (screen.availHeight > 375 && screen.availHeight <= 812) {
      return '100';
    }
    return '151';
  }

  onSelectedFile(event) {
    if (event.target.files.length > 0) {
      const file = event.target.files[0];
      const fileExtension = "." + file.name.split('.').pop();
      this.isFileExtensionAllowed = this.acceptedFileExtensions.split(',').indexOf(fileExtension.toLowerCase()) === -1 ? false : true;
      if (!this.isFileExtensionAllowed) {
        return;
      }
      this.form.get('image').setValue(file);
    }
  }

  onSubmit() {
    if (!this.form.valid) return;
    this.btnIsDisabled = true;
    this.fileUploadService.addClaimProbatoryDocument(
      this.claimProbatoryDocumentId,
      this.form.value.image
    )
    .pipe(
      catchError(err => {
        console.log('Handling error locally and rethrowing it...', err);
        this.progress = 0;
        return of([]); // do not send further details to the console
        // return throwError(err); // send all the details of the error to the console
      })
    )
    .subscribe((event: HttpEvent<any>) => {
      switch (event.type) {
        case HttpEventType.Sent:
          console.log('Request has been made!');
          break;
        case HttpEventType.ResponseHeader:
          console.log('Response header has been received!');
          break;
        case HttpEventType.UploadProgress:
          this.progress = Math.round(event.loaded / event.total * 100);
          console.log(`Uploaded! ${this.progress}%`);
          break;
        case HttpEventType.Response:
          console.log('File successfully uploaded!')
          setTimeout(() => {
            const media = event.body as Media;
            this.save(media);
          }, 1500);

      }
    });
  }

  save(media: Media) {
    this.dialogRef.close(media);
  }

  close() {
    this.dialogRef.close();
  }
}

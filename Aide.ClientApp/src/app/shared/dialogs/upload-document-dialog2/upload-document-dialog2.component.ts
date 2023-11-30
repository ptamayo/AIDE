import { Component, OnInit, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { FormBuilder, FormGroup, Validators, FormControl } from '@angular/forms';
import { FileUploadService } from '../../services/file-upload.service';
import { HttpEvent, HttpEventType } from '@angular/common/http';
import { catchError } from 'rxjs/operators';
import { of } from 'rxjs';
import { ClaimDocument } from 'src/app/models/claim-document';

@Component({
  selector: 'app-upload-document-dialog2',
  templateUrl: './upload-document-dialog2.component.html',
  styleUrls: ['./upload-document-dialog2.component.css']
})
export class UploadDocumentDialog2Component implements OnInit {
  progress: number = 0;
  btnIsDisabled: boolean = false;

  form: FormGroup;
  image = new FormControl('', [Validators.required]);
  title: string;
  documentTypeId: number;
  claimId: number;
  overwrite: boolean;
  sortPriority: number;
  groupId: number;
  acceptedFileExtensions: string;
  isFileExtensionAllowed: boolean = true;
  
  constructor(
    private fb: FormBuilder,
    public fileUploadService: FileUploadService,
    private dialogRef: MatDialogRef<UploadDocumentDialog2Component>,
    @Inject(MAT_DIALOG_DATA) data) { 
      this.title = data.title;
      this.documentTypeId = data.documentTypeId;
      this.claimId = data.claimId;
      this.overwrite = data.overwrite;
      this.sortPriority = data.sortPriority;
      this.groupId = data.groupId;
      this.acceptedFileExtensions = data.acceptedFileExtensions;
    }

  ngOnInit() {
    this.progress = 0;
    this.btnIsDisabled = false;
    this.form = this.fb.group({
      image: this.image
    });
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
    this.fileUploadService.addClaimDocument(
      this.documentTypeId,
      this.claimId,
      this.overwrite,
      this.sortPriority,
      this.groupId,
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
            const claimDocument = event.body as ClaimDocument;
            this.save(claimDocument);
          }, 1500);

      }
    });
  }

  save(claimDocument: ClaimDocument) {
    this.dialogRef.close(claimDocument);
  }

  close() {
    this.dialogRef.close();
  }
}

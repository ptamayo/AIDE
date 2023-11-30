import { Component, Input, OnInit, Output, EventEmitter } from '@angular/core';
import { MatDialog, MatDialogConfig } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { DocumentOrientationId } from 'src/app/enums/document-orientation-id';
import { ClaimProbatoryDocument } from 'src/app/models/claim-probatory-document';
import { DownloadMediaRequest } from 'src/app/models/download-media-request';
import { Media } from 'src/app/models/media';
import { OpenMediaRequest } from 'src/app/models/open-media-request';
import { MediaService } from 'src/app/services/media.service';
import { ConfirmDialogComponent } from 'src/app/shared/dialogs/confirm-dialog/confirm-dialog.component';
import { UploadDocumentDialogComponent } from 'src/app/shared/dialogs/upload-document-dialog/upload-document-dialog.component';
import { OpenBlob } from 'src/app/shared/helpers/open-blob';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-claim-probatory-documents',
  templateUrl: './claim-probatory-documents.component.html',
  styleUrls: ['./claim-probatory-documents.component.css']
})
export class ClaimProbatoryDocumentsComponent implements OnInit {
  private openMediaSubject: Subject<OpenMediaRequest> = new Subject<OpenMediaRequest>();
  public _title: string = "";
  public _description: string;
  public _isPhoneDevice = false;
  public _isAllowedToEditOrder = false;
  public _isClaimCompleted: boolean = false;
  public _claimProbatoryDocuments: ClaimProbatoryDocument[] = [];
  public _actionButton: string = 'upload';
  public _actionButtonDisabled: boolean = false;
  public _claimId: number;
  public columnDefinitions = [
    { def: 'sortPriority', hide: true }, 
    { def: 'probatoryDocument.name', hide: false }, 
    { def: 'actions', hide: false }
  ];

  @Input()
  public set title(value: string) {
    this._title = value;
  }

  @Input()
  public set description(value: string) {
    this._description = value;
  }

  @Input()
  public set isPhoneDevice(value: boolean) {
    this._isPhoneDevice = value;
  }
  
  @Input()
  public set isAllowedToEditOrder(value: boolean) {
    this._isAllowedToEditOrder = value;
  }

  @Input()
  public set isClaimCompleted(value: boolean) {
    this._isClaimCompleted = value;
    this.showOrHideColumns();
  }

  @Input()
  public set datasource(values: ClaimProbatoryDocument[]) {
    this._claimProbatoryDocuments = values;
  }

  @Input()
  public set actionButton(value: string) {
    this._actionButton = value;
  }

  @Input()
  public set actionButtonDisabled(value: boolean) {
    this._actionButtonDisabled = value;
  }

  @Input()
  public set claimId(value: number) {
    this._claimId = value;
  }

  @Output() 
  datasourceChange: EventEmitter<DownloadMediaRequest> = new EventEmitter<DownloadMediaRequest>();

  @Output()
  showImageListChange: EventEmitter<boolean> = new EventEmitter<boolean>();

  constructor(private router: Router, private dialog: MatDialog, private snackBar: MatSnackBar, private mediaService: MediaService) { }

  ngOnInit() {
    this.openMediaSubject.subscribe(request => OpenBlob(request));
  }

  getDisplayedColumns(): string[] {
    return this.columnDefinitions.filter(cd=>!cd.hide).map(cd=>cd.def);
  }

  showOrHideColumns() {
    if (this.columnDefinitions.length > 0) {
      if (this._isClaimCompleted) {
        this.columnDefinitions[2].hide = true;
      }
      else {
        this.columnDefinitions[2].hide = false;
      }
    }
  }

  onShowImageListChange(value: boolean) {
    this.showImageListChange.emit(value);
  }

  onClickSignatureBtn(row) {
    if(row.media) {
      this.redirectTo(`claim/${this._claimId}/signature`);
    } else {
      const dialogRef = this.openConfirmDialog('Confirmación de documentos completados y firmados', 'IMPORTANTE: Todos los documentos deben estar correctamente llenados. No se aceptarán documentos sin la firma del asegurado. ¿Están todos los documentos completados y firmados por el cliente asegurado? Si no está seguro por favor revise antes de continuar.');
      dialogRef.afterClosed().subscribe((answer: boolean) => {
        if (answer) {
          this.redirectTo(`claim/${this._claimId}/signature`);
        }
      });
    }
  }

  redirectTo(url) {
    this.router.navigateByUrl(url, {skipLocationChange: false}).then(() =>
    this.router.navigate(url));
  }

  openConfirmDialog(title: string, message: string) {
    const dialogConfig = new MatDialogConfig();
    dialogConfig.disableClose = true;
    dialogConfig.autoFocus = true;
    if (environment.screenSize <= 2) {
      // Phone in portrait or landscape
      dialogConfig.minHeight = '100%';
      dialogConfig.minWidth = '100%'
      dialogConfig.maxHeight = '100%';
      dialogConfig.maxWidth = '100%';
    }
    else {
      // Any other device bigger than a phone
      dialogConfig.minHeight = '50%';
      dialogConfig.minWidth = '50%'
      dialogConfig.maxHeight = '50%';
      dialogConfig.maxWidth = '50%';
    }
    dialogConfig.data = { title: title, message: message };
    return this.dialog.open(ConfirmDialogComponent, dialogConfig);
  }

  openDialog(probatoryDocument: string, claimProbatoryDocumentId: number, acceptedFileExtensions: string, orientation: DocumentOrientationId) {
    const dialogConfig = new MatDialogConfig();
    dialogConfig.disableClose = true;
    dialogConfig.autoFocus = true;
    if (environment.screenSize <= 2) {
      // Phone in portrait or landscape
      dialogConfig.minHeight = '100%';
      dialogConfig.minWidth = '100%'
      dialogConfig.maxHeight = '100%';
      dialogConfig.maxWidth = '100%';
    }
    else {
      // Any other device bigger than a phone
      dialogConfig.minHeight = '50%';
      dialogConfig.minWidth = '50%'
      dialogConfig.maxHeight = '50%';
      dialogConfig.maxWidth = '50%';
    }
    dialogConfig.data = { 
      title: probatoryDocument, 
      claimProbatoryDocumentId: claimProbatoryDocumentId, 
      acceptedFileExtensions: acceptedFileExtensions, 
      orientation: orientation
    };

    const dialogRef = this.dialog.open(UploadDocumentDialogComponent, dialogConfig);
    dialogRef.afterClosed().subscribe((media: Media) => {
        if (media) {
          const claimProbatorDocument = this._claimProbatoryDocuments.find(x => x.id === claimProbatoryDocumentId);
          claimProbatorDocument.media = media;
          // Push the change back to the parent component so that:
          // 1. It will update the galleryImages
          // 2. It will check onAllDocumentsCompleted
          const request = <DownloadMediaRequest> {
            id: media.id,
            description: claimProbatorDocument.probatoryDocument.name
          };
          this.datasourceChange.emit(request);
        }
      }
    );
  }

  isDownloading = [];
  
  download(mediaId: number, name: string, index: number) {
    if (this.isDownloading[index]) return;
    this.isDownloading[index] = true;
    this.mediaService.downloadMedia(mediaId).subscribe(blob => {
      const request = <OpenMediaRequest> {
        name: name,
        blob: blob
      };
      this.openMediaSubject.next(request);
      this.isDownloading[index] = false;
    },
    error => {
        console.log(error?.message);
        this.openSnackBar(`Cannot download the file of ${name}`, 'Dismiss');
        this.isDownloading[index] = false;
    });
  }

  openSnackBar(message: string, action: string) {
    this.snackBar.open(message, action, {
      duration: 3000,
    });
  }
}

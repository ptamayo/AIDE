import { Component, Input, Output, OnInit, EventEmitter } from '@angular/core';
import { MatDialog, MatDialogConfig } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subject } from 'rxjs';
import { ClaimDocument } from 'src/app/models/claim-document';
import { OpenMediaRequest } from 'src/app/models/open-media-request';
import { ClaimProbatoryDocumentsService } from 'src/app/services/claim-probatory-documents.service';
import { MediaService } from 'src/app/services/media.service';
import { AppError } from 'src/app/shared/common/app-error';
import { BadInput } from 'src/app/shared/common/bad-input';
import { ConfirmDialogComponent } from 'src/app/shared/dialogs/confirm-dialog/confirm-dialog.component';
import { MessageDialogComponent } from 'src/app/shared/dialogs/message-dialog/message-dialog.component';
import { OpenBlob } from 'src/app/shared/helpers/open-blob';

const ZipFileId: number = 102;
const ZipFileGroupId: number = 2;
const PdfFileId: number = 105;
const PdfFileGroupId: number = 2;
const InProcessStatusId = 20;

@Component({
  selector: 'app-export-documents',
  templateUrl: './export-documents.component.html',
  styleUrls: ['./export-documents.component.css']
})
export class ExportDocumentsComponent implements OnInit {
  private openDocumentSubject: Subject<OpenMediaRequest> = new Subject<OpenMediaRequest>();
  public _title: string = "";
  public _isPhoneDevice = false;
  public _btnExportZipDisabled = false;
  public _btnExportZipVisible = false;
  public _btnExportPdfDisabled = false;
  public _btnExportPdfVisible = false;
  public _claimId: number;
  public _claimDocuments: ClaimDocument[] = [];
  public documentsTableColumnDefinitions = [
    { def: 'sortPriority', hide: true }, 
    { def: 'documentType.name', hide: false }, 
    { def: 'actions', hide: false }
  ];

  @Input()
  public set title(value: string) {
    this._title = value;
  }

  @Input()
  public set isPhoneDevice(value: boolean) {
    this._isPhoneDevice = value;
  }

  @Input()
  public set exportZipDisabled(value: boolean) {
    this._btnExportZipDisabled = value;
  }

  @Input()
  public set exportZipVisible(value: boolean) {
    this._btnExportZipVisible = value;
  }

  @Input()
  public set exportPdfDisabled(value: boolean) {
    this._btnExportPdfDisabled = value;
  }

  @Input()
  public set exportPdfVisible(value: boolean) {
    this._btnExportPdfVisible = value;
  }

  @Input()
  public set claimId(value: number) {
    this._claimId = value;
  }

  @Input()
  public set datasource(values: ClaimDocument[]) {
    this._claimDocuments = values;
  }

  @Output()
  datasourceChange: EventEmitter<ClaimDocument> = new EventEmitter<ClaimDocument>();

  constructor(private snackBar: MatSnackBar, private dialog: MatDialog, private claimProbatoryDocumentsService: ClaimProbatoryDocumentsService, private mediaService: MediaService) { }

  ngOnInit() {
    this.openDocumentSubject.subscribe(request => OpenBlob(request));
  }

  getDocumentsTableDisplayedColumns(): string[] {
    return this.documentsTableColumnDefinitions.filter(cd=>!cd.hide).map(cd=>cd.def);
  }

  zipClaimFiles() {
    const dialogRef = this.openInfoDialog('Exportar ZIP', 'Info: El ZIP ya se está procesando, porfavor espere la notificación en la campanita.', 'info');
    dialogRef.afterClosed().subscribe();
    let processing = this._claimDocuments.find(x => x.documentTypeId == ZipFileId && x.groupId == ZipFileGroupId && x.statusId == InProcessStatusId && x.claimId == this._claimId);
    if (processing) {
      return;
    }
    this.claimProbatoryDocumentsService.zipClaimFiles(this._claimId).subscribe((newClaimDocument: ClaimDocument) => {
      if (newClaimDocument) {
        let currentClaimDocument = this._claimDocuments.find(x => x.documentTypeId == ZipFileId && x.groupId == ZipFileGroupId && x.claimId == this._claimId);
        if (currentClaimDocument) {
          currentClaimDocument.id = newClaimDocument.id;
          currentClaimDocument.documentId = newClaimDocument.documentId;
          currentClaimDocument.statusId = newClaimDocument.statusId;
          currentClaimDocument.dateCreated = newClaimDocument.dateCreated;
          currentClaimDocument.dateModified = newClaimDocument.dateModified;
          currentClaimDocument.document = newClaimDocument.document;
        }
        else {
          // this._claimDocuments.push(newClaimDocument); // This one does not work so that the line below is added
          // Send the new document back to the parent component for displaying on screen
          this.datasourceChange.emit(newClaimDocument);
        }
        // this.openSnackBar("Operation completed.", "Dismiss");
      }
    },
    (error: AppError) => {
      if (error instanceof BadInput) {
        this.openSnackBar(error.originalError.message, "Dismiss");
      }
      else throw error;
    });
  }

  pdfExportClaimFiles() {
    const dialogRef = this.openInfoDialog('Exportar PDF', 'Info: El PDF ya se está procesando, porfavor espere la notificación en la campanita.', 'info');
    dialogRef.afterClosed().subscribe();
    let processing = this._claimDocuments.find(x => x.documentTypeId == PdfFileId && x.groupId == PdfFileGroupId && x.statusId == InProcessStatusId && x.claimId == this._claimId);
    if (processing) {
      return;
    }
    this.claimProbatoryDocumentsService.pdfExportClaimFiles(this._claimId).subscribe((newClaimDocument: ClaimDocument) => {
      if (newClaimDocument) {
        let currentClaimDocument = this._claimDocuments.find(x => x.documentTypeId == PdfFileId && x.groupId == PdfFileGroupId && x.claimId == this._claimId);
        if (currentClaimDocument) {
          currentClaimDocument.id = newClaimDocument.id;
          currentClaimDocument.documentId = newClaimDocument.documentId;
          currentClaimDocument.statusId = newClaimDocument.statusId;
          currentClaimDocument.dateCreated = newClaimDocument.dateCreated;
          currentClaimDocument.dateModified = newClaimDocument.dateModified;
          currentClaimDocument.document = newClaimDocument.document;
        }
        else {
          // this._claimDocuments.push(newClaimDocument); // This one does not work so that the line below is added
          // Send the new document back to the parent component for displaying on screen
          this.datasourceChange.emit(newClaimDocument);
        }
        // this.openSnackBar("Operation completed.", "Dismiss");
      }
    },
    (error: AppError) => {
      if (error instanceof BadInput) {
        this.openSnackBar(error.originalError.message, "Dismiss");
      }
      else throw error;
    });
  }

  openSnackBar(message: string, action: string) {
    this.snackBar.open(message, action, {
      duration: 3000,
    });
  }

  openInfoDialog(title: string, message: string, icon: string) {
    const dialogConfig = new MatDialogConfig();
    dialogConfig.data = { title: title, message: message, icon: icon };
    return this.dialog.open(MessageDialogComponent, dialogConfig);
  }

  openConfirmDialog(title: string, message: string) {
    const dialogConfig = new MatDialogConfig();
    dialogConfig.disableClose = true;
    dialogConfig.autoFocus = true;
    dialogConfig.data = { title: title, message: message };
    return this.dialog.open(ConfirmDialogComponent, dialogConfig);
  }

  isDownloading = [];
  
  download(documentId: number, name: string, index: number) {
    if (this.isDownloading[index]) return;
    this.isDownloading[index] = true;
    this.mediaService.downloadDocument(documentId).subscribe(blob => {
      const request = <OpenMediaRequest> {
        name: name,
        blob: blob
      };
      this.openDocumentSubject.next(request);
      this.isDownloading[index] = false;
    },
    error => {
        console.log(error?.message);
        this.openSnackBar(`Cannot download the file of ${name}`, 'Dismiss');
        this.isDownloading[index] = false;
    });
  }
}

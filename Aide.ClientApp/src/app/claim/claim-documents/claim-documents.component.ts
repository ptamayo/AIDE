import { Component, Input, OnInit } from '@angular/core';
import { MatDialog, MatDialogConfig } from '@angular/material/dialog';
import { ClaimDocument } from 'src/app/models/claim-document';
import { UploadDocumentDialog2Component } from 'src/app/shared/dialogs/upload-document-dialog2/upload-document-dialog2.component';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-claim-documents',
  templateUrl: './claim-documents.component.html',
  styleUrls: ['./claim-documents.component.css']
})
export class ClaimDocumentsComponent implements OnInit {
  public _title: string = "";
  public _isPhoneDevice = false;
  public _isAllowedToEditOrder = false;
  public _claimId: number;
  public _claimDocuments: ClaimDocument[] = [];
  public columnDefinitions = [
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
  public set isAllowedToEditOrder(value: boolean) {
    this._isAllowedToEditOrder = value;
  }

  @Input()
  public set claimId(value: number) {
    this._claimId = value;
  }

  @Input()
  public set datasource(values: ClaimDocument[]) {
    this._claimDocuments = values;
  }

  constructor(private dialog: MatDialog) { }

  ngOnInit() {
  }

  getDisplayedColumns(): string[] {
    return this.columnDefinitions.filter(cd=>!cd.hide).map(cd=>cd.def);
  }

  openDialog(documentTypeName: string, documentTypeId: number, overwrite: boolean, sortPriority: number, groupId: number, acceptedFileExtensions: string) {
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
      title: documentTypeName, 
      claimId: this._claimId,
      documentTypeId: documentTypeId,
      overwrite: overwrite,
      sortPriority: sortPriority,
      groupId: groupId,
      acceptedFileExtensions: acceptedFileExtensions
    };

    const dialogRef = this.dialog.open(UploadDocumentDialog2Component, dialogConfig);
    dialogRef.afterClosed().subscribe((newClaimDocument: ClaimDocument) => {
        if (newClaimDocument) {
          let currentClaimDocument = this._claimDocuments.find(x => x.documentTypeId == documentTypeId && x.groupId == groupId && x.claimId == this._claimId);
          if (currentClaimDocument) {
            currentClaimDocument.id = newClaimDocument.id;
            currentClaimDocument.documentId = newClaimDocument.documentId;
            currentClaimDocument.statusId = newClaimDocument.statusId;
            currentClaimDocument.dateCreated = newClaimDocument.dateCreated;
            currentClaimDocument.dateModified = newClaimDocument.dateModified;
            currentClaimDocument.document = newClaimDocument.document;
          }
        }
      }
    );
  }
}

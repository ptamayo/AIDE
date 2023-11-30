import { Component, OnInit, OnDestroy } from '@angular/core';
import { environment } from 'src/environments/environment';
import { Subscription } from 'rxjs';
import { ResizeService } from 'src/app/shared/services/resize.service';
import { delay, catchError } from 'rxjs/operators';
import { ClaimService } from 'src/app/services/claim.service';
import { Signature } from 'src/app/models/signature';
import { ActivatedRoute } from '@angular/router';
import { Claim } from 'src/app/models/claim';
import { AuthService } from 'src/app/services/auth.service';
import { UserRoleId } from 'src/app/enums/user-role-id.enum';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ClaimReceiptService } from 'src/app/services/claim-receipt.service';
import { AppError } from 'src/app/shared/common/app-error';
import { BadInput } from 'src/app/shared/common/bad-input';
import '../../shared/extension-methods/date.component'
import { MatDialog, MatDialogConfig } from '@angular/material/dialog';
import { ConfirmDialogComponent } from 'src/app/shared/dialogs/confirm-dialog/confirm-dialog.component';
import { MessageDialogComponent } from 'src/app/shared/dialogs/message-dialog/message-dialog.component';
import { ClaimProbatoryDocument } from 'src/app/models/claim-probatory-document';

@Component({
  selector: 'app-signature',
  templateUrl: './signature.component.html',
  styleUrls: ['./signature.component.css']
})
export class SignatureComponent implements OnInit, OnDestroy {
  screenResizeSubscription: Subscription;
  screenSize;
  signatureCanvasStyle: string = "center-div-signature";
  signatureCanvasWidth: number = 595;
  signatureCanvasHeight: number = 200;
  editableSignaturePad: boolean = false;
  isLoadingPage: boolean = true;
  signaturePoints = [];
  signatureImage;
  signatureDate;
  signatureDateTimeZone;
  claimId: number;
  claim: Claim = new Claim();
  currentUserIsAllowedToEditOrder: boolean = false;
  currentUserCanEmailReceipt: boolean = false;
  listOfDocuments: ClaimProbatoryDocument[] | [];

  constructor(
    private authService: AuthService,
    private route: ActivatedRoute, 
    private resizeSvc: ResizeService, 
    private claimService: ClaimService,
    private claimReceiptService: ClaimReceiptService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar) { 
    this.screenResizeSubscription = this.resizeSvc.onResize$.pipe(delay(0)).subscribe(() => {
      this.setStyleOnSignatureCanvas();
    });
    this.setStyleOnSignatureCanvas();
  }

  ngOnInit() {
    this.currentUserIsAllowedToEditOrder = true;
    if (this.authService.currentUser.roleId === UserRoleId.InsuranceReadOnly) {
      this.currentUserIsAllowedToEditOrder = false;
    }

    if (!this.currentUserIsAllowedToEditOrder) {
      this.editableSignaturePad = false;
    }
    else {
      this.editableSignaturePad = true;
    }

    this.currentUserCanEmailReceipt = false;
    if (this.authService.currentUser.roleId != UserRoleId.InsuranceReadOnly && this.authService.currentUser.roleId != UserRoleId.WsOperator) {
      this.currentUserCanEmailReceipt = true;
    }

    this.isLoadingPage = true;
    this.claimId = +this.route.snapshot.paramMap.get('id');
    this.populate();
  }

  populate() {
    if (this.claimId) {
      this.claimService.getById(this.claimId)
      .pipe(
        catchError(() => {
          this.isLoadingPage = false;
          return null;
        })
      )
      .subscribe((data: Claim) => {
        this.claim = data;
        if (this.claim) {
          this.claimId = this.claim.id;
          this.populateSignature();
          this.listOfDocuments = this.listOfClaimProbatoryDocuments();
        }
      });
    }
  }

  listOfClaimProbatoryDocuments(): ClaimProbatoryDocument[] {
    // Notice that TPA Docs are skipped here
    if (!this.claim || !this.claim.claimProbatoryDocuments) return [];
    let docs = this.claim.claimProbatoryDocuments.filter(x => x.groupId == environment.AdminDocsGroupId || x.groupId == environment.PicturesGroupId);
    if (this.claim.itemsQuantity > 1) {
      const claimItemId = this.claim.claimProbatoryDocuments.filter(x => x.groupId == environment.PicturesXItemGroupId)[0].claimItemId;
      if (claimItemId) {
        const picturesXItem = this.claim.claimProbatoryDocuments.filter(x => x.groupId == environment.PicturesXItemGroupId && x.claimItemId == claimItemId).map(picture => {
          let document = picture.probatoryDocument;
          document.name = `${this.claim.itemsQuantity} x ${picture.probatoryDocument.name}`;
          return <ClaimProbatoryDocument> {
            probatoryDocument: document
          };
        });
        docs = docs.concat(picturesXItem);
      }
    } else {
      docs = docs.concat(this.claim.claimProbatoryDocuments.filter(x => x.groupId == environment.PicturesXItemGroupId));
    }
    return docs;
  }

  populateSignature() {
    if (this.claimId) {
      this.claimService.getSignatureByClaimId(this.claimId)
      .pipe(
        catchError(() => {
          this.isLoadingPage = false;
          return null;
        })
      )
      .subscribe((data: Signature) => {
        if (data) {
          this.signatureImage = data.base64image;
          this.signatureDate = data.localDate;
          this.signatureDateTimeZone = data.localTimeZone;
        }
        this.isLoadingPage = false;
      });
    }
  }
  
  setStyleOnSignatureCanvas() {
    if (environment.screenSize < 2) {
      this.signatureCanvasStyle = "center-div-signature-xs";
      this.signatureCanvasWidth = 370;
      this.signatureCanvasHeight = 200;
    }
    else {
      this.signatureCanvasStyle = "center-div-signature";
      this.signatureCanvasWidth = 595;
      this.signatureCanvasHeight = 180;
    }
  }

  showImage(data: Blob) {
    this.signatureImage = data;
  }

  submitSignature(data: Blob) {
    if (this.signaturePoints.length == 0) {
      const dialogRef = this.openInfoDialog('Firma digital', 'Error: Debe propocionar una firma para poder continuar', 'error');
      dialogRef.afterClosed().subscribe(() => {
        return;
      });
    } else {
      const dialogRef = this.openConfirmDialog('Confirmación de firma', 'Favor de confirmar que desea continuar con la firma proporcionada');
      dialogRef.afterClosed().subscribe((answer: boolean) => {
        if (answer) {
          this.isLoadingPage = true;
          this.editableSignaturePad = false;
          const signature: Signature = {
            base64image: data,
            localDate: new Date().formatToISO8601(),
            localTimeZone: Intl.DateTimeFormat().resolvedOptions().timeZone
          };
          this.claimService.submitSignature(this.claimId, signature).subscribe((event: Signature) =>  {
            this.signatureDate = event.localDate;
            this.signatureDateTimeZone = event.localTimeZone;
            this.showImage(data);
            this.isLoadingPage = false;
          });
        }
      });
    }
  }

  openConfirmDialog(title: string, message: string) {
    const dialogConfig = new MatDialogConfig();
    dialogConfig.disableClose = true;
    dialogConfig.autoFocus = true;
    dialogConfig.data = { title: title, message: message };
    return this.dialog.open(ConfirmDialogComponent, dialogConfig);
  }

  openInfoDialog(title: string, message: string, icon: string) {
    const dialogConfig = new MatDialogConfig();
    dialogConfig.data = { title: title, message: message, icon: icon };
    return this.dialog.open(MessageDialogComponent, dialogConfig);
  }

  emailClaimReceipt() {
    this.claimReceiptService.emailClaimReceipt(this.claimId).subscribe(() => {
      this.openSnackBar("Operation completed.", "Dismiss");
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
  
  ngOnDestroy(): void {
    this.screenResizeSubscription.unsubscribe();
  }
}

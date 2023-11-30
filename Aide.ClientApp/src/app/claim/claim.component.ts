import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormControl, Validators, FormGroup, AsyncValidatorFn, AbstractControl, ValidationErrors } from '@angular/forms';
import { ClaimService } from '../services/claim.service';
import { InsuranceCompanyService } from '../services/insurance-company.service';
import { InsuranceCompany } from '../models/insurance-company';
import { ClaimTypeService } from '../services/claim-type.service';
import { ClaimType } from '../models/claim-type';
import { Claim } from '../models/claim';
import { AppError } from '../shared/common/app-error';
import { BadInput } from '../shared/common/bad-input';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ClaimProbatoryDocument } from '../models/claim-probatory-document';
import { ClaimStatusId } from '../enums/claim-status-id.enum';
import { ClaimProbatoryDocumentStatusId } from '../enums/claim-probatory-document-status-id.enum';
import { NgxGalleryImage } from 'ngx-gallery-9';
// ------------------
// 3/13/2021
// IMPORTANT: This one seems to be NOT necessary BUT not sure of what's the impact of commenting out.
// It's possible the signature may need this but I'm not quite sure.
// import 'hammerjs';
// In the meantime implemented the solution below (for your consideration in case you want a rollback):
// https://dev.to/susomejias/solution-working-hammer-js-after-upgrading-to-angular-9-25n2
// ------------------
import { AuthService } from '../services/auth.service';
import { UserRoleId } from '../enums/user-role-id.enum';
import { Store } from '../models/store';
import { StoreService } from '../services/store.service';
import { ClaimDocument } from '../models/claim-document';
import { ClaimProbatoryDocumentsService } from '../services/claim-probatory-documents.service';
import { NotificationsQuery } from '../notifications/store/notifications-query';
import { forkJoin, Subscription, of as observableOf, timer, Observable, Subject } from 'rxjs';
import { Notification } from '../notifications/notification'
import { Document } from '../models/document';
import { environment } from 'src/environments/environment';
import { ClaimFormValidators } from './claim-form-validators';
import { IDictionary } from '../models/dictionary';
import { MatDialog, MatDialogConfig } from '@angular/material/dialog';
import { MessageDialogComponent } from '../shared/dialogs/message-dialog/message-dialog.component';
import { ConfirmDialogComponent } from '../shared/dialogs/confirm-dialog/confirm-dialog.component';
import { map, switchMap } from 'rxjs/operators';
import { MediaService } from '../services/media.service';
import { DownloadMediaRequest } from '../models/download-media-request';

const TpaDocsGroup: number = 4;
const PictureDocumentsGroup: number = 2;

const TableInsuranceZipFiles: number = 1;
const TableInsuranceElectronicInvoice: number = 2;
const TableAdministrativeDocuments: number = 3;
const TablePictures: number = 4;
const TableReceipt: number = 5;

const regexAGRI = new RegExp('^MJ[0-9]{7}$');

@Component({
  selector: 'app-claim',
  templateUrl: './claim.component.html',
  styleUrls: ['./claim.component.css']
})
export class ClaimComponent implements OnInit, OnDestroy {
  private notificationStoreSubscription: Subscription;
  
  action: string = "Add";
  saveBtnIsDisabled: boolean = false;
  receiptBtnIsDisabled: boolean = true;

  showImageList: boolean = true;
  galleryImages: NgxGalleryImage[];
  galleryImagesSubject: Subject<DownloadMediaRequest> = new Subject<DownloadMediaRequest>();

  isLoadingPage: boolean = true;
  stores: Store[] = [];
  insuranceCompanies: InsuranceCompany[] = [];
  claimTypes: ClaimType[] = [];
  claimId: number = 0;
  claim: Claim | null;
  claimDocuments: ClaimDocument[] = [];
  claimProbatoryDocuments: ClaimProbatoryDocument[] = [];
  claimStatusOptions: IDictionary<number[]> = { 10: [10, 20, 25], 20: [10, 20, 25, 30], 25: [10, 25], 30: [30] };

  myForm: FormGroup;
  orderStatus = new FormControl(ClaimStatusId.InProgress, [Validators.required]);
  store = new FormControl('', [Validators.required]);
  insuranceCompany = new FormControl('', [Validators.required]);
  claimType = new FormControl('', [Validators.required]);
  customerFullName = new FormControl('', [Validators.required, Validators.maxLength(50)]);
  policyNumber = new FormControl('', [Validators.maxLength(50)]);
  policySubsection = new FormControl('', [Validators.required, Validators.maxLength(10)]);
  claimNumber = new FormControl('', [Validators.maxLength(50)]);
  reportNumber = new FormControl('', [Validators.maxLength(50)]);
  externalOrderNumber = new FormControl('', [Validators.pattern('^(MJ[0-9]{7}|[A-Z]{3,5}[0-9]{9})$')], [this.externalOrderNumberAsyncValidator()]);
  depositSlip = new FormControl('', [Validators.required]);
  itemsQuantity = new FormControl('1', [Validators.required, Validators.pattern('^[1-9][0-9]*'), Validators.maxLength(2)]);
  isDepositSlipRequired: boolean = false;

  get currentUserRoleId(): UserRoleId {
    return this.authService.currentUser.roleId;
  }

  get claimZipFile(): ClaimDocument {
    if (this.claimDocuments) {
      return this.claimDocuments.find(x => x.documentTypeId === 102);
    }
    return null;
  }

  get claimPdfFile(): ClaimDocument {
    if (this.claimDocuments) {
      return this.claimDocuments.find(x => x.documentTypeId === 105);
    }
    return null;
  }

  get isPhoneDevice(): boolean {
    return environment.screenSize == 0;
  }

  get isAGRI(): boolean {
    return regexAGRI.test(this.claim?.externalOrderNumber);
  }

  constructor(
    private authService: AuthService,
    private route: ActivatedRoute, 
    private snackBar: MatSnackBar, 
    private dialog: MatDialog,
    private claimService: ClaimService, 
    private storeService: StoreService,
    private insuranceCompanyService: InsuranceCompanyService, 
    private claimTypeService: ClaimTypeService,
    private claimProbatoryDocumentsService: ClaimProbatoryDocumentsService,
    private notificationsQuery: NotificationsQuery,
    private mediaService: MediaService) { 
      this.notificationStoreSubscription = this.notificationsQuery.selectAll().subscribe(data => {
        // If the incoming notification matches any of the documents in this order then update the document state
        const latestNotification = data.sort((a, b) => (a.id < b.id) ? 1 : -1).slice(0, 1);
        if (latestNotification.length > 0) {
          const nx = latestNotification[0];
          this.refreshDocument(nx);
        }
      });
      this.galleryImagesSubject.subscribe(image => this.download(image));
    }

  ngOnInit() {
    this.myForm = new FormGroup({
      orderStatus: this.orderStatus,
      store: this.store,
      insuranceCompany: this.insuranceCompany,
      claimType: this.claimType,
      externalOrderNumber: this.externalOrderNumber,
      depositSlip: this.depositSlip,
      itemsQuantity: this.itemsQuantity,
      formGroup1: new FormGroup({
        policyNumber: this.policyNumber,
        claimNumber: this.claimNumber,
        reportNumber: this.reportNumber,
      }, { validators: [ClaimFormValidators.AtLeastOneClaimIdentifierMustBeProvided] }),
      policySubsection: this.policySubsection
    });

    this.galleryImages = [];
    this.claimId = +this.route.snapshot.paramMap.get('id');
    if (this.claimId) {
      this.action = "Edit";
    }
    this.initializeFormFields();
  }

  initializeFormFields() {
    let requestStores = this.storeService.getAll();
    let requestInsuranceCompanyServices = this.insuranceCompanyService.getAllEnabled();
    let requestClaimTypes = this.claimTypeService.getAll();
    // Determine if the claim it's new or existing
    let requestClaim = observableOf<Object>('NA');
    if (this.claimId && this.claimId > 0) {
      requestClaim = this.claimService.getById(this.claimId);
    }
    forkJoin([requestStores, requestInsuranceCompanyServices, requestClaimTypes, requestClaim])
    .subscribe(results => {
      this.stores = <Store[]>results[0];
      this.insuranceCompanies = <InsuranceCompany[]>results[1];
      this.claimTypes = <ClaimType[]>results[2];
      if (this.authService.currentUser.roleId == UserRoleId.WsAdmin || this.authService.currentUser.roleId == UserRoleId.WsOperator) {
        if (this.authService.currentUser.companies.length === 1) {
          const companyId = +this.authService.currentUser.companies[0].companyId;
          this.store.setValue(companyId);
        }
      }
      // If not a new claim
      if (this.claimId && this.claimId > 0 && results[3]) {
        this.populate(<Claim>results[3]);
      }
    },
    (error: AppError) => {
      if (error instanceof BadInput) {
        this.openSnackBar(error.originalError.message, "Dismiss");
      }
      else throw error;
    })
    .add(() => this.isLoadingPage = false);
  }

  populate(data: Claim) {
    if (this.claimId) {
      if (data) {
        this.isLoadingPage = true;
        this.claim = data;
        if (this.claim) {
          this.claimId = this.claim.id;
          this.orderStatus.setValue(this.claim.claimStatusId);
          this.store.setValue(this.claim.storeId);
          this.insuranceCompany.setValue(this.claim.insuranceCompanyId);
          this.claimType.setValue(this.claim.claimTypeId);
          this.customerFullName.setValue(this.claim.customerFullName);
          this.policyNumber.setValue(this.claim.policyNumber);
          this.policySubsection.setValue(this.claim.policySubsection);
          this.claimNumber.setValue(this.claim.claimNumber);
          this.reportNumber.setValue(this.claim.reportNumber);
          this.externalOrderNumber.setValue(this.claim.externalOrderNumber);
          this.depositSlip.setValue(String(Number(this.claim.hasDepositSlip)));
          this.isDepositSlipRequired = this.claim.isDepositSlipRequired;
          this.itemsQuantity.setValue(this.claim.itemsQuantity);
          this.claimDocuments = this.claim.claimDocuments;
          this.claimProbatoryDocuments = this.claim.claimProbatoryDocuments;
        }
        this.isLoadingPage = false;
      }
    }
  }

  download(request: DownloadMediaRequest) {
    this.mediaService.downloadMedia(request.id).subscribe(blob => {
      const downloadURL = window.URL.createObjectURL(blob);
      const image = <NgxGalleryImage> {
        small: downloadURL,
        medium: downloadURL,
        big: downloadURL
      };
      this.galleryImages.push(image);
    },
    error => {
        console.log(error?.message);
        this.openSnackBar(`Cannot download the image of ${request.description}`, 'Dismiss');
    }
    );
  }

  get isSaveBtnVisible(): boolean {
    // If the current user role is Insurance Read-only then disable it
    if (this.authService.currentUser.roleId == UserRoleId.InsuranceReadOnly) return false;

    // If it's a new order then enable it
    if (!this.claim) return true;

    // If the order is cancelled or invoiced then disable it
    if (this.claim.claimStatusId == environment.ClaimStatusCancelled || this.claim.claimStatusId == environment.ClaimStatusInvoiced) return false;

    // If the current user role is Store Admin or Operator
    if (this.authService.currentUser.roleId == UserRoleId.WsAdmin || this.authService.currentUser.roleId == UserRoleId.WsOperator) {
      // ... and the order status is in progress then enable it
      if (this.claim.claimStatusId == environment.ClaimStatusInProcess) return true;
      else return false;
    }

    // If the current user role is Admin then enable it for the order status that are not cancelled nor invoiced
    if (this.authService.currentUser.roleId == UserRoleId.Admin) {
      return true;
    }

    return false;
  }

  get readonlyFormControl() {
    return !this.isSaveBtnVisible;
  }

  onInsuranceCompanySelectionChange() {
    this.claimType.reset();
  }

  get getClaimTypes(): ClaimType[] | [] {
    if (this.insuranceCompany.value) {
      const claimTypeSettings = this.insuranceCompanies.find(x => x.id == this.insuranceCompany.value).claimTypeSettings;
      if (claimTypeSettings) {
        const enabledClaimTypes = Object.values(claimTypeSettings).filter(value => value.isClaimServiceEnabled).map(m => m.claimTypeId);
        if (enabledClaimTypes) {
          return this.claimTypes.filter(claimType => enabledClaimTypes.find(enabledClaimTypeId => enabledClaimTypeId == claimType.id));
        }
      }
    }
    return [];
  }

  get claimStatusList(): {id: number, name: string}[] {
    let statusOptions = [environment.ClaimStatusInProcess];

    if (this.claim) {
      // These are the statuses an order can go based in its current status
      statusOptions = this.claimStatusOptions[this.claim.claimStatusId];

      // If the current user role is Insurance Read-only then display the current status on order only
      if (this.authService.currentUser.roleId === UserRoleId.InsuranceReadOnly) statusOptions = [this.claim.claimStatusId];

      // If the current user role is Store Admin (or Operator) and the order is Cancelled or Invoiced then display the current status on order only
      if (this.authService.currentUser.roleId != UserRoleId.Admin && this.claim.claimStatusId > environment.ClaimStatusCompleted) statusOptions = [this.claim.claimStatusId];

      // If the current user role is Store Admin (or Operator) and the order is in status Completed then display these two statuses only: Completed (default), In Process
      if (this.authService.currentUser.roleId != UserRoleId.Admin && this.claim.claimStatusId == environment.ClaimStatusCompleted) statusOptions = [environment.ClaimStatusCompleted, environment.ClaimStatusInProcess];

      // If the current status on order is Invoiced then display the current status on order only
      if (this.claim.claimStatusId >= environment.ClaimStatusInvoiced) statusOptions = [this.claim.claimStatusId];
    }

    return Object.entries(environment.ClaimStatus).filter(s => statusOptions.find(status => status == +s[0])).map(item => { 
      return { 
        id: +item[0], name: item[1] 
      }
    });
  }

  onStatusChange($event: any) {
    if (!this.myForm.valid) {
      const dialogRef = this.openInfoDialog('Cambio de estatus', 'Error: No se puede cambiar el estatus porque faltan datos en la orden.', 'error');
      dialogRef.afterClosed().subscribe();
      this.orderStatus.setValue(this.claim.claimStatusId);
      return;
    }
    if (+$event.value == environment.ClaimStatusInProcess) {
      if (this.claim.source?.length > 0) {
        const dialogRef = this.openInfoDialog('Cambio de estatus', `Error: Solo es permitido cambiar el estatus de la orden por medio de ${this.claim.source}.`, 'error');
        dialogRef.afterClosed().subscribe();
        this.orderStatus.setValue(this.claim.claimStatusId);
        return;
      }
    }
    if (+$event.value == environment.ClaimStatusCompleted && !this.isReceiptCompleted) {
      const dialogRef = this.openInfoDialog('Cambio de estatus', 'Error: No se puede cambiar el estatus porque falta el recibo firmado.', 'error');
      dialogRef.afterClosed().subscribe();
      this.orderStatus.setValue(this.claim.claimStatusId);
      return;
    }
    if (+$event.value == environment.ClaimStatusCompleted && !this.arePostSignatureDocsCompleted) {
      const dialogRef = this.openInfoDialog('Cambio de estatus', 'Error: No se puede cambiar el estatus porque faltan documentos post-firma del recibo.', 'error');
      dialogRef.afterClosed().subscribe();
      this.orderStatus.setValue(this.claim.claimStatusId);
      return;
    }
    if (+$event.value == environment.ClaimStatusInvoiced) {
      if (this.claim.source?.length > 0) {
        const dialogRef = this.openInfoDialog('Cambio de estatus', `Error: Solo es permitido cambiar el estatus de la orden por medio de ${this.claim.source}.`, 'error');
        dialogRef.afterClosed().subscribe();
        this.orderStatus.setValue(this.claim.claimStatusId);
        return;
      }
      if (this.hasTpaDocs && !this.areTpaDocsCompleted) {
        const dialogRef = this.openInfoDialog('Cambio de estatus', 'Error: No se puede cambiar el estatus porque faltan documentos TPA.', 'error');
        dialogRef.afterClosed().subscribe();
        this.orderStatus.setValue(this.claim.claimStatusId);
        return;
      }
    }
    if (+$event.value == environment.ClaimStatusCancelled) {
      if (this.claim.source?.length > 0) {
        const dialogRef = this.openInfoDialog('Cambio de estatus', `Error: Solo es permitido cambiar el estatus de la orden por medio de ${this.claim.source}.`, 'error');
        dialogRef.afterClosed().subscribe();
        this.orderStatus.setValue(this.claim.claimStatusId);
        return;
      }
    }
    const confirmDialog = this.openConfirmDialog('Cambio de estatus', `Confirmar: Seguro(a) que desea cambiar el estatus de ${environment.ClaimStatus[this.claim.claimStatusId]} a ${environment.ClaimStatus[$event.value]}?`);
    confirmDialog.afterClosed().pipe(
      switchMap(answer => {
        return observableOf(answer);
      })
    )
    .subscribe(answer => {
        if (answer) {
          this.claim.claimStatusId = +$event.value;
          this.claimService.updateStatus(this.claim.id, +$event.value).subscribe(() => {
            const dialogRef = this.openInfoDialog('Cambio de estatus', 'Info: El cambio de estatus se ha completado.', 'info');
            dialogRef.afterClosed().subscribe();
          });
        } else {
          this.orderStatus.setValue(this.claim.claimStatusId);
        }
    });
  }

  get isOrderStatusControlDisabled(): boolean {
    // If the current user role is Insurance Read-only then you can NOT change the status
    if (this.authService.currentUser.roleId === UserRoleId.InsuranceReadOnly) return true;
    // If the claim is being edited
    if (this.claim) {
      // If the claim is invoiced then you NO LONGER can change the status
      if (this.claim.claimStatusId === environment.ClaimStatusInvoiced) return true;
      // If the claim is completed
      if (this.claim.claimStatusId === environment.ClaimStatusCompleted) {
        // ... and the current user role is Admin then you can change the status 
        if (this.authService.currentUser.roleId === UserRoleId.Admin) {
          return false;
        } else {
          return true;
        }
      }
      // If the claim is in process then you DON'T need manually change the status
      if (this.claim.claimStatusId === environment.ClaimStatusInProcess) {
        return true;
      }
    }
    return true;
  }

  onClaimTypeSelectionChange() {
    if (!this.claim) {
      if (this.insuranceCompany.value && this.claimType.value) {
        const company = this.insuranceCompanies.filter(x => x.id == this.insuranceCompany.value);
        if (company && company.length > 0 && company[0].claimTypeSettings) {
          const claimTypeSettings = company[0].claimTypeSettings[this.claimType.value];
          if (claimTypeSettings && claimTypeSettings.isDepositSlipRequired) {
            this.isDepositSlipRequired = true;
            this.enableDisableDepositSlipControl();
            return;
          }
        }
      }
      this.isDepositSlipRequired = false;
      this.enableDisableDepositSlipControl();
    }
  }

  enableDisableDepositSlipControl() {
    if (this.isDepositSlipRequired) {
      this.depositSlip.enable();
    } else {
      this.depositSlip.disable();
    }
  }

  isAllowedToSeeDocs(tableId: number): boolean {
    if (this.currentUserRoleId == UserRoleId.Admin) return true;
    
    if (tableId == TableInsuranceZipFiles) {
      if (this.currentUserRoleId == UserRoleId.InsuranceReadOnly)
        return true;
      else
        return false;
    }

    if (tableId == TableInsuranceElectronicInvoice) {
      if (this.currentUserRoleId == UserRoleId.InsuranceReadOnly)
        return true;
      else
        return false;
    }

    if (tableId == TableAdministrativeDocuments) {
      if (this.currentUserRoleId == UserRoleId.WsAdmin)
        return true;
      else
        return false;
    }

    if (tableId == TablePictures) {
      if (this.currentUserRoleId == UserRoleId.WsAdmin || this.currentUserRoleId == UserRoleId.WsOperator)
        return true;
      else
        return false;
    }

    if (tableId == TableReceipt) {
      if (this.currentUserRoleId == UserRoleId.WsAdmin || this.currentUserRoleId == UserRoleId.WsOperator)
        return true;
      else
        return false;
    }

    return false;
  }
  
  get isAllowedToEditOrder(): boolean {
    if (this.currentUserRoleId == UserRoleId.InsuranceReadOnly)
      return false;
    else
      return true;
  }

  populateGallery(): void {
    this.galleryImages = [];
    if (!this.isAllowedToSeeDocs(TableAdministrativeDocuments)) {
      this.claimProbatoryDocuments.filter(x => x.media && x.groupId === PictureDocumentsGroup).map(x => {
        const request = <DownloadMediaRequest> {
          id: x.media.id,
          description: x.probatoryDocument.name
        };
        this.galleryImagesSubject.next(request);
      });
      return;
    }

    this.claimProbatoryDocuments.filter(x => x.media && x.groupId != TpaDocsGroup).map(x => {
      const request = <DownloadMediaRequest> {
        id: x.media.id,
        description: x.probatoryDocument.name
      };
      this.galleryImagesSubject.next(request);
    });
    return;
  }

  getDocumentsByGroupId(groupId: number) {
    return this.claimDocuments.filter(d=> d.groupId === groupId);
  }

  getProbatoryDocumentsByGroupId(groupId: number) {
    return this.claimProbatoryDocuments.filter(d=> d.groupId === groupId);
  }

  getProbatoryDocumentsByGroupIdAndClaimItemId(groupId: number, claimItemId: number) {
    return this.claimProbatoryDocuments.filter(d=> d.groupId === groupId && (d.claimItemId && d.claimItemId === claimItemId));
  }

  toClaimModel(): Claim {
    const claim: Claim = {
      id: this.claimId,
      claimStatusId: this.orderStatus.value,
      storeId: this.store.value,
      insuranceCompanyId: this.insuranceCompany.value,
      customerFullName: this.customerFullName.value,
      claimTypeId: this.claimType.value,
      policyNumber: this.policyNumber.value,
      policySubsection: this.policySubsection.value,
      claimNumber: this.claimNumber.value,
      reportNumber: this.reportNumber.value,
      externalOrderNumber: this.externalOrderNumber.value,
      isDepositSlipRequired: this.isDepositSlipRequired,
      hasDepositSlip: this.isDepositSlipRequired && this.depositSlip.value === '1',
      itemsQuantity: +this.itemsQuantity.value,
      createdByUserId: this.claimId ? this.claim.createdByUserId : this.authService.currentUser.id,
      claimProbatoryDocumentStatusId: this.claimId ? this.claim.claimProbatoryDocumentStatusId : ClaimProbatoryDocumentStatusId.InProgress
    };
    return claim;
  }

  upsert() {
    if (!this.myForm.valid) return;
    this.saveBtnIsDisabled = true;
    this.claim = this.toClaimModel();
    if (!this.claimId) {
      this.claimService.insert(this.claim)
      .subscribe((response: Claim) => {
        this.claimId = response.id;
        this.claimDocuments = [];
        this.claimDocuments = response.claimDocuments;
        this.claimProbatoryDocuments = [];
        this.claimProbatoryDocuments = response.claimProbatoryDocuments;
        this.openSnackBar("Operation completed.", "Dismiss");
      },
      (error: AppError) => {
        if (error instanceof BadInput) {
          this.openSnackBar(error.originalError.message, "Dismiss");
        }
        else throw error;
      })
      .add(() => this.saveBtnIsDisabled = false);
    } else {
      this.claimService.update(this.claim)
      .subscribe((response: Claim) => {
        if (response.claimDocuments || response.claimProbatoryDocuments) {
          this.claimDocuments = [];
          this.claimDocuments = response.claimDocuments;
          this.claimProbatoryDocuments = [];
          this.claimProbatoryDocuments = response.claimProbatoryDocuments;
        }
        this.openSnackBar("Operation completed.", "Dismiss");
      },
      (error: AppError) => {
        if (error instanceof BadInput) {
          this.openSnackBar(error.originalError.message, "Dismiss");
        }
        else throw error;
      })
      .add(() => this.saveBtnIsDisabled = false);
    }
  }

  openSnackBar(message: string, action: string) {
    this.snackBar.open(message, action, {
      duration: 3000,
    });
  }

  onExportDocumentChange(newClaimDocument: ClaimDocument) {
    // Add the new document and display on screen
    this.claimDocuments.push(newClaimDocument);
  }

  onShowImageListChange(value: boolean) {
    this.showImageList = value;
    if (!this.showImageList && this.galleryImages.length == 0) {
      this.populateGallery();
    }
  }

  // When a new probatory document is uploaded it shuold refresh the galleryImages and verify onAllDocumentsCompleted
  onProbatoryDocumentChange(request: DownloadMediaRequest) {
    if (this.galleryImages.length > 0) {
      this.galleryImagesSubject.next(request);
    }
  }
  
  getNameOfStore(s: Store) {
    return s.sapNumber ? s.sapNumber + '-' + s.name : s.name;
  }

  // get isElectronicInvoiceCompleted(): boolean {
  //   const electronicInvoiceDocs = this.getDocumentsByGroupId(1);
  //   const incompletedDocs = electronicInvoiceDocs.filter(x => x.statusId != 40);
  //   if (incompletedDocs.length > 0) {
  //     return false;
  //   }
  //   else {
  //     return true;
  //   }
  // }

  externalOrderNumberAsyncValidator(time: number = 500): AsyncValidatorFn {
    return (control: AbstractControl): Observable<ValidationErrors | null> => {
      if (!control || !control?.value || control?.value?.length == 0) return observableOf<Object>(null);
      // return this.claimService.isExternalOrderNumberTaken(this.claimId, control.value).pipe(
      //   map(result => {
      //     return result ? { externalOrderNumberExist: true } : null;
      //   })
      // );
      return timer(time).pipe(
        switchMap(() => this.claimService.isExternalOrderNumberTaken(this.claimId, control.value)),
        map(exist => {
          return exist ? { externalOrderNumberExist: true } : null
        })
      );
    }
  }
  
  get isExternalOrderNumberProvided(): boolean {
    if (this.externalOrderNumber.value && this.externalOrderNumber.value.length > 0 && this.externalOrderNumber.valid
        && this.claim?.externalOrderNumber && this.claim?.externalOrderNumber?.length > 0) {
      return true;
    }
    return false;
  }

  get areAllStoreDocumentsCompleted(): boolean {
    return this.areAdminDocsCompleted && this.arePicturesCompleted && this.arePicturesXItemCompleted;
  }

  get hasTpaDocs(): boolean {
    const docs = this.getProbatoryDocumentsByGroupId(4);
    return docs.length > 0 ? true : false;
  }

  get areTpaDocsCompleted(): boolean {
    const docs = this.getProbatoryDocumentsByGroupId(4);
    const missingMedia = docs.find(x => !x.media);
    if (!missingMedia) {
      return true;
    }
    return false;
  }

  get areAdminDocsCompleted(): boolean {
    const docs = this.getProbatoryDocumentsByGroupId(1);
    const missingMedia = docs.find(x => !x.media);
    if (!missingMedia) {
      return true;
    }
    return false;
  }

  get arePicturesCompleted(): boolean {
    const docs = this.getProbatoryDocumentsByGroupId(2);
    const missingMedia = docs.find(x => !x.media);
    if (!missingMedia) {
      return true;
    }
    return false;
  }

  get arePicturesXItemCompleted(): boolean {
    const docs = this.getProbatoryDocumentsByGroupId(3);
    const missingMedia = docs.find(x => !x.media);
    if (!missingMedia) {
      return true;
    }
    return false;
  }

  get isReceiptCompleted(): boolean {
    const docs = this.getProbatoryDocumentsByGroupId(5);
    const missingMedia = docs.find(x => !x.media);
    if (!missingMedia) {
      return true;
    }
    return false;
  }

  // Dev notes: The post-signature documents are not mandatory for the receipt signature,
  // but they are required before you can change the status of the order to completed.
  // If the order doesn't have this type of documents then the result should be defaulted to true.
  get arePostSignatureDocsCompleted(): boolean {
    const docs = this.getProbatoryDocumentsByGroupId(6);
    if (docs.length == 0) {
      return true;
    }
    const missingMedia = docs.find(x => !x.media);
    if (!missingMedia) {
      return true;
    }
    return false;
  }

  get isStoreAllowedToZipAndEmailClaimFiles(): boolean {
    return this.isAllowedToEditOrder 
        && this.claimId > 0 
        && this.areAdminDocsCompleted 
        && this.arePicturesCompleted 
        && this.arePicturesXItemCompleted 
        && this.isReceiptCompleted 
        && this.isAllowedToSeeDocs(5)
        && (this.authService.currentUser.roleId == UserRoleId.WsAdmin || this.authService.currentUser.roleId == UserRoleId.WsOperator);
  }

  zipAndEmailClaimFiles() {
    this.claimProbatoryDocumentsService.zipAndEmailClaimFiles(this.claimId, this.authService.currentUser.email).subscribe(() => {
      this.openSnackBar("Operation completed.", "Dismiss");
    },
    (error: AppError) => {
      if (error instanceof BadInput) {
        this.openSnackBar(error.originalError.message, "Dismiss");
      }
      else throw error;
    });
  }

  refreshDocument(nx: Notification) {
    if (Notification.jsonParse(nx).claimId == this.claimId) {
      if (nx.messageType === 'ZipClaimFilesReady' && this.claimZipFile && this.claimZipFile.statusId != 40) {
        // Update state on insurance zip file
        const dx: Document = {
          id: 0,
          mimeType: null,
          documentId: Notification.jsonParse(nx).documentId,
          metadataTitle: null,
          metadataAlt: null,
          dateCreated: null
        };
        this.claimZipFile.document = dx;
        this.claimZipFile.document.id = dx.documentId;
        this.claimZipFile.statusId = 40;
      }
      if (nx.messageType === 'PdfClaimFilesReady' && this.claimPdfFile && this.claimPdfFile.statusId != 40) {
        // Update state on insurance zip file
        const dx: Document = {
          id: 0,
          mimeType: null,
          documentId: Notification.jsonParse(nx).documentId,
          metadataTitle: null,
          metadataAlt: null,
          dateCreated: null
        };
        this.claimPdfFile.document = dx;
        this.claimPdfFile.document.id = dx.documentId;
        this.claimPdfFile.statusId = 40;
      }
    }
  }

  getItemsQuantity() {
    if (this.claim && this.claim.itemsQuantity) {
      return Array(this.claim.itemsQuantity);
    }
    return Array(0);
  }

  get isformGroup1Invalid() {
    return this.myForm.controls.formGroup1.touched && this.myForm.controls.formGroup1.invalid;
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

  ngOnDestroy() {
    // unsubscribe to ensure no memory leaks
    this.notificationStoreSubscription.unsubscribe();
  }
}

import { Component, Inject, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialog, MatDialogConfig, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { switchMap } from 'rxjs/operators';
import { ClaimType } from 'src/app/models/claim-type';
import { InsuranceCollage } from 'src/app/models/insurance-collage';
import { InsuranceCollageProbatoryDocument } from 'src/app/models/insurance-collage-probatory-document';
import { InsuranceProbatoryDocument } from 'src/app/models/insurance-probatory-document';
import { ClaimTypeService } from 'src/app/services/claim-type.service';
import { InsuranceCollageService } from 'src/app/services/insurance-collage-service';
import { InsuranceProbatoryDocumentService } from 'src/app/services/insurance-probatory-document.service';
import { AppError } from '../../common/app-error';
import { BadInput } from '../../common/bad-input';
import { ConfirmDialogComponent } from '../confirm-dialog/confirm-dialog.component';
import { of as observableOf } from 'rxjs';

@Component({
  selector: 'app-collage-dialog',
  templateUrl: './collage-dialog.component.html',
  styleUrls: ['./collage-dialog.component.css']
})
export class CollageDialogComponent implements OnInit {
  isLoadingPage: boolean;
  saveBtnIsDisabled: boolean = false;
  readonlyFormControl: boolean = false;

  action: string = "Add";
  companyId: number;
  collageId: number;
  collage: InsuranceCollage | null;

  claimTypeIdSelected: number;
  claimTypes: ClaimType[] = [];
  insuranceProbatoryDocuments: InsuranceProbatoryDocument[] = [];
  checklistItems: { id: number, name: string, checked: boolean }[] = [];

  myForm: FormGroup;
  collageName = new FormControl('', [Validators.required]);
  claimType = new FormControl('', [Validators.required]);
  collageColumns = new FormControl('1', [Validators.required, Validators.pattern('^[1-9][0-9]*')]);

  constructor(
    private dataService: InsuranceCollageService,
    private claimTypeService: ClaimTypeService,
    private insuranceProbatoryDocumentService: InsuranceProbatoryDocumentService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog,
    private dialogRef: MatDialogRef<CollageDialogComponent>,
    @Inject(MAT_DIALOG_DATA) data) { 
      this.companyId = data.companyId;
      this.collageId = data.collageId;
    }

  ngOnInit() {
    this.myForm = new FormGroup({
      collageName: this.collageName,
      claimType: this.claimType,
      collageColumns: this.collageColumns
    });

    this.initializeFormFields();

    if (this.collageId) {
      this.action = "Edit";
    }
    this.populate();
  }

  initializeFormFields() {
    this.claimTypeService.getAll().subscribe((data: ClaimType[]) => this.claimTypes = data);
  }

  populate() {
    if (this.collageId) {
      this.isLoadingPage = true;
      this.dataService.getById(this.collageId).subscribe((data: InsuranceCollage) => {
        if (data) {
          this.collage = data;
          this.collageId = this.collage.id;
          this.collageName.setValue(this.collage.name);
          this.collageColumns.setValue(this.collage.columns);
          this.claimTypeIdSelected = this.collage.claimTypeId;
          this.claimType.setValue(this.collage.claimTypeId);
          this.claimType.disable();
          this.readonlyFormControl = true;
          this.populateCollageDocuments();
        }
        this.isLoadingPage = false;
      });
    }
  }

  populateCollageDocuments() {
    // Get the list of probatory documents for this company and service/claim type
    this.isLoadingPage = true;
    this.insuranceProbatoryDocumentService.getByInsuranceCompanyIdAndClaimTypeId(this.companyId, this.claimTypeIdSelected).subscribe((data: InsuranceProbatoryDocument[]) => {
      if (data) {
        this.insuranceProbatoryDocuments = data;
        this.checklistItems = data.map((d: InsuranceProbatoryDocument) => {
          return {
            id: d.probatoryDocumentId,
            name: d.probatoryDocument.name,
            checked: false
          };
        });
        // Tick the checkboxes of the documents that exist in the collage if any
        if (this.collageId && this.collage && this.collage.probatoryDocuments && this.collage.probatoryDocuments.length > 0) {
          this.checklistItems.forEach(i => {
            const docsInCollage = this.collage.probatoryDocuments.filter((pd :InsuranceCollageProbatoryDocument) => pd.probatoryDocumentId == i.id);
            if (docsInCollage && docsInCollage.length > 0) {
              i.checked = true;
            }
            else {
              i.checked = false;
            }
          });
        }
      }
      this.isLoadingPage = false;
    });
  }

  toModel(): InsuranceCollage {
    const collage: InsuranceCollage = {
      id: this.collageId ? this.collageId : 0,
      insuranceCompanyId: this.companyId,
      claimTypeId: this.claimTypeIdSelected,
      name: this.collageName.value,
      columns: +this.collageColumns.value
    };
    const collageDocuments = this.checklistItems.filter(li => li.checked).map((i: any) => {
      return <InsuranceCollageProbatoryDocument> {
        insuranceCollageId: this.collageId ? this.collageId : 0,
        probatoryDocumentId: i.id
      };
    });
    collage.probatoryDocuments = collageDocuments;
    return collage;
  }

  onSubmit() {
    if (!this.myForm.valid) return;
    this.collage = this.toModel();
    if (!this.collageId) {
      this.isLoadingPage = true;
      this.dataService.insert(this.collage)
        .subscribe((response: InsuranceCollage) => {
          this.collageId = response.id;
          this.openSnackBar("Operation completed.", "Dismiss");
          // CLOSE WINDOW HERE
        },
        (error: AppError) => {
          if (error instanceof BadInput) {
            this.openSnackBar(error.originalError.message, "Dismiss");
          }
          else throw error;
        })
        .add(() => {
          this.saveBtnIsDisabled = false;
          this.isLoadingPage = false;
          this.close(this.collage);
        });
    } else {
      this.isLoadingPage = true;
      this.dataService.update(this.collage)
        .subscribe(() => {
          this.openSnackBar("Operation completed.", "Dismiss");
        },
        (error: AppError) => {
          if (error instanceof BadInput) {
            this.openSnackBar(error.originalError.message, "Dismiss");
          }
          else throw error;
        })
        .add(() => {
          this.saveBtnIsDisabled = false;
          this.isLoadingPage = false;
        });
    }
  }

  openConfirmDialog() {
    const dialogConfig = new MatDialogConfig();
    dialogConfig.disableClose = true;
    dialogConfig.autoFocus = true;
    dialogConfig.data = { 
      title: 'Confirm Remove Collage',
      message: 'Are you sure you want to remove the collage?'
    };
    return this.dialog.open(ConfirmDialogComponent, dialogConfig);
  }

  onDelete() {
    const confirmDialog = this.openConfirmDialog();
    confirmDialog.afterClosed().pipe(
      switchMap(answer => {
        if (answer) {
          this.dataService.delete(this.collageId).subscribe(() => {
            this.close(this.collage);
          });
        }
        return observableOf(answer);
      })
    )
    .subscribe(answer => {
        console.log(`Collage deleted: ${answer}`);
    });
  }

  onClaimTypeSelectionChange($event) {
    this.claimTypeIdSelected = $event.value as number;
    this.populateCollageDocuments();
  }

  openSnackBar(message: string, action: string) {
    this.snackBar.open(message, action, {
      duration: 3000,
    });
  }

  close(collage: InsuranceCollage) {
    if  (collage) {
      this.dialogRef.close(collage)
    } else {
      this.dialogRef.close();
    }
  }
}

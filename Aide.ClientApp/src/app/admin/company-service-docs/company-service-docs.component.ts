import { Component, OnInit, Input, ViewChild } from '@angular/core';
import { ProbatoryDocument } from 'src/app/models/probatory-document';
import { InsuranceProbatoryDocument } from 'src/app/models/insurance-probatory-document';
import { ClaimTypeId } from 'src/app/enums/claim-type-id.enum';
import { SelectionModel } from '@angular/cdk/collections';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableDataSource, MatTable } from '@angular/material/table';
import { CdkDragDrop, moveItemInArray, transferArrayItem, CdkDropList } from '@angular/cdk/drag-drop';
import { ProbatoryDocumentRow } from './probatory-document-row';
import { AppError } from 'src/app/shared/common/app-error';
import { BadInput } from 'src/app/shared/common/bad-input';
import { InsuranceProbatoryDocumentService } from 'src/app/services/insurance-probatory-document.service';
import { CompanyClaimTypeSettings } from 'src/app/models/company-claim-type-settings';
import { FormControl, FormGroup } from '@angular/forms';
import { MatSlideToggleChange } from '@angular/material/slide-toggle';
import { InsuranceCompanyService } from 'src/app/services/insurance-company.service';
import { forkJoin } from 'rxjs';
import clonedeep from 'lodash.clonedeep';

const AdminDocsGroupId: number = 1;
const DepositSlipId: number = 21;

@Component({
  selector: 'app-company-service-docs',
  templateUrl: './company-service-docs.component.html',
  styleUrls: ['./company-service-docs.component.css']
})
export class CompanyServiceDocsComponent implements OnInit {
  @Input() 
  public set companyId(value: number) {
    this._companyId = value;
  }

  @Input()
  public set claimTypeId(value: ClaimTypeId) {
    this._claimType = value;
  }

  @Input()
  public set companyClaimTypeSettings(value: CompanyClaimTypeSettings) {
    this._companyClaimTypeSettings = value;
  }

  @Input()
  public set insuranceProbatoryDocuments(values: InsuranceProbatoryDocument[]) { // These are the probatory documents per service type that are currently assigned to the insurance company
    if (values && values.length > 0) {
      this._insuranceProbatoryDocuments = values.sort((a, b) => (a.sortPriority < b.sortPriority) ? -1 : 1);
      this.populate();
    }
  }
  
  @Input() 
  public set allProbatoryDocuments(values: ProbatoryDocument[]) { // This is the full catalog of probatory documents
    if (values && values.length > 0) {
      this._allProbatoryDocuments = values;
      this.populate();
    }
  }
  
  _companyId: number;
  _claimType: ClaimTypeId;
  _companyClaimTypeSettings: CompanyClaimTypeSettings;
  _insuranceProbatoryDocuments: InsuranceProbatoryDocument[] = <InsuranceProbatoryDocument[]>[];
  _allProbatoryDocuments: ProbatoryDocument[] = <ProbatoryDocument[]>[];

  currentClaimTypeProbatoryDocuments: ProbatoryDocumentRow[];
  remainingClaimTypeProbatoryDocuments: ProbatoryDocumentRow[];
  currentClaimTypeProbatoryDocumentsDataSource;
  remainingClaimTypeProbatoryDocumentsDataSource;
  displayedColumns1 = ['name', 'group'];
  displayedColumns2 = ['name'];
  selection = new SelectionModel<ProbatoryDocument>(true, []);
  saveBtnIsDisabled: boolean = false;

  @ViewChild('table1') table1: MatTable<any>;
  @ViewChild('table2') table2: MatTable<any>;
  @ViewChild('list1') list1: CdkDropList;

  myForm: FormGroup;
  isDepositSlipRequired = new FormControl();
  isClaimServiceEnabled = new FormControl();

  constructor(private dataService: InsuranceProbatoryDocumentService, private insuranceCompanyService: InsuranceCompanyService, private snackBar: MatSnackBar) { }

  ngOnInit() {
    this.myForm = new FormGroup({
      isDepositSlipRequired: this.isDepositSlipRequired,
      isClaimServiceEnabled: this.isClaimServiceEnabled
    });
    this.populateSettings();
  }

  populateSettings() {
    if (this._companyClaimTypeSettings) {
      this.isDepositSlipRequired.setValue(this._companyClaimTypeSettings.isDepositSlipRequired);
      this.isClaimServiceEnabled.setValue(this._companyClaimTypeSettings.isClaimServiceEnabled);
    }
  }

  populate() {
    if (this._insuranceProbatoryDocuments) {
      this.currentClaimTypeProbatoryDocuments = this.getCurrentClaimTypeProbatoryDocuments(this._claimType);
      this.currentClaimTypeProbatoryDocumentsDataSource = new MatTableDataSource(this.currentClaimTypeProbatoryDocuments);
    }
    if (this._allProbatoryDocuments) {
      this.remainingClaimTypeProbatoryDocuments = this.getRemainingClaimTypeProbatoryDocuments(this._claimType);
      this.remainingClaimTypeProbatoryDocumentsDataSource = new MatTableDataSource<ProbatoryDocumentRow>(this.remainingClaimTypeProbatoryDocuments.map(doc => {
        return doc;
        // return <ProbatoryDocument> {
        //   id: doc.id,
        //   name: doc.name
        // };
      }));
    }
  }

  // Get the list of probatory documents per claim type that are currently assigned to the insurance company
  getCurrentClaimTypeProbatoryDocuments(claimTypeId: ClaimTypeId): ProbatoryDocumentRow[] {
    const currentClaimTypeProbatoryDocuments = this._insuranceProbatoryDocuments?.filter(x => x.claimTypeId == claimTypeId && x.insuranceCompanyId == this._companyId).map(doc => {
      return <ProbatoryDocumentRow> {
        id: doc.probatoryDocumentId,
        name: doc.probatoryDocument.name,
        groupId: doc.groupId
      };
    });
    if (currentClaimTypeProbatoryDocuments) {
      return currentClaimTypeProbatoryDocuments;
    } else {
      return <ProbatoryDocumentRow[]>[];
    }
  }

  // Get the list of remaining and available probatory documents for a given claim type
  getRemainingClaimTypeProbatoryDocuments(claimTypeId: ClaimTypeId): ProbatoryDocumentRow[] {
    const currentClaimTypeProbatoryDocuments = this.getCurrentClaimTypeProbatoryDocuments(claimTypeId);
    const remainingClaimTypeProbatoryDocuments = this._allProbatoryDocuments?.filter(probatoryDoc => currentClaimTypeProbatoryDocuments.findIndex(currentDoc => currentDoc.name == probatoryDoc.name) == -1).map(doc => {
      return <ProbatoryDocumentRow> {
        id: doc.id,
        name: doc.name,
        groupId: AdminDocsGroupId // Here defaulted to Admin Docs
      };
    });
    if (remainingClaimTypeProbatoryDocuments) {
      return remainingClaimTypeProbatoryDocuments;
    } else {
      return <ProbatoryDocumentRow[]>[];
    }
  }

  drop(event: CdkDragDrop<string[]>) {
    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
    } else {
      transferArrayItem(event.previousContainer.data,
                        event.container.data,
                        event.previousIndex,
                        event.currentIndex);
    }

    // updates moved data and table, but not dynamic if more dropzones
    this.currentClaimTypeProbatoryDocumentsDataSource.data = clonedeep(this.currentClaimTypeProbatoryDocumentsDataSource.data);
    this.remainingClaimTypeProbatoryDocumentsDataSource.data = clonedeep(this.remainingClaimTypeProbatoryDocumentsDataSource.data);

    // check/uncheck the toggle button if the deposit slip is added to required docs
    const isDepositSlipRequired = this.currentClaimTypeProbatoryDocumentsDataSource.data.findIndex(x => x.id == DepositSlipId);
    this.isDepositSlipRequired.setValue(isDepositSlipRequired >= 0 ? true : false);
  }
  
  onDepositSlipRequiredChange(event: MatSlideToggleChange) {
    if (event.checked) {
      const index = this.remainingClaimTypeProbatoryDocuments.findIndex(x => x.id == DepositSlipId);
      if (index >= 0) {
        const item = this.remainingClaimTypeProbatoryDocuments[index];
        this.currentClaimTypeProbatoryDocuments.unshift(item);
        this.remainingClaimTypeProbatoryDocuments.splice(index, 1);
      }
    } else {
      const index = this.currentClaimTypeProbatoryDocuments.findIndex(x => x.id == DepositSlipId);
      if (index >= 0) {
        const item = this.currentClaimTypeProbatoryDocuments[index];
        this.remainingClaimTypeProbatoryDocuments.unshift(item);
        this.currentClaimTypeProbatoryDocuments.splice(index, 1);
      }
    }
    this.currentClaimTypeProbatoryDocumentsDataSource = new MatTableDataSource(this.currentClaimTypeProbatoryDocuments);
    this.remainingClaimTypeProbatoryDocumentsDataSource = new MatTableDataSource<ProbatoryDocument>(this.remainingClaimTypeProbatoryDocuments.map(doc => {
      return <ProbatoryDocument> {
        id: doc.id,
        name: doc.name,
      };
    }));
  }

  toModel() {
    const docs = this.currentClaimTypeProbatoryDocumentsDataSource.data.map((doc: ProbatoryDocumentRow) => {
      return {
        probatoryDocumentId: doc.id,
        groupId: doc.groupId
      };
    });
    return docs;
  }

  upsert() {
    this.saveBtnIsDisabled = true;
    const docs = this.toModel();
    let upsertDocs = this.dataService.upsert(docs, this._companyId, this._claimType);

    const serviceTypeSettings = <CompanyClaimTypeSettings> {
      isDepositSlipRequired: this.isDepositSlipRequired.value ? true : false,
      isClaimServiceEnabled: this.isClaimServiceEnabled.value ? true : false
    };
    let upsertClaimTypeSettings = this.insuranceCompanyService.upsertClaimTypeSettings(serviceTypeSettings, this._companyId, this._claimType);
    
    forkJoin([upsertDocs, upsertClaimTypeSettings]).subscribe(results => {
      this.saveBtnIsDisabled = false;
      this.openSnackBar("Operation completed.", "Dismiss");
    },
    (error: AppError) => {
      this.saveBtnIsDisabled = false;
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

}

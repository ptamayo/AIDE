import { CdkDragDrop, CdkDropList, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { Component, Input, OnInit, ViewChild } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTable, MatTableDataSource } from '@angular/material/table';
import { CompanyClaimTypeSettings } from 'src/app/models/company-claim-type-settings';
import { InsuranceCompanyService } from 'src/app/services/insurance-company.service';
import { InsuranceProbatoryDocumentService } from 'src/app/services/insurance-probatory-document.service';
import { InsuranceExportProbatoryDocumentRow } from './insurance-export-probatory-document-row';
import { InsuranceExportProbatoryDocument } from 'src/app/models/insurance-export-probatory-document';
import { ClaimTypeId } from 'src/app/enums/claim-type-id.enum';
import { ExportTypeId } from 'src/app/enums/export-type-id';
import { FormControl, FormGroup } from '@angular/forms';
import { AppError } from 'src/app/shared/common/app-error';
import { BadInput } from 'src/app/shared/common/bad-input';
import { forkJoin } from 'rxjs';
import clonedeep from 'lodash.clonedeep';

@Component({
  selector: 'app-company-export-docs',
  templateUrl: './company-export-docs.component.html',
  styleUrls: ['./company-export-docs.component.css']
})
export class CompanyExportDocsComponent implements OnInit {

  public _exportTypeId: number;
  public _companyId: number;
  public _claimType: ClaimTypeId;
  public _companyClaimTypeSettings: CompanyClaimTypeSettings;

  @Input()
  public set exportTypeId(value: number) {
    this._exportTypeId = value;
  }

  @Input() 
  public set companyId(value: number) {
    this._companyId = value;
  }

  @Input()
  public set claimType(value: ClaimTypeId) {
    this._claimType = value;
  }

  @Input()
  public set companyClaimTypeSettings(value: CompanyClaimTypeSettings) {
    this._companyClaimTypeSettings = value;
  }

  @Input()
  public set insuranceProbatoryDocuments(values: InsuranceExportProbatoryDocument[]) { // These are the probatory documents per service type that are currently assigned to the insurance company
    if (values && values.length > 0) {
      this._insuranceProbatoryDocuments = values.sort((a, b) => (a.sortPriority < b.sortPriority) ? -1 : 1);
      this.populate();
    }
  }
  
  @Input() 
  public set allProbatoryDocuments(values: InsuranceExportProbatoryDocument[]) { // This is the full catalog of probatory documents
    if (values && values.length > 0) {
      this._allProbatoryDocuments = values;
      this.populate();
    }
  }
  
  _insuranceProbatoryDocuments: InsuranceExportProbatoryDocument[] = <InsuranceExportProbatoryDocument[]>[];
  _allProbatoryDocuments: InsuranceExportProbatoryDocument[] = <InsuranceExportProbatoryDocument[]>[];

  currentClaimTypeProbatoryDocuments: InsuranceExportProbatoryDocumentRow[];
  remainingClaimTypeProbatoryDocuments: InsuranceExportProbatoryDocumentRow[];
  currentClaimTypeProbatoryDocumentsDataSource;
  remainingClaimTypeProbatoryDocumentsDataSource;
  displayedColumns1 = ['name'];
  displayedColumns2 = ['name'];
  saveBtnIsDisabled: boolean = false;

  @ViewChild('table1') table1: MatTable<any>;
  @ViewChild('table2') table2: MatTable<any>;
  @ViewChild('list1') list1: CdkDropList;

  myForm: FormGroup;
  isExportingCustomizedDocs = new FormControl();

  constructor(private dataService: InsuranceProbatoryDocumentService, private insuranceCompanyService: InsuranceCompanyService, private snackBar: MatSnackBar) { }

  ngOnInit() {
    this.myForm = new FormGroup({
      isExportingCustomizedDocs: this.isExportingCustomizedDocs
    });
    this.populateSettings();
  }

  populateSettings() {
    if (this._companyClaimTypeSettings) {
      if (this._exportTypeId == ExportTypeId.PDF) {
        this.isExportingCustomizedDocs.setValue(this._companyClaimTypeSettings.isExportingCustomizedDocsToPdf);
      } else if (this._exportTypeId == ExportTypeId.ZIP) {
        this.isExportingCustomizedDocs.setValue(this._companyClaimTypeSettings.isExportingCustomizedDocsToZip);
      }
    }
  }

  populate() {
    if (this._insuranceProbatoryDocuments) {
      this.currentClaimTypeProbatoryDocuments = this.getCurrentClaimTypeProbatoryDocuments(this._claimType);
      this.currentClaimTypeProbatoryDocumentsDataSource = new MatTableDataSource(this.currentClaimTypeProbatoryDocuments);
    }
    if (this._allProbatoryDocuments) {
      this.remainingClaimTypeProbatoryDocuments = this.getRemainingClaimTypeProbatoryDocuments(this._claimType);
      this.remainingClaimTypeProbatoryDocumentsDataSource = new MatTableDataSource<InsuranceExportProbatoryDocumentRow>(this.remainingClaimTypeProbatoryDocuments.map(doc => {
        return doc;
        // return <ExportProbatoryDocumentRow> {
        //   // id: doc.id,
        //   name: doc.name
        // };
      }));
    }
  }

  // Get the list of probatory documents per claim type that are currently assigned to the insurance company
  getCurrentClaimTypeProbatoryDocuments(claimTypeId: ClaimTypeId): InsuranceExportProbatoryDocumentRow[] {
    const currentClaimTypeProbatoryDocuments = this._insuranceProbatoryDocuments?.filter(x => x.claimTypeId == claimTypeId && x.exportTypeId == this._exportTypeId && x.insuranceCompanyId == this._companyId).map(doc => {
      return <InsuranceExportProbatoryDocumentRow> {
        name: doc.name,
        exportDocumentTypeId: doc.exportDocumentTypeId,
        probatoryDocumentId: doc.probatoryDocumentId,
        collageId: doc.collageId,
        exportTypeId: doc.exportTypeId
      };
    });
    if (currentClaimTypeProbatoryDocuments) {
      return currentClaimTypeProbatoryDocuments;
    } else {
      return <InsuranceExportProbatoryDocumentRow[]>[];
    }
  }

  // Get the list of remaining and available probatory documents for a given claim type
  getRemainingClaimTypeProbatoryDocuments(claimTypeId: ClaimTypeId): InsuranceExportProbatoryDocumentRow[] {
    const currentClaimTypeProbatoryDocuments = this.getCurrentClaimTypeProbatoryDocuments(claimTypeId);
    const remainingClaimTypeProbatoryDocuments = this._allProbatoryDocuments?.filter(probatoryDoc => currentClaimTypeProbatoryDocuments.findIndex(currentDoc => currentDoc.name == probatoryDoc.name) == -1).map(doc => {
      return <InsuranceExportProbatoryDocumentRow> {
        name: doc.name,
        exportDocumentTypeId: doc.exportDocumentTypeId,
        probatoryDocumentId: doc.probatoryDocumentId,
        collageId: doc.collageId,
        exportTypeId: doc.exportTypeId
      };
    });
    if (remainingClaimTypeProbatoryDocuments) {
      return remainingClaimTypeProbatoryDocuments;
    } else {
      return <InsuranceExportProbatoryDocumentRow[]>[];
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
  }
  
  toModel() {
    if (this.currentClaimTypeProbatoryDocumentsDataSource) {
      const docs = this.currentClaimTypeProbatoryDocumentsDataSource.data.map((doc: InsuranceExportProbatoryDocumentRow) => {
        return { // This is the payload for the api request
          exportDocumentTypeId: doc.exportDocumentTypeId,
          probatoryDocumentId: doc.probatoryDocumentId,
          collageId: doc.collageId
        };
      });
      return docs;
    } else {
      return <InsuranceExportProbatoryDocumentRow[]>[];
    }
  }

  upsert() {
    this.saveBtnIsDisabled = true;
    const docs = this.toModel();
    let upsertDocs = this.dataService.upsertExportDocuments(docs, this._companyId, this._claimType, this._exportTypeId);

    const serviceTypeSettings: CompanyClaimTypeSettings = { claimTypeId: this._claimType };
    if (this._exportTypeId == ExportTypeId.PDF) {
      serviceTypeSettings.isExportingCustomizedDocsToPdf = this.isExportingCustomizedDocs.value;
    } else if (this._exportTypeId == ExportTypeId.ZIP) {
      serviceTypeSettings.isExportingCustomizedDocsToZip = this.isExportingCustomizedDocs.value;
    }
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

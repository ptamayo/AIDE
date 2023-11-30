import { Component, Input, OnInit } from '@angular/core';
import { ClaimType } from 'src/app/models/claim-type';
import { CompanyClaimTypeSettings } from 'src/app/models/company-claim-type-settings';
import { InsuranceProbatoryDocumentService } from 'src/app/services/insurance-probatory-document.service';
import { forkJoin } from 'rxjs';
import { IDictionary } from 'src/app/models/dictionary';
import { InsuranceExportProbatoryDocument } from 'src/app/models/insurance-export-probatory-document';

@Component({
  selector: 'app-company-export',
  templateUrl: './company-export.component.html',
  styleUrls: ['./company-export.component.css']
})
export class CompanyExportComponent implements OnInit {
  
  public _companyId: number;
  public _claimTypeSettings: { [claimId: number]: CompanyClaimTypeSettings };
  public _claimTypes: ClaimType[];

  @Input() 
  public set companyId(value: number) {
    this._companyId = value;
  }

  @Input()
  public set claimTypeSettings(value: { [claimId: number]: CompanyClaimTypeSettings }) {
    this._claimTypeSettings = value;
  }

  @Input()
  public set claimTypes(values: ClaimType[]) {
    this._claimTypes = values;
  }

  private allProbatoryDocuments: IDictionary<InsuranceExportProbatoryDocument[]> = { };
  private insuranceProbatoryDocuments: IDictionary<InsuranceExportProbatoryDocument[]> = { };

  constructor(private insuranceProbatoryDocumentService: InsuranceProbatoryDocumentService) { }

  ngOnInit(): void { }

  getCompanyClaimTypeSettings(claimTypeId: number): CompanyClaimTypeSettings {
    if (this._claimTypeSettings && this._claimTypeSettings[claimTypeId]) {
      return this._claimTypeSettings[claimTypeId];
    } else {
      return null;
    }
  }

  onAccordionItemOpened(claimType: ClaimType, exportTypeId: number) {
    let getInsuranceProbatoryDocuments = this.insuranceProbatoryDocumentService.getExportDocumentsByCompanyIdAndClaimTypeId(this._companyId, claimType.id);
    let getExportProbatoryDocuments = this.insuranceProbatoryDocumentService.getExportDocumentsByCompanyIdAndClaimTypeIdAndExportTypeId(this._companyId, claimType.id, exportTypeId);

    forkJoin([getInsuranceProbatoryDocuments, getExportProbatoryDocuments]).subscribe(results =>{
      this.allProbatoryDocuments[claimType.id] = <InsuranceExportProbatoryDocument[]>results[0];
      this.insuranceProbatoryDocuments[claimType.id] = <InsuranceExportProbatoryDocument[]>results[1];
    });
  }

  getAllProbatoryDocuments(claimTypeId: number) {
    if (this.allProbatoryDocuments && this.allProbatoryDocuments[claimTypeId]) {
      return this.allProbatoryDocuments[claimTypeId];
    } else {
      return <InsuranceExportProbatoryDocument[]>[];
    }
  }

  getInsuranceProbatoryDocuments(claimTypeId: number) {
    if (this.insuranceProbatoryDocuments && this.insuranceProbatoryDocuments[claimTypeId]) {
      return this.insuranceProbatoryDocuments[claimTypeId];
    } else {
      return <InsuranceExportProbatoryDocument[]>[];
    }
  }
}

import { Component, OnInit, Input } from '@angular/core';
import { DocumentService } from 'src/app/services/document.service';
import { ProbatoryDocument } from 'src/app/models/probatory-document';
import { ClaimType } from 'src/app/models/claim-type';
import { InsuranceProbatoryDocument } from 'src/app/models/insurance-probatory-document';
import { InsuranceProbatoryDocumentService } from 'src/app/services/insurance-probatory-document.service';
import { CompanyClaimTypeSettings } from 'src/app/models/company-claim-type-settings';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-company-services',
  templateUrl: './company-services.component.html',
  styleUrls: ['./company-services.component.css']
})
export class CompanyServicesComponent implements OnInit {
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

  _companyId: number;
  _claimTypeSettings: { [claimId: number]: CompanyClaimTypeSettings };
  _claimTypes: ClaimType[];
  insuranceProbatoryDocuments: InsuranceProbatoryDocument[] = [];
  allProbatoryDocuments: ProbatoryDocument[] = [];

  constructor(
    private insuranceProbatoryDocumentService: InsuranceProbatoryDocumentService,
    private probatoryDocumentsService: DocumentService) { }

  ngOnInit() {
    let getInsuranceProbatoryDocuments = this.insuranceProbatoryDocumentService.getByInsuranceCompanyId(this._companyId);
    let getProbatoryDocuments = this.probatoryDocumentsService.getAll();

    forkJoin([getInsuranceProbatoryDocuments, getProbatoryDocuments]).subscribe(results =>{
      this.insuranceProbatoryDocuments = <InsuranceProbatoryDocument[]>results[0];
      this.allProbatoryDocuments = <ProbatoryDocument[]>results[1];      
    });
  }

  getCompanyClaimTypeSettings(claimTypeId: number): CompanyClaimTypeSettings {
    if (this._claimTypeSettings && this._claimTypeSettings[claimTypeId]) {
      return this._claimTypeSettings[claimTypeId];
    } else {
      return null;
    }
  }
}

import { Injectable } from '@angular/core';
import { DataService } from '../shared/services/data.service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

@Injectable()
export class InsuranceProbatoryDocumentService extends DataService {
    constructor(http: HttpClient) { 
        super(environment.endpointInsuranceProbatoryDocumentService, http);
    }

    getByInsuranceCompanyId(id) {
        return this.get(`/insuranceCompany/${id}/documents`);
    }

    getByInsuranceCompanyIdAndClaimTypeId(companyId, claimTypeId) {
        return this.get(`/insuranceCompany/${companyId}/serviceType/${claimTypeId}/documents`);
    }

    getExportDocumentsByCompanyIdAndClaimTypeId(companyId, claimTypeId) {
        return this.get(`/insuranceCompany/${companyId}/serviceType/${claimTypeId}/export`);
    }

    getExportDocumentsByCompanyIdAndClaimTypeIdAndExportTypeId(companyId, claimTypeId, exportTypeId) {
        return this.get(`/insuranceCompany/${companyId}/serviceType/${claimTypeId}/export/${exportTypeId}`);
    }

    upsert(documents, companyId, claimTypeId) {
        return this.post(documents, `/insuranceCompany/${companyId}/serviceType/${claimTypeId}/documents`);
    }

    upsertExportDocuments(documents, companyId, claimTypeId, exportTypeId) {
        return this.post(documents, `/insuranceCompany/${companyId}/serviceType/${claimTypeId}/export/${exportTypeId}`);
    }
}

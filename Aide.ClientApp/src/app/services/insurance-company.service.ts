import { Injectable } from '@angular/core';
import { DataService } from '../shared/services/data.service';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { CompaniesFilter } from '../admin/companies/companies-filter';

@Injectable()
export class InsuranceCompanyService extends DataService {

  constructor(http: HttpClient) { 
    super(environment.endpointInsuranceCompanyService, http);
  }

  getAllEnabled(): Observable<Object> {
    return this.get('/list/enabled');
  }

  getAll(): Observable<Object> {
    return this.get();
  }

  getPage(pagingAndFilters: CompaniesFilter): Observable<Object> {
    return this.post(pagingAndFilters, '/list');
  }

  getById(id) {
    return this.get(`/${id}`);
  }

  insert(insuranceCompany) {
    return this.post(insuranceCompany);
  }

  update(insuranceCompany) {
    return this.put(insuranceCompany);
  }

  getUsersByCompanyId(companyId, pagingAndFilters: CompaniesFilter) {
    return this.post(pagingAndFilters, `/${companyId}/users`);
  }

  upsertClaimTypeSettings(settings, companyId, claimTypeId) {
    return this.post(settings, `/${companyId}/serviceType/${claimTypeId}/settings`);
  }
}

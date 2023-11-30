import { Injectable } from '@angular/core';
import { DataService } from '../shared/services/data.service';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { Dashboard1Filter } from '../dashboard/dashboard1/dashboard1.filter';

@Injectable()
export class ClaimService extends DataService {

  constructor(http: HttpClient) { 
    super(environment.endpointClaimService, http);
  }

  getPage(pagingAndFilters: Dashboard1Filter): Observable<Object> {
    return this.post(pagingAndFilters, '/list');
  }

  getById(id: number) {
    return this.get(`/${id}`);
  }

  insert(claim) {
    return this.post(claim);
  }

  update(claim) {
    return this.put(claim);
  }

  updateStatus(claimId: number, statusId: number) {
    return this.put(statusId, `/${claimId}/status`);
  }

  submitSignature(claimId: number, signature: any) {
    return this.post(signature, `/${claimId}/signature`);
  }

  getSignatureByClaimId(claimId: number) {
    return this.get(`/${claimId}/signature`);
  }

  isExternalOrderNumberTaken(claimId: number, externalOrderNumber: string) {
    return this.get(`/${claimId}/externalOrderNumber/${externalOrderNumber}/verify`);
  }
}

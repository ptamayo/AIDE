import { Injectable } from '@angular/core';
import { DataService } from '../shared/services/data.service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { Dashboard1Filter } from '../dashboard/dashboard1/dashboard1.filter';

@Injectable()
export class MessageBrokerService extends DataService {
  
  constructor(http: HttpClient) { 
    super(environment.endpointMessageBroker, http);
  }

  //// The method below is functional but is not being used by the client app
  // emailClaimReceipt(message: EmailClaimReceiptMessage) {
  //   return this.post(message, '/emailClaimReceipt');
  // }

  emailClaimReceipt(claimId: number) {
    return this.post(claimId, `/emailClaimReceipt`);
  }

  zipClaimFiles(claimId: number) {
    return this.post(claimId, '/zipClaimFiles');
  }

  pdfExportClaimFiles(claimId: number) {
    return this.post(claimId, '/pdfExportClaimFiles');
  }

  zipAndEmailClaimFiles(claimId: number, emailTo: string) {
    return this.post({ claimId: claimId, emailTo: emailTo }, '/zipAndEmailClaimFiles');
  }

  dashboard1ClaimsReport(filters: Dashboard1Filter) {
    return this.post(filters, '/dashboard1ClaimsReport');
  }
}

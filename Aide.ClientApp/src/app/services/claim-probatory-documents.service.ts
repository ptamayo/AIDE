import { Injectable } from '@angular/core';
import { MessageBrokerService } from './message-broker.service';

@Injectable()
export class ClaimProbatoryDocumentsService {

  constructor(private messageBrokerService: MessageBrokerService) { }

  zipClaimFiles(claimId: number) {
    return this.messageBrokerService.zipClaimFiles(claimId);
  }

  pdfExportClaimFiles(claimId: number) {
    return this.messageBrokerService.pdfExportClaimFiles(claimId);
  }

  zipAndEmailClaimFiles(claimId: number, emailTo: string) {
    return this.messageBrokerService.zipAndEmailClaimFiles(claimId, emailTo);
  }
}

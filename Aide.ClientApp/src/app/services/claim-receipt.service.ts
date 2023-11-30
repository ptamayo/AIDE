import { Injectable } from '@angular/core';
// import { Claim } from '../models/claim';
// import { EmailClaimReceiptMessage } from '../messages/email-claim-receipt-message';
import { MessageBrokerService } from './message-broker.service';

@Injectable()
export class ClaimReceiptService {

  constructor(private messageBrokerService: MessageBrokerService) { }

  //// The method below is functional but is not being used by the client app
  // emailClaimReceipt(claim: Claim) {
  //   const message: EmailClaimReceiptMessage = {
  //     claimId: claim.id,
  //     claimNumber: claim.claimNumber,
  //     externalOrderNumber: claim.externalOrderNumber,
  //     customerFullName: claim.customerFullName,
  //     insuranceCompanyName: claim.insuranceCompany.name,
  //     storeName: claim.store.name,
  //     storeEmail: claim.store.email,
  //     claimProbatoryDocuments: claim.claimProbatoryDocuments.filter(x => x).map((d) => d.probatoryDocument.name)
  //   };
  //   return this.messageBrokerService.emailClaimReceipt(message);
  // }

  emailClaimReceipt(claimId: number) {
    return this.messageBrokerService.emailClaimReceipt(claimId);
  }

}

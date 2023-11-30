import { MessageTest } from './messages/message-test';
import { MessageZipClaimFilesReady } from './messages/message-zip-claim-files-ready';
import { MessagePdfClaimFilesReady } from './messages/message-pdf-claim-files-ready';
import { MessageClaimReceiptReady } from './messages/message-claim-receipt-ready';
import { MessageDashboard1ClaimsReportReady } from './messages/message-dashboard1-claims-report-ready';
import { NotificationMessageBase } from './messages/notification-message-base';

export class Notification {
    id: number;
    type: number;
    source: string;
    target: string;
    messageType: string;
    message: string;
    isRead: boolean;
    dateCreated: string;

    public static jsonParse(n: Notification) {
        if (n.messageType == "Test") {
          const m = JSON.parse(n.message) as MessageTest;
          return {
              title: m.title,
              content: m.content,
              url: m.url,
              hasUrl: m.hasUrl,
              hasClaim: m.hasClaim,
              hasDepositSlip: m.hasDepositSlip
          };
        }
        else if (n.messageType == "ZipClaimFilesReady") {
            const m = JSON.parse(n.message) as MessageZipClaimFilesReady;
            return {
                title: m.title,
                content: m.content,
                claimId: m.claimId,
                documentId: m.documentId,
                url: m.url,
                hasUrl: m.hasUrl,
                hasClaim: m.hasClaim,
                hasDepositSlip: m.hasDepositSlip
            };
        }
        else if (n.messageType == "PdfClaimFilesReady") {
            const m = JSON.parse(n.message) as MessagePdfClaimFilesReady;
            return {
                title: m.title,
                content: m.content,
                claimId: m.claimId,
                documentId: m.documentId,
                hasUrl: m.hasUrl,
                hasClaim: m.hasClaim,
                hasDepositSlip: m.hasDepositSlip
            };
        }
        else if (n.messageType == "ClaimReceiptReady") {
            const m = JSON.parse(n.message) as MessageClaimReceiptReady;
            return {
                title: m.title,
                content: m.content,
                claimId: m.claimId,
                mediaId: m.mediaId,
                hasUrl: m.hasUrl,
                hasClaim: m.hasClaim,
                hasDepositSlip: m.hasDepositSlip
            };
        }
        else if (n.messageType == "Dashboard1ClaimsReportReady") {
            const m = JSON.parse(n.message) as MessageDashboard1ClaimsReportReady;
            return {
                title: m.title,
                content: m.content,
                url: m.url,
                hasUrl: m.hasUrl,
                hasClaim: m.hasClaim,
                hasDepositSlip: m.hasDepositSlip
            };
        }
        else {
            const m = JSON.parse(n.message) as NotificationMessageBase;
            return {
                title: m.title,
                content: m.content,
                url: m.url,
                hasUrl: m.hasUrl,
                hasClaim: m.hasClaim,
                hasDepositSlip: m.hasDepositSlip
            };
        }
    }
}

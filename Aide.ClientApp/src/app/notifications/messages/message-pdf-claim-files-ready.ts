import { NotificationMessageBase } from './notification-message-base';

export interface MessagePdfClaimFilesReady extends NotificationMessageBase {
    claimId: number;
    documentId: number;
}

import { NotificationMessageBase } from './notification-message-base';

export interface MessageZipClaimFilesReady extends NotificationMessageBase {
    claimId: number;
    documentId: number;
}

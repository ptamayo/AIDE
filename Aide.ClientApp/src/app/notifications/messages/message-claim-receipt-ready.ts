import { NotificationMessageBase } from './notification-message-base';

export interface MessageClaimReceiptReady extends NotificationMessageBase {
    claimId: number;
    mediaId: number;
}

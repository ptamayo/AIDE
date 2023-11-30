import { DocumentType } from './document-type';
import { Document } from './document';

export interface ClaimDocument {
    id: number;
    claimId: number;
    documentTypeId: number;
    documentId: number;
    statusId: number;
    sortPriority: number;
    groupId: number;
    dateCreated: string;
    dateModified: string;

    documentType?: DocumentType;
    document?: Document;
}

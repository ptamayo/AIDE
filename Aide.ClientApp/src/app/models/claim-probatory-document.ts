import { ProbatoryDocument } from './probatory-document';
import { Media } from './media';

export interface ClaimProbatoryDocument {
    id: number;
    claimId: number;
    claimItemId?: number;
    probatoryDocumentId: number;
    sortPriority: number;
    groupId: number;
    dateCreated: string;
    dateModified: string;

    probatoryDocument?: ProbatoryDocument;
    media?: Media;
}

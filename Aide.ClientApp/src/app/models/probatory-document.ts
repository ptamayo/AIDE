import { DocumentOrientationId } from "../enums/document-orientation-id";

export interface ProbatoryDocument {
    id: number;
    name: string;
    orientation?: DocumentOrientationId;
    acceptedFileExtensions: string;
    dateCreated?: string;
    dateModified?: string;
}

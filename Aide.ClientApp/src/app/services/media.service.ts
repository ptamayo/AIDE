import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';

@Injectable()
export class MediaService {

  constructor(private http: HttpClient) { }

  downloadMedia(mediaId: number) {
    return this.http.get(`${environment.endpointMediaFileUploadService}?h=${mediaId}`, {responseType: "blob"});
  }

  downloadDocument(documentId: number) {
    return this.http.get(`${environment.endpointDocumentFileUploadService}?h=${documentId}`, {responseType: "blob"});
  }
}

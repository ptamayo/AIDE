import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class FileUploadService {

  constructor(private http: HttpClient) { }

  addClaimProbatoryDocument(claimProbatoryDocumentId: number, image: File): Observable<any> {
    var formData: any = new FormData();
    formData.append("claimProbatoryDocumentId", claimProbatoryDocumentId);
    formData.append("image", image);

    return this.http.post(environment.endpointMediaFileUploadService, formData, {
      reportProgress: true,
      observe: 'events'
    }).pipe(
      catchError(this.errorMgmt)
    );
  }

  addClaimDocument(documentTypeId: number, claimId: number, overwrite: boolean, sortPriority: number, groupId: number, image: File): Observable<any> {
    var formData: any = new FormData();
    formData.append("documentTypeId", documentTypeId);
    formData.append("claimId", claimId);
    formData.append("overwrite", overwrite);
    formData.append("sortPriority", sortPriority);
    formData.append("groupId", groupId);
    formData.append("image", image);

    return this.http.post(environment.endpointDocumentFileUploadService, formData, {
      reportProgress: true,
      observe: 'events'
    }).pipe(
      catchError(this.errorMgmt)
    );
  }
  
  errorMgmt(error: HttpErrorResponse) {
    let errorMessage = '';
    if (error.error instanceof ErrorEvent) {
      // Get client-side error
      errorMessage = error.error.message;
    } else {
      // Get server-side error
      errorMessage = `Error Code: ${error.status}\nMessage: ${error.message}`;
    }
    return throwError(errorMessage);
  }

}

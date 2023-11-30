import { Injectable } from '@angular/core';
import { DataService } from '../shared/services/data.service';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { DocumentsFilter } from '../admin/documents/documents-filter';

@Injectable()
export class DocumentService extends DataService {
    constructor(http: HttpClient) { 
        super(environment.endpointProbatoryDocumentService, http);
    }

    getAll(): Observable<Object> {
        return this.get();
    }

    getPage(pagingAndFilters: DocumentsFilter): Observable<Object> {
        return this.post(pagingAndFilters, '/list');
    }

    getById(id) {
        return this.get(`/${id}`);
    }

    insert(doc) {
        return this.post(doc);
    }

    update(doc) {
        return this.put(doc);
    }
}

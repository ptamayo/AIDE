import { Injectable } from '@angular/core';
import { DataService } from '../shared/services/data.service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { Observable } from 'rxjs';
import { StoresFilter } from '../admin/stores/stores-filter';

@Injectable()
export class StoreService extends DataService {

  constructor(http: HttpClient) { 
    super(environment.endpointStoreService, http);
  }

  getAll(): Observable<Object> {
    return this.get();
  }

  getPage(pagingAndFilters: StoresFilter): Observable<Object> {
    return this.post(pagingAndFilters, '/list');
  }

  getById(id) {
    return this.get(`/${id}`);
  }

  insert(store) {
    return this.post(store);
  }

  update(store) {
    return this.put(store);
  }

  getUsersByCompanyId(companyId, pagingAndFilters: StoresFilter) {
    return this.post(pagingAndFilters, `/${companyId}/users`);
  }
}
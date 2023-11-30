import { Injectable } from '@angular/core';
import { DataService } from '../shared/services/data.service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { Observable } from 'rxjs';
import { CompanyCollagesFilter } from '../admin/company-collages/company-collages-filter';

@Injectable()
export class InsuranceCollageService extends DataService {

    constructor(http: HttpClient) { 
      super(environment.endpointInsuranceCollageService, http);
    }
   
    getPage(pagingAndFilters: CompanyCollagesFilter): Observable<Object> {
      return this.post(pagingAndFilters, '/list');
    }

    getById(id) {
      return this.get(`/${id}`);
    }

    insert(collage) {
      return this.post(collage);
    }
  
    update(collage) {
      return this.put(collage);
    }

    delete(id) {
      return this.del(`/${id}/delete`);
    }
  }

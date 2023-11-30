import { Injectable } from '@angular/core';
import { DataService } from '../shared/services/data.service';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

@Injectable()
export class ClaimTypeService extends DataService {

  constructor(http: HttpClient) { 
    super(environment.endpointClaimTypeService, http);
  }

  getAll(): Observable<Object> {
    return this.get();
  }
}

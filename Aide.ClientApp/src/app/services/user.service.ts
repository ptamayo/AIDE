import { Injectable } from '@angular/core';
import { DataService } from '../shared/services/data.service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { Observable } from 'rxjs';
import { UsersFilter } from '../admin/users/users-filter';

@Injectable()
export class UserService extends DataService {

    constructor(http: HttpClient) { 
      super(environment.endpointUserService, http);
    }
   
    getPage(pagingAndFilters: UsersFilter): Observable<Object> {
      return this.post(pagingAndFilters, '/list');
    }

    getById(id) {
      return this.get(`/${id}`);
    }
  
    getByEmail(emailUTFEncoded: string) {
      return this.get(`?email=${emailUTFEncoded}`);
    }

    updateProfile(userProfile) {
      return this.put(userProfile, '/profile');
    }

    insert(user) {
      return this.post(user);
    }
  
    update(user) {
      return this.put(user);
    }

    resetPsw(userId) {
      const payload = { userId: userId };
      return this.post(payload, '/reset');
    }
  }

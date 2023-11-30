import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { JwtHelperService } from '@auth0/angular-jwt';
import { environment } from 'src/environments/environment';
import { DataService } from '../shared/services/data.service';
import { User } from '../models/user';
import { UserCompany } from '../models/user-company';
import { UserRoleId } from '../enums/user-role-id.enum';
import { sha256 } from 'js-sha256';
import { CompanyTypeId } from '../enums/company-type-id.enum';

@Injectable()
export class AuthService extends DataService {
  constructor(http: HttpClient) { 
    super(environment.endpointAuthService, http);
  }

  login(credentials) {
    credentials.password = sha256(credentials.password);
    return this.post(credentials, '/authenticate');
   }
 
   logout(request) {
     localStorage.removeItem('token');
     return this.post(request, '/logout');
   }
 
   isLoggedIn() {
     const token = localStorage.getItem('token');
     if (!token) return false;
 
     const jwtHelper = new JwtHelperService();
     const isExpired = jwtHelper.isTokenExpired(token);
     return !isExpired;
   }
 
   get currentUser(): User {
     const token = localStorage.getItem('token');
     if (!token) return null;
 
     const result = new JwtHelperService().decodeToken(token);
    //  const resultx = result.companies ? result.companies.split(',').map(Number) : [];
     //// Dev Notes: Need revisit. This same logic exists in the API. Need re-think the approach to avoid have this logic repeated in the front-end
     //// For further details see UserService class in the back-end.
     let companyType = CompanyTypeId.Unknown;
     switch(+result.role as UserRoleId) {
       case UserRoleId.InsuranceReadOnly:
         companyType = CompanyTypeId.Insurance;
         break;
       case UserRoleId.WsAdmin:
       case UserRoleId.WsOperator:
         companyType = CompanyTypeId.Store;
         break;
     }
     const user: User = {
       id: +result.nameid,
       roleId: +result.role as UserRoleId,
       firstName: result.given_name,
       lastName: result.family_name,
       email: result.email,
       companies: result.companies ? result.companies.split(',').map(companyId => {
         return <UserCompany> {
           id: 0,
           companyId: +companyId,
           companyTypeId: companyType
         }
       }) : [],
       dateCreated: result.dateCreated,
       dateLogout: result.dateLogout
     };
     return user;
   }

   get token(): string {
    const token = localStorage.getItem('token');
    if (token)
    {
      return token;
    }
    else {
      return null;
    }
   }
}

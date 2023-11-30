import { Component, OnInit } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { UserRoleId } from 'src/app/enums/user-role-id.enum';
import { CompanyTypeId } from 'src/app/enums/company-type-id.enum';

@Component({
  selector: 'app-users',
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.css']
})
export class UsersComponent implements OnInit {
  private companyUsersInputArgs = new BehaviorSubject({
    companyTypeId: CompanyTypeId.Unknown,
    companyId: 0,
    userRoles: [UserRoleId.Admin, UserRoleId.InsuranceReadOnly, UserRoleId.WsAdmin, UserRoleId.WsOperator]
  });
  public eventStream$ = this.companyUsersInputArgs.asObservable();
  
  constructor() { }

  ngOnInit() {
    this.populateCompanyUsers(0);
  }

  // Load company users in child component
  populateCompanyUsers(companyId) {
    let inputArgs = this.companyUsersInputArgs.value;
    inputArgs.companyId = companyId;
    this.companyUsersInputArgs.next(inputArgs);
  }
}

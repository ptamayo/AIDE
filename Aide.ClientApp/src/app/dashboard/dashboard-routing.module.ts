import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AuthGuard } from '../services/auth-guard.service';
import { UserRoleId } from '../enums/user-role-id.enum';
import { Dashboard1Component } from './dashboard1/dashboard1.component';

const routes: Routes = [
  {
    path: '',
    children: [
      {
        path: '',
        redirectTo: 'dashboard1',
        pathMatch: 'full',
      },
      {
        path: 'dashboard1',
        component: Dashboard1Component,
        canActivate: [AuthGuard],
        data: { roles: [UserRoleId.Admin, UserRoleId.InsuranceReadOnly, UserRoleId.WsAdmin, UserRoleId.WsOperator] }
      }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class DashboardRoutingModule { }

import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AuthGuard } from '../services/auth-guard.service';
import { UserRoleId } from '../enums/user-role-id.enum';
import { ClaimComponent } from './claim.component';
import { SignatureComponent } from './signature/signature.component';

const routes: Routes = [
  {
    path: '',
    children: [
      {
        path: '',
        redirectTo: 'new',
        pathMatch: 'full'
      },
      {
        path: 'new',
        component: ClaimComponent,
        canActivate: [AuthGuard],
        data: { roles: [UserRoleId.Admin, UserRoleId.WsAdmin, UserRoleId.WsOperator] }
      },
      {
        path: ':id',
        component: ClaimComponent,
        canActivate: [AuthGuard],
        data: { roles: [UserRoleId.Admin, UserRoleId.InsuranceReadOnly, UserRoleId.WsAdmin, UserRoleId.WsOperator] }
      },
      {
        path: ':id/signature',
        component: SignatureComponent,
        canActivate: [AuthGuard],
        data: { roles: [UserRoleId.Admin, UserRoleId.InsuranceReadOnly, UserRoleId.WsAdmin, UserRoleId.WsOperator] }
      }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ClaimRoutingModule { }

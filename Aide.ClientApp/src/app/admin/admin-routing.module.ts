import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AuthGuard } from '../services/auth-guard.service';
import { UserRoleId } from '../enums/user-role-id.enum';
import { CompaniesComponent } from './companies/companies.component';
import { DocumentsComponent } from './documents/documents.component';
import { StoresComponent } from './stores/stores.component';
import { StoreComponent } from './store/store.component';
import { CompanyComponent } from './company/company.component';
import { DocumentComponent } from './document/document.component';
import { UsersComponent } from './users/users.component';

const routes: Routes = [
  {
    path: '',
    children: [
      {
        path: '',
        redirectTo: 'stores',
        pathMatch: 'full',
      },
      {
        path: 'stores',
        component: StoresComponent,
        canActivate: [AuthGuard],
        data: { roles: [UserRoleId.Admin] }
      },
      {
        path: 'stores/new',
        component: StoreComponent,
        canActivate: [AuthGuard],
        data: { roles: [UserRoleId.Admin] }
      },
      {
        path: 'store/:id',
        component: StoreComponent,
        canActivate: [AuthGuard],
        data: { roles: [UserRoleId.Admin] }
      },
      {
        path: 'companies',
        component: CompaniesComponent,
        canActivate: [AuthGuard],
        data: { roles: [UserRoleId.Admin] }
      },
      {
        path: 'companies/new',
        component: CompanyComponent,
        canActivate: [AuthGuard],
        data: { roles: [UserRoleId.Admin] }
      },
      {
        path: 'company/:id',
        component: CompanyComponent,
        canActivate: [AuthGuard],
        data: { roles: [UserRoleId.Admin] }
      },
      {
        path: 'documents',
        component: DocumentsComponent,
        canActivate: [AuthGuard],
        data: { roles: [UserRoleId.Admin] }
      },
      {
        path: 'documents/new',
        component: DocumentComponent,
        canActivate: [AuthGuard],
        data: { roles: [UserRoleId.Admin] }
      },
      {
        path: 'document/:id',
        component: DocumentComponent,
        canActivate: [AuthGuard],
        data: { roles: [UserRoleId.Admin] }
      },
      {
        path: 'users',
        component: UsersComponent,
        canActivate: [AuthGuard],
        data: { roles: [UserRoleId.Admin] }
      }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class AdminRoutingModule { }

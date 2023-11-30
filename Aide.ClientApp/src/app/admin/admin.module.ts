import { NgModule } from '@angular/core';
import { CommonModule } from "@angular/common";

import { AdminRoutingModule } from "./admin-routing.module";
import { MatButtonModule } from '@angular/material/button';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSortModule } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { MatTabsModule } from '@angular/material/tabs';
import { MatToolbarModule } from '@angular/material/toolbar';

import { CompaniesComponent } from './companies/companies.component';
import { DocumentsComponent } from './documents/documents.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { CustomPipesModule } from '../shared/custom-pipes/custom-pipes.module';
import { StoresComponent } from './stores/stores.component';
import { StoreComponent } from './store/store.component';
import { CompanyUsersComponent } from './company-users/company-users.component';
import { CompanyComponent } from './company/company.component';
import { DocumentComponent } from './document/document.component';
import { UsersComponent } from './users/users.component';
import { CompanyServicesComponent } from './company-services/company-services.component';
import { CompanyServiceDocsComponent } from './company-service-docs/company-service-docs.component';
import { DragDropModule } from '@angular/cdk/drag-drop';
import { CompanyCollagesComponent } from './company-collages/company-collages.component';
import { CustomDirectivesModule } from '../shared/directives/custom-directives.module';
import { CompanyExportDocsComponent } from './company-export-docs/company-export-docs.component';
import { CompanyExportComponent } from './company-export/company-export.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        MatFormFieldModule,
        AdminRoutingModule,
        MatToolbarModule,
        MatProgressSpinnerModule,
        MatSnackBarModule,
        MatTableModule,
        MatSortModule,
        MatPaginatorModule,
        MatInputModule,
        MatIconModule,
        MatButtonModule,
        MatTabsModule,
        MatExpansionModule,
        MatSelectModule,
        MatSlideToggleModule,
        CustomPipesModule,
        DragDropModule,
        CustomDirectivesModule
    ],
    exports: [],
    declarations: [
        StoresComponent,
        CompaniesComponent,
        DocumentsComponent,
        StoreComponent,
        CompanyUsersComponent,
        CompanyComponent,
        DocumentComponent,
        UsersComponent,
        CompanyServicesComponent,
        CompanyServiceDocsComponent,
        CompanyCollagesComponent,
        CompanyExportDocsComponent,
        CompanyExportComponent,
    ],
    providers: [],
})
export class AdminModule { }

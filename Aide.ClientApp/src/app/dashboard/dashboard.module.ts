import { NgModule } from '@angular/core';
import { CommonModule } from "@angular/common";

import { DashboardRoutingModule } from "./dashboard-routing.module";

import { Dashboard1Component } from "./dashboard1/dashboard1.component";

import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSortModule } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { MatToolbarModule } from '@angular/material/toolbar';
import { CustomPipesModule } from '../shared/custom-pipes/custom-pipes.module';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatMomentDateModule } from '@angular/material-moment-adapter';
import { CustomDirectivesModule } from '../shared/directives/custom-directives.module';

@NgModule({
    imports: [
        CommonModule,
        CustomPipesModule,
        DashboardRoutingModule,
        MatProgressSpinnerModule,
        MatTableModule,
        MatPaginatorModule,
        MatSortModule,
        MatToolbarModule,
        MatButtonModule,
        MatCardModule,
        FormsModule,
        ReactiveFormsModule,
        MatInputModule,
        MatIconModule,
        MatSelectModule,
        MatAutocompleteModule,
        MatDatepickerModule,
        MatMomentDateModule,
        CustomDirectivesModule
    ],
    exports: [],
    declarations: [
        Dashboard1Component
    ],
    providers: [],
})
export class DashboardModule { }

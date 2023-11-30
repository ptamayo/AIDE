import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { ClaimRoutingModule } from './claim-routing.module';
import { ClaimComponent } from './claim.component';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatCardModule } from '@angular/material/card';

import { NgxGalleryModule } from 'ngx-gallery-9';
import { SignaturePadModule } from '@ng-plus/signature-pad';
import { SignatureComponent } from './signature/signature.component';
import { ExportDocumentsComponent } from './export-documents/export-documents.component';
import { ClaimProbatoryDocumentsComponent } from './claim-probatory-documents/claim-probatory-documents.component';
import { ClaimDocumentsComponent } from './claim-documents/claim-documents.component';
import { CustomDirectivesModule } from '../shared/directives/custom-directives.module';
import { GalleryComponent } from './gallery/gallery.component';
import { CustomPipesModule } from '../shared/custom-pipes/custom-pipes.module';
import { SharedModule } from '../shared/shared.module';

@NgModule({
  declarations: [
    ClaimComponent, 
    SignatureComponent, 
    ExportDocumentsComponent, 
    ClaimProbatoryDocumentsComponent, 
    ClaimDocumentsComponent, 
    GalleryComponent
  ],
  imports: [
    CommonModule,
    SharedModule,
    CustomPipesModule,
    FormsModule,
    ReactiveFormsModule,
    ClaimRoutingModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatSnackBarModule,
    MatTableModule,
    MatToolbarModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    NgxGalleryModule,
    SignaturePadModule,
    CustomDirectivesModule
  ]
})
export class ClaimModule { }

import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { AppRoutingModule } from './app-routing.module';
import { HttpClient, HttpClientModule } from '@angular/common/http';
import { AppComponent } from './app.component';
import { FullLayoutComponent } from './layouts/full-layout/full-layout.component';
import { HeaderComponent } from './shared/components/header/header.component';
import { LeftPanelComponent } from './shared/components/left-panel/left-panel.component';
import { RightPanelComponent } from './shared/components/right-panel/right-panel.component';
import { FooterComponent } from './shared/components/footer/footer.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { ClaimService } from './services/claim.service';
import { InsuranceCompanyService } from './services/insurance-company.service';
import { ClaimTypeService } from './services/claim-type.service';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatStepperModule } from '@angular/material/stepper';
import { MatTableModule } from '@angular/material/table';
import { UploadDocumentDialogComponent } from './shared/dialogs/upload-document-dialog/upload-document-dialog.component';
import { UploadDocumentDialog2Component } from './shared/dialogs/upload-document-dialog2/upload-document-dialog2.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ResizeService } from './shared/services/resize.service';
import { SizeDetectorComponent } from './shared/components/size-detector/size-detector.component';
import { LoginComponent } from './login/login.component';
import { NoAccessComponent } from './no-access/no-access.component';
import { AuthService } from './services/auth.service';
import { AuthGuard } from './services/auth-guard.service';
import { StoreService } from './services/store.service';
import { MessageBrokerService } from './services/message-broker.service';
import { ClaimReceiptService } from './services/claim-receipt.service';
import { ClaimProbatoryDocumentsService } from './services/claim-probatory-documents.service';
import { MatBadgeModule } from '@angular/material/badge';
import { NotificationService } from './services/notification.service';
import { UserService } from './services/user.service';
import { UserDialogComponent } from './shared/dialogs/user-dialog/user-dialog.component';
import { MessageDialogComponent } from './shared/dialogs/message-dialog/message-dialog.component';
import { DocumentService } from './services/document.service';
import { InsuranceProbatoryDocumentService } from './services/insurance-probatory-document.service';
import { InsuranceCollageService } from './services/insurance-collage-service';
import { CollageDialogComponent } from './shared/dialogs/collage-dialog/collage-dialog.component';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { ConfirmDialogComponent } from './shared/dialogs/confirm-dialog/confirm-dialog.component';
import { interceptorProviders } from './shared/interceptors/interceptorProviders';
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';
import { MediaService } from './services/media.service';

@NgModule({
  declarations: [
    AppComponent,
    FullLayoutComponent,
    HeaderComponent,
    LeftPanelComponent,
    RightPanelComponent,
    FooterComponent,
    UploadDocumentDialogComponent,
    UploadDocumentDialog2Component,
    UserDialogComponent,
    MessageDialogComponent,
    SizeDetectorComponent,
    LoginComponent,
    NoAccessComponent,
    CollageDialogComponent,
    ConfirmDialogComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    HttpClientModule,
    BrowserAnimationsModule,
    MatSidenavModule,
    MatToolbarModule,
    MatIconModule,
    MatDialogModule,
    MatProgressSpinnerModule,
    FormsModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatMenuModule,
    MatListModule,
    MatBadgeModule,
    MatSelectModule,
    MatStepperModule,
    MatTableModule,
    MatCheckboxModule,
    ScrollingModule,
    MatSnackBarModule,
    TranslateModule.forRoot({
      loader: {
        provide: TranslateLoader,
        useFactory: HttpLoaderFactory,
        deps: [HttpClient]
      }
    })
  ],
  providers: [
    interceptorProviders,
    ResizeService,
    UserService,
    ClaimService,
    ClaimReceiptService,
    ClaimProbatoryDocumentsService,
    StoreService,
    InsuranceCompanyService,
    ClaimTypeService,
    MessageBrokerService,
    AuthService,
    AuthGuard,
    NotificationService,
    DocumentService,
    InsuranceProbatoryDocumentService,
    InsuranceCollageService,
    MediaService
  ],
  bootstrap: [AppComponent],
  entryComponents: [
    UploadDocumentDialogComponent,
    UploadDocumentDialog2Component,
    UserDialogComponent,
    CollageDialogComponent,
    ConfirmDialogComponent,
    MessageDialogComponent
  ]
})
export class AppModule { }

export function HttpLoaderFactory(http: HttpClient) {
  return new TranslateHttpLoader(http, './assets/i18n/', '.json');
}
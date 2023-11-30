import { Component, OnInit, Inject, ViewChild } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogConfig, MatDialog } from '@angular/material/dialog';
import { MatTable } from '@angular/material/table';
import { User } from 'src/app/models/user';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { UserRoleId } from 'src/app/enums/user-role-id.enum';
import { environment } from 'src/environments/environment';
import { UserService } from 'src/app/services/user.service';
import { StoreService } from 'src/app/services/store.service';
import { InsuranceCompanyService } from 'src/app/services/insurance-company.service';
import { AppError } from '../../common/app-error';
import { Company } from 'src/app/models/company';
import { Store } from 'src/app/models/store';
import { InsuranceCompany } from 'src/app/models/insurance-company';
import { MessageDialogComponent } from '../message-dialog/message-dialog.component';
import { UserCompany } from 'src/app/models/user-company';
import { BadInput } from '../../common/bad-input';
import { ConfirmDialogComponent } from '../confirm-dialog/confirm-dialog.component';
import { of as observableOf } from 'rxjs';
import { switchMap } from 'rxjs/operators';

@Component({
  selector: 'app-user-dialog',
  templateUrl: './user-dialog.component.html',
  styleUrls: ['./user-dialog.component.css']
})
export class UserDialogComponent implements OnInit {
  isLoadingPage: boolean;
  saveBtnIsDisabled: boolean = false;

  action: string = "Add";
  companyTypeId: number;
  companyId: number;
  userId: number;
  userRoles: UserRoleId[] = [];
  isEmailVerified: boolean = false;
  companiesDataset: Company[] = [];
  selectedCompanies: Company[] = [];
  displayedColumns: string[] = ['name', 'actions'];
  user: User;

  firstFormGroup: FormGroup;
  userEmail = new FormControl('', [Validators.required, Validators.email, Validators.maxLength(100)]);
  userFirstName = new FormControl('', [Validators.required, Validators.maxLength(50)]);
  userLastName = new FormControl('', [Validators.required, Validators.maxLength(50)]);
  userRole = new FormControl('', [Validators.required]);
  
  secondFormGroup: FormGroup;
  userCompany = new FormControl('');

  constructor(
    private dataService: UserService,
    private storeService: StoreService,
    private insuranceCompanyService: InsuranceCompanyService,
    private dialog: MatDialog,
    private dialogRef: MatDialogRef<UserDialogComponent>,
    @Inject(MAT_DIALOG_DATA) data) { 
      this.companyTypeId = data.companyTypeId;
      this.companyId = data.companyId;
      this.userId = data.userId;
      this.userRoles = data.userRoles;
    }

  @ViewChild(MatTable) companiesTable: MatTable<any>;

  ngOnInit() {
    this.firstFormGroup = new FormGroup({
      userEmail: this.userEmail,
      userFirstName: this.userFirstName,
      userLastName: this.userLastName,
      userRole: this.userRole
    });

    this.secondFormGroup = new FormGroup({
      userCompany: this.userCompany
    });

    if (this.userId) {
      this.populate();
    }
    else {
      this.initializeFormControls(false);
    }
  }

  loadStores() {
    return this.storeService.getAll();
  }

  loadInsuranceCompanies() {
    return this.insuranceCompanyService.getAll();
  }

  get companies() {
    if (this.userRole.value == UserRoleId.Admin) return [];
    return this.companiesDataset;
  }

  populate() {
    this.dataService.getById(this.userId).subscribe((data: User) => {
      if (data) {
        this.user = data;
        this.populateFields();
      }
    });
  }

  populateFields() {
    if (this.user) {
      this.action = "Edit";
      this.initializeFormControls(true);
      this.userEmail.setValue(this.user.email);
      this.userFirstName.setValue(this.user.firstName);
      this.userLastName.setValue(this.user.lastName);
      this.userRole.setValue(this.user.roleId);
      this.userId = this.user.id;
      this.isEmailVerified = true;
    } else {
      this.action = "Add";
      this.initializeFormControls(false);
      this.isEmailVerified = false;
    }
    this.initializeSelectedCompanies();
  }

  initializeSelectedCompanies() {
    // If the user role selected in step 1 is Store Admin then the button to delete the store must be visible
    if (this.userRole.value == UserRoleId.WsAdmin) {
      this.displayedColumns = ['name', 'actions'];
    }
    else {
      this.displayedColumns = ['name'];
    }
    // Initialize the company(ies) on the table in step 2
    if (this.selectedCompanies.length > 0) {
      this.selectedCompanies = [];
    }
    if (this.userRole.value == UserRoleId.Admin) {
      this.companyId = 0;
      return;
    }
    if (this.userRole.value == UserRoleId.WsAdmin || this.userRole.value == UserRoleId.WsOperator) {
      this.loadStores().subscribe((data: Store[]) => {
        if (data) {
          this.companiesDataset = data.map(d => {
            return <Company> {
              id: d.id,
              name: `${d.sapNumber} - ${d.name}`,
            };
          });
          // Populate table with the store(s) that are related to the current user
          if (this.user && this.user.roleId == this.userRole.value && this.user.companies) {
            this.user.companies.map(company => company.companyId).forEach(companyId => this.addSelectedCompany(companyId));
          }
        }
      });
    }
    else if (this.userRole.value == UserRoleId.InsuranceReadOnly) {
      this.loadInsuranceCompanies().subscribe((data: InsuranceCompany[]) => {
        if (data) {
          this.companiesDataset = data.map(d => {
            return <Company> {
              id: d.id,
              name: d.name,
            };
          });
          // Populate table with the insurance company(ies) that are related to the current user
          if (this.user && this.user.roleId == this.userRole.value && this.user.companies) {
            this.companyId = this.user.companies[0].companyId; // NOTE: WsOperator and InsuranceReadOnly are associated to 1 single company
            this.user.companies.map(company => company.companyId).forEach(companyId => this.addSelectedCompany(companyId));
          }
        }
      });
    }
  }

  initializeFormControls(enableControls: boolean) {
    this.userFirstName.reset();
    this.userLastName.reset();
    this.userRole.reset();

    this.userFirstName.markAsUntouched();
    this.userLastName.markAsUntouched();
    this.userRole.markAsUntouched();
  }

  getNameOfUserRole(roleId) {
    return environment.UserRole[roleId];
  }

  verifyEmail() {
    const dialogTitle = 'Email validation result';
    this.saveBtnIsDisabled = true;

    // It is very important enconde the email to UTF-8 (this is needed for special chars)
    const emailUTFEncoded = encodeURIComponent(this.userEmail.value);
    this.dataService.getByEmail(emailUTFEncoded).subscribe((data: User) => {
      if (data) {
        // Verify if the email has been used in another account
        if (data.id != this.userId) {
        // If YES,
        // Ask confirmation if want to load the user's info
          const confirmDialog = this.openConfirmDialog(dialogTitle, 'Confirm: The email is being used by another user. Do you want to load the user?');
          confirmDialog.afterClosed().subscribe((answer: boolean) => {
            if (answer) {
              this.user = data;
              this.populateFields();
            } else {
              this.populateFields();
            }
          });
        }
        this.saveBtnIsDisabled = false;
      } else {
        // If NO,
        // Enable all fields and let create the new user
        const dialogRef = this.openInfoDialog(dialogTitle, 'Info: Congratulations, the email is available! Now you can proceed.', 'info');
        dialogRef.afterClosed().subscribe(() => {
          if (!this.userId) {
            this.initializeFormControls(true);
            this.isEmailVerified = true;
            this.action = "Add";
          }
          this.saveBtnIsDisabled = false;
        });
      }
    },
    (error: AppError) => {
        throw error;
    });
  }

  addSelectedCompany(companyId) {
    if (this.selectedCompanies.findIndex(x => x.id == companyId) == -1) {
      const company = this.companies.find(x => x.id == companyId);
      if (company) {
        if (this.selectedCompanies.length >= 1 && (this.userRole.value == UserRoleId.WsOperator || this.userRole.value == UserRoleId.InsuranceReadOnly)) {
          this.selectedCompanies = [];
        }
        this.selectedCompanies.push(company);
      }
      this.companiesTable.renderRows();
      this.userCompany.reset();
    }
  }
  
  removeSelectedCompany(companyId) {
    const index = this.selectedCompanies.findIndex(x => x.id == companyId);
    this.selectedCompanies.splice(index, 1);
    this.companiesTable.renderRows();
  }

  onUserRoleChanged(selectedOption) {
    this.initializeSelectedCompanies();
  }

  onSubmit() {
    if (!this.firstFormGroup.valid) return;
    
    this.saveBtnIsDisabled = true;
    const title = `${this.action} User`

    if (this.userRole.value != UserRoleId.Admin && this.selectedCompanies.length == 0) {
      const dialogRef = this.openInfoDialog(title, 'Error: Cannot save because you missed adding the user to a company', 'error')
      dialogRef.afterClosed().subscribe(() => {
        this.saveBtnIsDisabled = false;
      });
    }
    else {
      this.user = this.toModel();
      if (!this.userId) {
        this.dataService.insert(this.user)
          .subscribe((response: User) => {
            this.userId = response.id;
            this.saveBtnIsDisabled = false;
            const dialogRef = this.openInfoDialog(title, 'Info: The operation completed successfully!', 'info')
            dialogRef.afterClosed().subscribe(() => {
              this.close(this.user);
            });
          },
          (error: AppError) => {
            if (error instanceof BadInput) {
              this.saveBtnIsDisabled = false;
              const dialogRef = this.openInfoDialog(title, 'Error: The operation failed', 'error')
              dialogRef.afterClosed().subscribe();
            }
            else throw error;
          });
      } else {
        this.dataService.update(this.user)
          .subscribe((response: User) => {
            this.saveBtnIsDisabled = false;
            const dialogRef = this.openInfoDialog(title, 'Info: The operation completed successfully!', 'info')
            dialogRef.afterClosed().subscribe(() => {
              this.close(this.user);
            });
          },
          (error: AppError) => {
            if (error instanceof BadInput) {
              this.saveBtnIsDisabled = false;
              const dialogRef = this.openInfoDialog(title, `Error: ${error.originalError.error}`, 'error')
              dialogRef.afterClosed().subscribe();
            }
            else throw error;
          });
      }
    }
  }

  toModel(): User {
    if (this.userRole.value == UserRoleId.Admin) {
      this.companyTypeId = 0;
    }
    else if (this.userRole.value == UserRoleId.WsAdmin || this.userRole.value == UserRoleId.WsOperator) {
      this.companyTypeId = 1;
    }
    else if (this.userRole.value == UserRoleId.InsuranceReadOnly) {
      this.companyTypeId = 2;
    }

    const userCompanies = this.selectedCompanies.map(company => {
      return <UserCompany> {
        companyId: company.id,
        companyTypeId: this.companyTypeId
      };
    });

    const user: User = {
      id: this.userId ? this.userId : 0,
      roleId: this.userRole.value,
      firstName: this.userFirstName.value,
      lastName: this.userLastName.value,
      email: this.userEmail.value,
      companies: userCompanies
    };

    return user;
  }

  onClickButtonResetPsw() {
    const confirmDialog = this.openConfirmDialog('Confirm Reset Password', 'Confirm: Are you sure you want to reset the password for this user?');
    confirmDialog.afterClosed().pipe(
      switchMap(answer => {
        if (answer) {
          this.dataService.resetPsw(this.userId).subscribe(() => {
            // this.close(this.user);
          });
        }
        return observableOf(answer);
      })
    )
    .subscribe(answer => {
        if (answer) {
          const dialogRef = this.openInfoDialog('Reset password', 'Info: The operation completed successfully!', 'info');
          dialogRef.afterClosed().subscribe(() => {
            this.close(this.user);
          });
        }
    });
  }

  openInfoDialog(title: string, message: string, icon: string) {
    const dialogConfig = new MatDialogConfig();
    dialogConfig.data = { title: title, message: message, icon: icon };
    return this.dialog.open(MessageDialogComponent, dialogConfig);
  }

  openConfirmDialog(title: string, message: string) {
    const dialogConfig = new MatDialogConfig();
    dialogConfig.disableClose = true;
    dialogConfig.autoFocus = true;
    dialogConfig.data = { title: title, message: message };
    return this.dialog.open(ConfirmDialogComponent, dialogConfig);
  }

  close(user: User) {
    if  (user) {
      this.dialogRef.close(user)
    } else {
      this.dialogRef.close();
    }
  }
}

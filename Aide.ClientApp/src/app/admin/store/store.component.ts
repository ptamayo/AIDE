import { Component, OnInit } from '@angular/core';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { StoreService } from 'src/app/services/store.service';
import { Store } from '../../models/store';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AppError } from 'src/app/shared/common/app-error';
import { BadInput } from 'src/app/shared/common/bad-input';
import { BehaviorSubject } from 'rxjs';
import { UserRoleId } from 'src/app/enums/user-role-id.enum';
import { CompanyTypeId } from 'src/app/enums/company-type-id.enum';

@Component({
  selector: 'app-store',
  templateUrl: './store.component.html',
  styleUrls: ['./store.component.css']
})
export class StoreComponent implements OnInit {
  private companyUsersInputArgs = new BehaviorSubject({
    companyTypeId: CompanyTypeId.Store,
    companyId: 0,
    userRoles: [UserRoleId.WsAdmin, UserRoleId.WsOperator]
  });
  public eventStream$ = this.companyUsersInputArgs.asObservable();
  
  action: string = "Add";
  isLoadingPage: boolean;
  saveBtnIsDisabled: boolean = false;
  readonlyFormControl: boolean = false;

  storeId: number;
  store: Store | null;

  myForm: FormGroup;
  storeSAPNumber = new FormControl('', [Validators.required, Validators.maxLength(15)]);
  storeName = new FormControl('', [Validators.required, Validators.maxLength(250)]);
  storeEmail = new FormControl('', [Validators.required, Validators.email, Validators.maxLength(100)]);

  constructor(
    private route: ActivatedRoute, 
    private snackBar: MatSnackBar,
    private dataService: StoreService) { }

  ngOnInit() {
    this.myForm = new FormGroup({
      storeSAPNumber: this.storeSAPNumber,
      storeName: this.storeName,
      storeEmail: this.storeEmail
    });

    this.storeId = +this.route.snapshot.paramMap.get('id');
    if (this.storeId) {
      this.action = "Edit";
    }
    this.populate();
  }

  populate() {
    if (this.storeId) {
      this.isLoadingPage = true;
      this.dataService.getById(this.storeId).subscribe((data: Store) => {
        if (data) {
          this.store = data;
          this.storeId = this.store.id;
          this.storeSAPNumber.setValue(this.store.sapNumber);
          this.storeName.setValue(this.store.name);
          this.storeEmail.setValue(this.store.email);
          this.populateCompanyUsers(this.storeId);
        }
        this.isLoadingPage = false;
      });
    }
  }

  // Load company users in child component
  populateCompanyUsers(companyId) {
    let inputArgs = this.companyUsersInputArgs.value;
    inputArgs.companyId = companyId;
    this.companyUsersInputArgs.next(inputArgs);
  }

  toModel(): Store {
    const store: Store = {
      id: this.storeId,
      sapNumber: this.storeSAPNumber.value,
      name: this.storeName.value,
      email: this.storeEmail.value
    };
    return store;
  }

  upsert(){
    if (!this.myForm.valid) return;
    this.store = this.toModel();
    if (!this.storeId) {
      this.dataService.insert(this.store)
        .subscribe((response: Store) => {
          this.storeId = response.id;
          this.openSnackBar("Operation completed.", "Dismiss");
        },
        (error: AppError) => {
          if (error instanceof BadInput) {
            this.openSnackBar(error.originalError.message, "Dismiss");
          }
          else throw error;
        })
        .add(() => {
          this.saveBtnIsDisabled = false;
          this.populateCompanyUsers(this.storeId);
        });
    } else {
      this.dataService.update(this.store)
        .subscribe(() => {
          this.openSnackBar("Operation completed.", "Dismiss");
        },
        (error: AppError) => {
          if (error instanceof BadInput) {
            this.openSnackBar(error.originalError.message, "Dismiss");
          }
          else throw error;
        })
        .add(() => this.saveBtnIsDisabled = false);
    }
  }

  openSnackBar(message: string, action: string) {
    this.snackBar.open(message, action, {
      duration: 3000,
    });
  }
}

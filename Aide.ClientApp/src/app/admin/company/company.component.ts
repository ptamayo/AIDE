import { Component, OnInit } from '@angular/core';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { InsuranceCompanyService } from 'src/app/services/insurance-company.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AppError } from 'src/app/shared/common/app-error';
import { BadInput } from 'src/app/shared/common/bad-input';
import { BehaviorSubject } from 'rxjs';
import { forkJoin, of as observableOf } from 'rxjs';
import { UserRoleId } from 'src/app/enums/user-role-id.enum';
import { CompanyTypeId } from 'src/app/enums/company-type-id.enum';
import { Company } from 'src/app/models/company';
import { ClaimTypeService } from 'src/app/services/claim-type.service';
import { ClaimType } from 'src/app/models/claim-type';

@Component({
  selector: 'app-company',
  templateUrl: './company.component.html',
  styleUrls: ['./company.component.css']
})
export class CompanyComponent implements OnInit {
  private companyUsersInputArgs = new BehaviorSubject({
    companyTypeId: CompanyTypeId.Insurance,
    companyId: 0,
    userRoles: [UserRoleId.InsuranceReadOnly]
  });
  public eventStream$ = this.companyUsersInputArgs.asObservable();
  
  action: string = "Add";
  isLoadingPage: boolean;
  saveBtnIsDisabled: boolean = false;
  readonlyFormControl: boolean = false;

  insuranceCompanyId: number;
  insuranceCompany: Company | null;
  claimTypes: ClaimType[] = [];

  myForm: FormGroup;
  insuranceCompanyName = new FormControl('', [Validators.required, Validators.maxLength(250)]);
  insuranceCompanyEnabled = new FormControl();

  constructor(
    private route: ActivatedRoute, 
    private snackBar: MatSnackBar,
    private dataService: InsuranceCompanyService,
    private claimTypeService: ClaimTypeService) { }

  ngOnInit() {
    this.myForm = new FormGroup({
      insuranceCompanyName: this.insuranceCompanyName,
      insuranceCompanyEnabled: this.insuranceCompanyEnabled
    });

    this.insuranceCompanyId = +this.route.snapshot.paramMap.get('id');
    if (this.insuranceCompanyId) {
      this.action = "Edit";
    }
    this.populate();
  }

  populate() {
    this.isLoadingPage = true;
    let getClaimTypes = this.claimTypeService.getAll();
    let getInsuranceCompany = observableOf<Object>(null);
    if (this.insuranceCompanyId) {
      getInsuranceCompany = this.dataService.getById(this.insuranceCompanyId);
    }
    forkJoin([getClaimTypes, getInsuranceCompany]).subscribe(results => {
      this.claimTypes = <ClaimType[]>results[0];
      if (results[1]) {
        this.insuranceCompany = <Company>results[1];
        this.insuranceCompanyId = this.insuranceCompany.id;
        this.insuranceCompanyName.setValue(this.insuranceCompany.name);
        this.insuranceCompanyEnabled.setValue(this.insuranceCompany.isEnabled);
        this.populateCompanyUsers(this.insuranceCompanyId);
      }
      this.isLoadingPage = false;
    });
  }

  // Load company users in child component
  populateCompanyUsers(companyId) {
    let inputArgs = this.companyUsersInputArgs.value;
    inputArgs.companyId = companyId;
    this.companyUsersInputArgs.next(inputArgs);
  }

  toModel(): Company {
    const store: Company = {
      id: this.insuranceCompanyId,
      name: this.insuranceCompanyName.value,
      isEnabled: this.insuranceCompanyEnabled.value
    };
    return store;
  }

  upsert(){
    if (!this.myForm.valid) return;
    this.saveBtnIsDisabled = true;
    this.insuranceCompany = this.toModel();
    if (!this.insuranceCompanyId) {
      this.dataService.insert(this.insuranceCompany)
        .subscribe((response: Company) => {
          this.insuranceCompanyId = response.id;
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
          this.populateCompanyUsers(this.insuranceCompanyId);
        });
    } else {
      this.dataService.update(this.insuranceCompany)
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

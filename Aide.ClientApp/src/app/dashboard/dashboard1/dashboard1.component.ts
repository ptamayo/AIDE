import { Component, ViewChild, AfterViewInit, OnDestroy } from '@angular/core';
import { MatPaginator } from '@angular/material/paginator';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatSort } from '@angular/material/sort';
import { merge, of as observableOf, Subscription, Subject, Observable } from 'rxjs';
import { catchError, map, startWith, switchMap, delay } from 'rxjs/operators';
import { ClaimService } from 'src/app/services/claim.service';
import { Claim } from 'src/app/models/claim';
import { environment } from 'src/environments/environment';
import { ResizeService } from 'src/app/shared/services/resize.service';
import { PagedResult } from 'src/app/models/paged-result';
import { Dashboard1Filter } from './dashboard1.filter';
import { ClaimStatusId } from 'src/app/enums/claim-status-id.enum';
import { InsuranceCompany } from 'src/app/models/insurance-company';
import { InsuranceCompanyService } from 'src/app/services/insurance-company.service';
import { FormControl } from '@angular/forms';
import { Store } from 'src/app/models/store';
import { ClaimType } from 'src/app/models/claim-type';
import { StoreService } from 'src/app/services/store.service';
import { ClaimTypeService } from 'src/app/services/claim-type.service';
import { forkJoin } from 'rxjs';
import { AppError } from 'src/app/shared/common/app-error';
import { BadInput } from 'src/app/shared/common/bad-input';
import { AuthService } from 'src/app/services/auth.service';
import { UserRoleId } from 'src/app/enums/user-role-id.enum';
import { MessageBrokerService } from 'src/app/services/message-broker.service';
import { MatDialog, MatDialogConfig } from '@angular/material/dialog';
import { MessageDialogComponent } from 'src/app/shared/dialogs/message-dialog/message-dialog.component';

@Component({
  selector: 'app-dashboard1',
  templateUrl: './dashboard1.component.html',
  styleUrls: ['./dashboard1.component.css'],
})
export class Dashboard1Component implements AfterViewInit, OnDestroy {
  screenResizeSubscription: Subscription;
  screenSize = environment.screenSize;
  pageSize = environment.pageSize;
  columnDefinitions = [
    { def: 'dateCreated', hide: false }, 
    { def: 'createdByUser.firstName', hide: false }, 
    { def: 'externalOrderNumber', hide: false }, 
    { def: 'claimNumber', hide: false }, 
    { def: 'claimType.name', hide: false }, 
    { def: 'insuranceCompany.name', hide: false }, 
    { def: 'claimStatusId', hide: false }, 
    { def: 'actions', hide: false },
    { def: 'mobile', hide: true }
  ]
  data: Claim[] = [];
  resultsLength = 0;
  isLoadingResults = true;
  timeout: any = null;
  keywordSearchSubject = new Subject<string>();
  keywordSubscription: Subscription;
  keywordString: string = null;
  inputKeywordValue: string = "";
  inputClaimStatusValue: ClaimStatusId = ClaimStatusId.InProgress;
  reloadTableSubject = new Subject<boolean>();

  stores: Store[];
  storesFilteredOptions: Observable<Store[]>;
  inputStoreFilter = new FormControl();

  claimTypes: ClaimType[];
  inputServiceTypeValue: number = 0;

  insuranceCompanies: InsuranceCompany[];
  inputInsuranceCompanyValue: number = 0;

  inputStartDateValue: Date = null;
  inputEndDateValue: Date = null;

  public Status(key: number): string {
    return environment.ClaimStatus[key];
  }

  @ViewChild(MatPaginator) paginator: MatPaginator;
  @ViewChild(MatSort) sort: MatSort;

  constructor(
    private authService: AuthService,
    private resizeSvc: ResizeService, 
    private snackBar: MatSnackBar,
    private dialog: MatDialog,
    private dataService: ClaimService,
    private insuranceCompanyService: InsuranceCompanyService,
    private storeService: StoreService,
    private claimTypeService: ClaimTypeService,
    private messageBrokerService: MessageBrokerService) {
    this.screenResizeSubscription = this.resizeSvc.onResize$.pipe(delay(0)).subscribe((size) => {
      this.screenSize = size;
      this.showOrHideColumns();
    });
    this.showOrHideColumns();
    this.keywordSubscription = this.getKeywordString().subscribe(value => this.keywordString = value);
  } 

  private _filter(value: string): Store[] {
    if (this.stores) {
      const filterValue = value.toLowerCase();
      const filteredList = this.stores.filter(option => option.name.toLowerCase().includes(filterValue) || option.sapNumber.toLowerCase().includes(filterValue));
      return filteredList;
    } else {
      return [];
    }
  }

  ngOnInit() {
    // Get the data for filters
    if (this.authService.currentUser.roleId == UserRoleId.Admin) {
      let requestStores = this.storeService.getAll();
      let requestInsuranceCompanies = this.insuranceCompanyService.getAll();
      let claimTypes = this.getClaimTypeObservable();
      forkJoin([requestStores, requestInsuranceCompanies, claimTypes]).subscribe(results => {
        this.stores = <Store[]>results[0];
        this.insuranceCompanies = <InsuranceCompany[]>results[1];
        this.claimTypes = <ClaimType[]>results[2];
      },
      (error: AppError) => {
        if (error instanceof BadInput) {
          this.openSnackBar(error.originalError.message, "Dismiss");
        }
        else throw error;
      });
    }
  }

  ngAfterViewInit() {
    // Autocomplete filter for insurance companies
    this.storesFilteredOptions = this.inputStoreFilter.valueChanges
      .pipe(
        startWith(''),
        map(value => this._filter(value))
      );

    // If the user changes the sort order, reset back to the first page.
    this.sort.sortChange.subscribe(() => this.paginator.pageIndex = 0);

    merge(this.sort.sortChange, this.paginator.page, this.keywordSearchSubject, this.reloadTableSubject)
      .pipe(
        startWith({}),
        switchMap(() => {
          this.isLoadingResults = true;
          // return this.exampleDatabase!.getRepoIssues(this.sort.active, this.sort.direction, this.paginator.pageIndex);
          const pagingAndFilters = this.getDashboard1Filters();
          return this.dataService!.getPage(pagingAndFilters);
        }),
        map((page: PagedResult) => {
          // Flip flag to show that loading has finished.
          this.isLoadingResults = false;
          if (page) {
            if (this.paginator.pageIndex > (page.currentPage - 1)) {
              this.paginator.pageIndex = page.currentPage - 1;
            }
            this.resultsLength = page.rowCount;
            return page.results as Claim[];
          }
          else {
            return [];
          }
        }),
        catchError(() => {
          this.isLoadingResults = false;
          return observableOf([]);
        })
      ).subscribe(data => this.data = data);
  }

  get isPhoneDevice(): boolean {
    return environment.screenSize == 0;
  }

  get isFilterAllowed(): boolean {
    if (this.authService.currentUser.roleId == UserRoleId.Admin && !this.isPhoneDevice) return true;
    else return false;
  }

  getBackgroundColorStyle(row: Claim): string {
    // The colors will apply to status In Process only
    if (row.claimStatusId != environment.ClaimStatusInProcess) return '';
    const now = new Date();
    // The create date it's a string in format ISO-8601 and just added a letter Z at the end to indicate this is an UTC datetime
    // This will help to properly convert to local datetime in the following lines (https://www.digi.com/resources/documentation/digidocs/90001437-13/reference/r_iso_8601_date_format.htm)
    const dateCreatedUTCString = row.dateCreated + 'Z';
    const dateCreatedLocalTime = new Date(dateCreatedUTCString);
    const differenceInHours = Math.round(Math.abs(now.getTime() - dateCreatedLocalTime.getTime()) / 36e5);
    // The lines below are for debugging purposes only
    // if (row.id == 14268) {
    //   console.log(JSON.stringify(row));
    //   console.log(`Now: ${now}`);
    //   console.log(`Date Created (UTC): ${row.dateCreated} --> Local Time: ${dateCreatedLocalTime}`);
    //   console.log(`Difference in hours: ${differenceInHours}`);
    // }
    let color = '#d9ead3'; // green
    if (differenceInHours > 24) {
      color = '#fff2cc'; // yellow
    }
    if (differenceInHours > 48) {
      color = '#fce5cd'; // orange
    }
    if (differenceInHours > 72) {
      color = '#f4cccc'; // red
    }
    return `background-color: ${color};`;
  }

  getDashboard1Filters(): Dashboard1Filter {
    return <Dashboard1Filter> {
      pageSize: environment.pageSize,
      pageNumber: this.paginator.pageIndex + 1,
      keywords: this.keywordString ? encodeURIComponent(this.keywordString) : this.keywordString, // This is very important since it will enconde to UTF-8 (this is needed for special chars)
      statusId: this.inputClaimStatusValue,
      storeName: this.inputStoreFilter.value ? encodeURIComponent(this.inputStoreFilter.value) : this.inputStoreFilter.value,
      serviceTypeId: this.inputServiceTypeValue,
      insuranceCompanyId: this.inputInsuranceCompanyValue,
      startDate: this.inputStartDateValue,
      endDate: this.inputEndDateValue
    };
  }

  getClaimTypeObservable(): Observable<Object> {
    if (this.authService.currentUser.roleId == UserRoleId.Admin) {
      return this.claimTypeService.getAll();
    } else {
      return observableOf([]);
    }
  }

  getDisplayedColumns(): string[] {
    return this.columnDefinitions.filter(cd=>!cd.hide).map(cd=>cd.def);
  }

  showOrHideColumns() {
    if (this.columnDefinitions.length > 0) {
      if (this.screenSize >= 3) {
        this.columnDefinitions[0].hide = false;
        this.columnDefinitions[1].hide = false;
        this.columnDefinitions[2].hide = false;
        this.columnDefinitions[3].hide = false;
        this.columnDefinitions[4].hide = false;
        this.columnDefinitions[5].hide = false;
        this.columnDefinitions[6].hide = false;
        this.columnDefinitions[7].hide = false;
        this.columnDefinitions[8].hide = true;
      }
      else if (this.screenSize >= 1 && this.screenSize < 3) {
        this.columnDefinitions[0].hide = false;
        this.columnDefinitions[1].hide = true;
        this.columnDefinitions[2].hide = false;
        this.columnDefinitions[3].hide = false;
        this.columnDefinitions[4].hide = false;
        this.columnDefinitions[5].hide = true;
        this.columnDefinitions[6].hide = true;
        this.columnDefinitions[7].hide = false;
        this.columnDefinitions[8].hide = true;
      }
      if (this.screenSize < 1) {
        this.columnDefinitions[0].hide = true;
        this.columnDefinitions[1].hide = true;
        this.columnDefinitions[2].hide = true;
        this.columnDefinitions[3].hide = true;
        this.columnDefinitions[4].hide = true;
        this.columnDefinitions[5].hide = true;
        this.columnDefinitions[6].hide = true;
        this.columnDefinitions[7].hide = true;
        this.columnDefinitions[8].hide = false;
      }
    }
  }

  getKeywordString(): Observable<string> {
    return this.keywordSearchSubject.asObservable();
  }
  
  onKeySearch(event: any) {
    clearTimeout(this.timeout);
    var $this = this;
    this.timeout = setTimeout(function () {
      if (event.keyCode != 13) {
        $this.executeSearchByKerword(event.target.value);
      }
    }, 500);
  }

  onKeySearchCleaned(event: any) {
    this.inputKeywordValue = '';
    this.onKeySearch(event);
  }

  private executeSearchByKerword(value: string) {
    this.keywordSearchSubject.next(value);
  }
  
  get claimStatusList(): {id: number, name: string}[] {
    return Object.entries(environment.ClaimStatus).map(item => { 
      return { 
        id: +item[0], name: item[1] 
      }
    });
  }

  reloadOrders() {
    this.paginator.pageIndex = 0;
    this.reloadTableSubject.next(true); // This is the right way of refreshing the table. Don't use "this.populate()"
  }

  onClaimStatusChange() {
    this.reloadOrders();
  }

  onStoreFilterChange() {
    if (this.inputStoreFilter.value) {
      this.reloadOrders();
    }
  }

  onStoreFilterCleaned() {
    this.inputStoreFilter.setValue('');
    this.reloadOrders();
  }

  onServiceTypeChange() {
    this.reloadOrders();
  }

  onInsuranceCompanyChange() {
    this.reloadOrders();
  }

  onDateChange() {
    this.reloadOrders();
  }

  exportCsv() {
    const dialogRef = this.openInfoDialog('Exportar CSV', 'Info: El CSV ya se está procesando, porfavor espere la notificación en la campanita.', 'info');
    dialogRef.afterClosed().subscribe();
    const pagingAndFilters = this.getDashboard1Filters();
    this.messageBrokerService.dashboard1ClaimsReport(pagingAndFilters).subscribe(() => { },
    (error: AppError) => {
      if (error instanceof BadInput) {
        this.openSnackBar(error.originalError.message, "Dismiss");
      }
      else throw error;
    });
  }

  openSnackBar(message: string, action: string) {
    this.snackBar.open(message, action, {
      duration: 3000,
    });
  }

  openInfoDialog(title: string, message: string, icon: string) {
    const dialogConfig = new MatDialogConfig();
    dialogConfig.data = { title: title, message: message, icon: icon };
    return this.dialog.open(MessageDialogComponent, dialogConfig);
  }
  
  ngOnDestroy(): void {
    this.screenResizeSubscription.unsubscribe();
    this.keywordSubscription.unsubscribe();
  }
}

import { Component, AfterViewInit, Input, ViewChild, OnDestroy } from '@angular/core';
import { Observable, Subscription, Subject, merge, of as observableOf } from 'rxjs';
import { MatDialogConfig, MatDialog } from '@angular/material/dialog';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { User } from 'src/app/models/user';
import { environment } from 'src/environments/environment';
import { startWith, switchMap, catchError, map } from 'rxjs/operators';
import { UsersFilter } from '../users/users-filter';
import { CompanyTypeId } from 'src/app/enums/company-type-id.enum';
import { PagedResult } from 'src/app/models/paged-result';
import { UserRoleId } from 'src/app/enums/user-role-id.enum';
import { UserDialogComponent } from 'src/app/shared/dialogs/user-dialog/user-dialog.component';
import { StoreService } from 'src/app/services/store.service';
import { UserService } from 'src/app/services/user.service';

@Component({
  selector: 'app-company-users',
  templateUrl: './company-users.component.html',
  styleUrls: ['./company-users.component.css']
})
export class CompanyUsersComponent implements AfterViewInit, OnDestroy {
  @Input() eventStream: Observable<any>;

  companyTypeId: CompanyTypeId;
  companyId: number;
  userRoles: UserRoleId[];
  userRolesSelected: UserRoleId[];

  columnDefinitions = [
    { def: 'roleId', hide: false }, 
    { def: 'name', hide: false }, 
    { def: 'email', hide: false }, 
    { def: 'actions', hide: false }
  ]
  data: User[] = [];
  isLoadingResults: boolean = true;
  pageSize = environment.pageSize;
  resultsLength = 0;

  timeout: any = null;
  keywordSearchSubject = new Subject<string>();
  keywordSubscription: Subscription;
  keywordString: string = null;
  inputKeywordValue: string = "";
  inputUserRoleValue: UserRoleId = UserRoleId.Unknown;
  
  reloadTableSubject = new Subject<boolean>();

  constructor(private dialog: MatDialog, private dataService: StoreService, private userService: UserService) {
    this.keywordSubscription = this.getKeywordString().subscribe(value => this.keywordString = value);
  }

  @ViewChild(MatPaginator) paginator: MatPaginator;
  @ViewChild(MatSort) sort: MatSort;

  ngAfterViewInit() {
    // If the user changes the sort order, reset back to the first page.
    this.sort.sortChange.subscribe(() => this.paginator.pageIndex = 0);
    this.eventStream.subscribe(value => {
      if (value.companyId > 0 || (value.companyId == 0 && value.userRoles[0] == UserRoleId.Admin)) {
        this.companyTypeId = value.companyTypeId;
        this.companyId = +value.companyId;
        this.userRoles = value.userRoles as UserRoleId[];
        this.userRolesSelected = value.userRoles as UserRoleId[];
        this.reloadTableSubject.next(true);
      }
    });
    this.populate();
  }
  
  getUserRole(roleId) {
    return environment.UserRole[roleId];
  }
  
  getNameOfUserRole(roleId) {
    return environment.UserRole[roleId];
  }

  // IMPORTANT: This method is intended to run 1 time only.
  // After that, only Emitters or Subjects should trigger the data load in the table.
  populate() {
    this.isLoadingResults = false;
    merge(this.sort.sortChange, this.paginator.page, this.keywordSearchSubject, this.reloadTableSubject)
      .pipe(
        startWith({}),
        switchMap(() => {
          if (!this.userRoles) return observableOf([]); // This is very important to let the local vars get initialized
          this.isLoadingResults = true;
          // return this.exampleDatabase!.getRepoIssues(this.sort.active, this.sort.direction, this.paginator.pageIndex);
          const pagingAndFilters: UsersFilter = {
            pageSize: environment.pageSize,
            pageNumber: this.paginator.pageIndex + 1,
            keywords: this.keywordString ? encodeURIComponent(this.keywordString) : this.keywordString, // This is very important since it will enconde to UTF-8 (this is needed for special chars)
            companyId: this.companyId,
            companyTypeId: this.companyTypeId, // i.e. StoreId or InsuranceCompanyId
            userRoleIds: this.userRolesSelected // i.e. WsAdmin, WsOperator, InsuranceReadOnly, Admin
          };
          return this.userService!.getPage(pagingAndFilters);
        }),
        map((page: PagedResult) => {
          // Flip flag to show that loading has finished.
          this.isLoadingResults = false;
          if (page) {
            if (this.paginator.pageIndex > (page.currentPage - 1)) {
              this.paginator.pageIndex = page.currentPage - 1;
            }
            this.resultsLength = page.rowCount;
            return page.results as User[];
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

  getDisplayedColumns(): string[] {
    return this.columnDefinitions.filter(cd=>!cd.hide).map(cd=>cd.def);
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
    this.inputKeywordValue = "";
    this.onKeySearch(event);
  }

  private executeSearchByKerword(value: string) {
    this.keywordSearchSubject.next(value); // Notice the populate() method is not being called but triggered the subject
  }
  
  openDialog(userId: number) {
    const dialogConfig = new MatDialogConfig();
    dialogConfig.disableClose = true;
    dialogConfig.autoFocus = true;
    if (environment.screenSize <= 2) {
      // Phone in portrait or landscape
      dialogConfig.minHeight = '100%';
      dialogConfig.minWidth = '100%'
      dialogConfig.maxHeight = '100%';
      dialogConfig.maxWidth = '100%';
    }
    else {
      // Any other device bigger than a phone
      dialogConfig.minHeight = '75%';
      dialogConfig.minWidth = '50%'
      dialogConfig.maxHeight = '75%';
      dialogConfig.maxWidth = '50%';
    }
    dialogConfig.data = { 
      companyTypeId: this.companyTypeId, // i.e. CompanyTypeId.Store
      companyId: this.companyId, 
      userId: userId,
      userRoles: this.userRoles // i.e. [UserRoleId.WsAdmin, UserRoleId.WsOperator]
    };

    const dialogRef = this.dialog.open(UserDialogComponent, dialogConfig);
    dialogRef.afterClosed().subscribe((user: User) => {
      if (user) {
        this.reloadUsers();
      }
    });
  }

  reloadUsers() {
    this.paginator.pageIndex = 0;
    this.reloadTableSubject.next(true); // This is the right way of refreshing the table. Don't use "this.populate()"
  }

  onUserRoleChange($event: any) {
    if ($event.value === UserRoleId.Unknown) {
      this.userRolesSelected = this.userRoles;
    }
    else {
      this.userRolesSelected = [this.inputUserRoleValue];
    }
    this.reloadUsers();
  }

  ngOnDestroy(): void {
    this.keywordSubscription.unsubscribe();
  }
}

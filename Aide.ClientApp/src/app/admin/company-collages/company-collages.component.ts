import { Component, AfterViewInit, Input, ViewChild, OnDestroy } from '@angular/core';
import { Observable, Subscription, Subject, merge, of as observableOf } from 'rxjs';
import { MatDialogConfig, MatDialog } from '@angular/material/dialog';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { environment } from 'src/environments/environment';
import { startWith, switchMap, catchError, map } from 'rxjs/operators';
import { PagedResult } from 'src/app/models/paged-result';
import { InsuranceCollage } from 'src/app/models/insurance-collage';
import { CompanyCollagesFilter } from './company-collages-filter';
import { InsuranceCollageService } from 'src/app/services/insurance-collage-service';
import { CollageDialogComponent } from 'src/app/shared/dialogs/collage-dialog/collage-dialog.component';

@Component({
  selector: 'app-company-collages',
  templateUrl: './company-collages.component.html',
  styleUrls: ['./company-collages.component.css']
})
export class CompanyCollagesComponent implements AfterViewInit, OnDestroy {
  @Input() companyId: number;

  columnDefinitions = [
    { def: 'name', hide: false }, 
    { def: 'claimTypeName', hide: false }, 
    { def: 'actions', hide: false }
  ]
  data: InsuranceCollage[] = [];
  isLoadingResults: boolean = true;
  pageSize = environment.pageSize;
  resultsLength = 0;

  timeout: any = null;
  keywordSearchSubject = new Subject<string>();
  keywordSubscription: Subscription;
  keywordString: string = null;
  inputKeywordValue = '';
  
  reloadTableSubject = new Subject<boolean>();

  constructor(private dialog: MatDialog, private dataService: InsuranceCollageService) {
    this.keywordSubscription = this.getKeywordString().subscribe(value => this.keywordString = value);
  }

  @ViewChild(MatPaginator) paginator: MatPaginator;
  @ViewChild(MatSort) sort: MatSort;

  ngAfterViewInit() {
    // If the user changes the sort order, reset back to the first page.
    this.sort.sortChange.subscribe(() => this.paginator.pageIndex = 0);
    // this.eventStream.subscribe(value => {
    //   if (value.companyId > 0) {
    //     // this.companyTypeId = value.companyTypeId;
    //     this.companyId = +value.companyId;
    //     // this.userRoles = value.userRoles as UserRoleId[];
    //     this.reloadTableSubject.next(true);
    //   }
    // });
    this.populate();
  }
  
  // IMPORTANT: This method is intended to run 1 time only.
  // After that, only Emitters or Subjects should trigger the data load in the table.
  populate() {
    this.isLoadingResults = false;
    merge(this.sort.sortChange, this.paginator.page, this.keywordSearchSubject, this.reloadTableSubject)
      .pipe(
        startWith({}),
        switchMap(() => {
          // if (!this.companyId) {
          //   if (!this.userRoles || this.userRoles[0] != UserRoleId.Admin) {
          //     return observableOf([]); // This is very important to prevent a call to the api when there's no company id specified
          //   }
          // }
          this.isLoadingResults = true;
          // return this.exampleDatabase!.getRepoIssues(this.sort.active, this.sort.direction, this.paginator.pageIndex);
          const pagingAndFilters: CompanyCollagesFilter = {
            pageSize: environment.pageSize,
            pageNumber: this.paginator.pageIndex + 1,
            keywords: this.keywordString ? encodeURIComponent(this.keywordString) : this.keywordString, // This is very important since it will enconde to UTF-8 (this is needed for special chars)
            companyId: this.companyId
          };
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
            return page.results as InsuranceCollage[];
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
    this.inputKeywordValue = '';
    this.onKeySearch(event);
  }

  private executeSearchByKerword(value: string) {
    this.keywordSearchSubject.next(value); // Notice the populate() method is not being called but triggered the subject
  }
  
  openDialog(collageId: number) {
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
      companyId: this.companyId,
      collageId: collageId
    };

    const dialogRef = this.dialog.open(CollageDialogComponent, dialogConfig);
    dialogRef.afterClosed().subscribe((collage: InsuranceCollage) => {
        if (collage) {
          this.reloadCollages();
        }
      }
    );
  }

  reloadCollages() {
    this.paginator.pageIndex = 0;
    this.reloadTableSubject.next(true); // This is the right way of refreshing the table. Don't use "this.populate()"
  }

  ngOnDestroy(): void {
    this.keywordSubscription.unsubscribe();
  }
}

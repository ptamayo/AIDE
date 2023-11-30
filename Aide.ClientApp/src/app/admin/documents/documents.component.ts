import { Component, OnDestroy, ViewChild, AfterViewInit } from '@angular/core';
import { Subject, Subscription, Observable, merge, of as observableOf } from 'rxjs';
import { environment } from 'src/environments/environment';
import { ProbatoryDocument } from 'src/app/models/probatory-document';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { startWith, switchMap, map, catchError } from 'rxjs/operators';
import { PagedResult } from 'src/app/models/paged-result';
import { DocumentsFilter } from './documents-filter';
import { DocumentService } from 'src/app/services/document.service';

@Component({
  selector: 'app-documents',
  templateUrl: './documents.component.html',
  styleUrls: ['./documents.component.css']
})
export class DocumentsComponent implements AfterViewInit, OnDestroy {
  columnDefinitions = [
    { def: 'dateCreated', hide: false }, 
    { def: 'name', hide: false }, 
    { def: 'actions', hide: false }
  ]
  data: ProbatoryDocument[] = [];
  isLoadingResults = true;
  pageSize = environment.pageSize;
  resultsLength = 0;
  timeout: any = null;
  keywordSearchSubject = new Subject<string>();
  keywordSubscription: Subscription;
  keywordString: string = null;
  inputKeywordValue = '';

  @ViewChild(MatPaginator) paginator: MatPaginator;
  @ViewChild(MatSort) sort: MatSort;
  
  constructor(private dataService: DocumentService) { 
    this.keywordSubscription = this.getKeywordString().subscribe(value => this.keywordString = value);
  }

  ngAfterViewInit() {
    // If the user changes the sort order, reset back to the first page.
    this.sort.sortChange.subscribe(() => this.paginator.pageIndex = 0);

    merge(this.sort.sortChange, this.paginator.page, this.keywordSearchSubject)
      .pipe(
        startWith({}),
        switchMap(() => {
          this.isLoadingResults = true;
          // return this.exampleDatabase!.getRepoIssues(this.sort.active, this.sort.direction, this.paginator.pageIndex);
          const pagingAndFilters: DocumentsFilter = {
            pageSize: environment.pageSize,
            pageNumber: this.paginator.pageIndex + 1,
            keywords: this.keywordString ? encodeURIComponent(this.keywordString) : this.keywordString // This is very important since it will enconde to UTF-8 (this is needed for special chars)
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
            return page.results as ProbatoryDocument[];
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
    this.keywordSearchSubject.next(value);
  }
  
  ngOnDestroy(): void {
    this.keywordSubscription.unsubscribe();
  }
}

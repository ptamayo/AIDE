import { BadInput } from '../common/bad-input';
import { NotFoundError } from '../common/not-found-error';
import { AppError } from '../common/app-error';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { throwError, Observable } from 'rxjs';
import { catchError } from 'rxjs/operators';

const httpOptions = {
  headers: new HttpHeaders({
      'Content-Type': 'application/json'
  })
};

export class DataService {
  constructor(private url: string, private http: HttpClient) { }

  protected get(route?: string) {
      return this.http.get(this.url + (route != null ? route : '')).pipe(catchError(this.handleError));
  }

  protected post(resource, route?: string) {
      return this.http.post(this.url + (route != null ? route : ''), JSON.stringify(resource), httpOptions).pipe(catchError(this.handleError));
  }

  protected put(resource, route?: string) {
      return this.http.put(this.url + (route != null ? route : ''), JSON.stringify(resource), httpOptions).pipe(catchError(this.handleError));
  }

  protected del(route: string) {
      return this.http.delete(this.url + route).pipe(catchError(this.handleError));
  }

  private handleError(error: Response) {
      // var jsonErrorMessage = JSON.stringify(error);

      if (error.status === 400)
          return throwError(new BadInput(error));
      // return Observable.throw(new BadInput(error.json()));

      if (error.status === 404)
          return throwError(new NotFoundError(error));
      // return Observable.throw(new NotFoundError());

      return Observable.throw(new AppError(error));
  }
}

import { Injectable } from '@angular/core';
import { CanActivate, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {

  constructor(private router: Router, private authService: AuthService) { }

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot) {
    if (this.authService.isLoggedIn()) {
      if (route.data && route.data.roles) {
        if (route.data.roles.indexOf(this.authService.currentUser.roleId) > -1) {
          return true;
        }
        else {
          this.router.navigate(['/no-access']);
          return false;
        }
      }
    }
    this.router.navigate(['/login'], { queryParams: { returnUrl: state.url }});
    return false;
  }
}

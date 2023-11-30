import { Component } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { AppError } from '../shared/common/app-error';
import { BadInput } from '../shared/common/bad-input';
import { AuthResponse } from '../models/auth-response';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  loading = false;
  invalidLogin: boolean = false;
  isUserLocked: boolean = false;

  constructor(private router: Router, 
    private route: ActivatedRoute,
    private authService: AuthService) { }

    urlLogo(): string {
      return environment.logo;
    }

    // This event is only intended to reset the flags for displaying messages
    // related to invalid psw or security block upon focusing on psw control
    onFocusPswEvent() {
      if (this.invalidLogin) this.invalidLogin = false;
      if (this.isUserLocked) this.isUserLocked = false;
    }

    signIn(credentials) {
      this.loading = true;
      this.invalidLogin = false;
      this.isUserLocked = false;
      this.authService.login(credentials)
        .subscribe(result => { 
          const response = result as AuthResponse;
          if (response.isLoginSuccessful) {
            localStorage.setItem('token', response.token);
            const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
            this.router.navigate([returnUrl || '/']);
          }
          else if (response.isUserLocked) {
            this.isUserLocked = true;
          }
          else {
            this.invalidLogin = true;
          }
          this.loading = false;
        },
        (error: AppError) => {
          this.loading = false;
          if (error instanceof BadInput) {
            this.invalidLogin = true;
          }
          else throw error;
        });
    }
}

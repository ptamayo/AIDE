import { Component, OnInit } from '@angular/core';
import { AuthService } from 'src/app/services/auth.service';
import { ActivatedRoute } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { UserService } from 'src/app/services/user.service';
import { FormGroup, FormControl, Validators, FormBuilder } from '@angular/forms';
import { User } from 'src/app/models/user';
import { UserProfile } from 'src/app/models/user-profile';
import { AppError } from 'src/app/shared/common/app-error';
import { BadInput } from 'src/app/shared/common/bad-input';
import { sha256 } from 'js-sha256';
// import custom validator to validate that password and confirm password fields match
import { MustMatch } from 'src/app/shared/helpers/must-match.validator';
import { UpdateUserProfileResponse } from './update-user-profile-response';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {
  isLoadingPage: boolean;
  saveBtnIsDisabled: boolean = false;

  userId: number;
  user: User | null;

  myForm: FormGroup;
  email = new FormControl('', [Validators.required, Validators.email, Validators.maxLength(100)]);
  firstName = new FormControl('', [Validators.required, Validators.maxLength(50)]);
  lastName = new FormControl('', [Validators.required, Validators.maxLength(50)]);

  myForm2: FormGroup;
  psw1 = new FormControl('', [Validators.required, Validators.minLength(6), Validators.maxLength(50)]);
  psw2 = new FormControl('', [Validators.required, Validators.maxLength(50)]);

  constructor(private authService: AuthService,
    private route: ActivatedRoute, 
    private snackBar: MatSnackBar,
    private userService: UserService,
    private formBuilder: FormBuilder) { }

  ngOnInit() {
    this.myForm = new FormGroup({
      email: this.email,
      firstName: this.firstName,
      lastName: this.lastName
    });
    this.myForm2 = this.formBuilder.group({
      psw1: this.psw1,
      psw2: this.psw2
    }, { validator: MustMatch('psw1', 'psw2') });
    this.userId = this.authService.currentUser.id;
    this.populate();
  }

  populate() {
    if (this.userId) {
      this.isLoadingPage = true;
      this.userService.getById(this.userId).subscribe((data: User) => {
        if (data) {
          this.user = data;
          this.userId = this.user.id;
          this.email.setValue(this.user.email);
          this.firstName.setValue(this.user.firstName);
          this.lastName.setValue(this.user.lastName);
        }
        this.isLoadingPage = false;
      });
    }
  }

  toModel(): UserProfile {
    const userProfile: UserProfile = {
      id: this.userId,
      email: this.email.value,
      firstName: this.firstName.value,
      lastName: this.lastName.value,
      psw: null
    };
    return userProfile;
  }

  toModel2(): UserProfile {
    const userProfile = this.toModel();
    userProfile.psw = sha256(this.psw1.value);
    return userProfile;
  }

  upsert() {
    if (!this.myForm.valid) { 
      this.myForm.markAllAsTouched(); 
      return; 
    }
    const userProfile = this.toModel();
    if (this.userId) {
      this.saveBtnIsDisabled = true;
      this.userService.updateProfile(userProfile)
        .subscribe((response: UserProfile) => {
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

  get f() { return this.myForm2.controls; }

  upsert2() {
    if (!this.myForm2.valid) { 
      this.myForm2.markAllAsTouched(); 
      return; 
    }
    const userProfile = this.toModel2();
    if (this.userId) {
      this.saveBtnIsDisabled = true;
      this.userService.updateProfile(userProfile)
        .subscribe((response: UpdateUserProfileResponse) => {
          if (response.isOperationSuccesful) {
            this.openSnackBar("Operation completed.", "Dismiss");
          } else {
            this.openSnackBar(response.message, "ERROR");
          }
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

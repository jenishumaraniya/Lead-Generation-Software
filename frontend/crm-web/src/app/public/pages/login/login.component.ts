import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  loginForm: FormGroup;
  forgotForm: FormGroup;
  resetForm: FormGroup;

  selectedPortal: 'ADMIN' | 'SALES' = 'ADMIN';
  viewMode: 'LOGIN' | 'FORGOT' | 'RESET' = 'LOGIN';

  loginError: string = '';
  forgotSuccessMessage: string = '';
  forgotErrorMessage: string = '';
  resetSuccessMessage: string = '';
  resetErrorMessage: string = '';

  isLoading = false;
  showPassword = false;
  showNewPassword = false;
  returnUrl: string;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });

    this.forgotForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });

    this.resetForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      code: ['', [Validators.required]],
      newPassword: ['', [Validators.required, Validators.minLength(6)]]
    });

    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '';
    if (this.authService.isAuthenticated()) {
      this.redirectBasedOnRole();
    }
  }

  setPortal(portal: 'ADMIN' | 'SALES'): void {
    this.selectedPortal = portal;
    this.loginError = '';
  }

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }

  toggleNewPassword(): void {
    this.showNewPassword = !this.showNewPassword;
  }

  switchView(mode: 'LOGIN' | 'FORGOT' | 'RESET'): void {
    this.viewMode = mode;
    this.loginError = '';
    this.forgotSuccessMessage = '';
    this.forgotErrorMessage = '';
    this.resetSuccessMessage = '';
    this.resetErrorMessage = '';
  }

  onSubmit(): void {
    if (this.loginForm.invalid) return;
    this.isLoading = true;
    this.loginError = '';

    const { email, password } = this.loginForm.value;
    this.authService.login(email, password).subscribe({
      next: (res) => {
        this.isLoading = false;
        // Verify role matches selected portal if necessary, or route automatically
        this.redirectBasedOnRole();
      },
      error: (err) => {
        this.isLoading = false;
        this.loginError = err.error?.message || err.error?.error || 'Invalid email or password';
      }
    });
  }

  onForgotPasswordSubmit(): void {
    if (this.forgotForm.invalid) return;
    this.isLoading = true;
    this.forgotSuccessMessage = '';
    this.forgotErrorMessage = '';

    const email = this.forgotForm.value.email;
    this.authService.forgotPassword(email).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.forgotSuccessMessage = 'Password reset instructions / verification code generated.';
        this.resetForm.patchValue({ email, code: res.code || '' });
        setTimeout(() => {
          this.switchView('RESET');
        }, 1500);
      },
      error: (err) => {
        this.isLoading = false;
        this.forgotErrorMessage = err.error?.message || err.error?.error || 'Failed to process request.';
      }
    });
  }

  onResetPasswordSubmit(): void {
    if (this.resetForm.invalid) return;
    this.isLoading = true;
    this.resetSuccessMessage = '';
    this.resetErrorMessage = '';

    this.authService.resetPassword(this.resetForm.value).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.resetSuccessMessage = res.message || 'Password successfully updated!';
        setTimeout(() => {
          this.switchView('LOGIN');
          this.loginForm.patchValue({ email: this.resetForm.value.email, password: '' });
        }, 2000);
      },
      error: (err) => {
        this.isLoading = false;
        this.resetErrorMessage = err.error?.message || err.error?.error || 'Failed to reset password.';
      }
    });
  }

  private redirectBasedOnRole(): void {
    const user = this.authService.getCurrentUser();
    const role = user?.role;

    if (this.returnUrl && this.returnUrl !== '/' && this.returnUrl !== '/login') {
      if (role === 'ADMIN') {
        this.router.navigateByUrl(this.returnUrl);
        return;
      }
      if ((role === 'SALES' || role === 'SALES_REP') && this.returnUrl.startsWith('/sales')) {
        this.router.navigateByUrl(this.returnUrl);
        return;
      }
    }

    if (role === 'ADMIN') {
      this.router.navigate(['/admin/dashboard']);
    } else if (role === 'SALES' || role === 'SALES_REP') {
      this.router.navigate(['/sales/dashboard']);
    } else {
      this.router.navigate(['/']);
    }
  }
}
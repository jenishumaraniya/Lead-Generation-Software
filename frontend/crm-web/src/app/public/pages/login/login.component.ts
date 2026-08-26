import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  loginForm: FormGroup;
  loginError: string = '';
  isLoading = false;
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
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '';
    if (this.authService.isAuthenticated()) {
      this.redirectBasedOnRole();
    }
  }

  quickFill(role: 'admin' | 'sales'): void {
    if (role === 'admin') {
      this.loginForm.patchValue({
        email: 'admin@leadgen.com',
        password: 'Admin@123'
      });
    } else {
      this.loginForm.patchValue({
        email: 'sales@leadgen.com',
        password: 'Sales@123'
      });
    }
  }

  onSubmit(): void {
    if (this.loginForm.invalid) return;
    this.isLoading = true;
    this.loginError = '';

    const { email, password } = this.loginForm.value;
    this.authService.login(email, password).subscribe({
      next: () => {
        this.isLoading = false;
        this.redirectBasedOnRole();
      },
      error: (err) => {
        this.isLoading = false;
        this.loginError = err.error?.message || err.error?.error || 'Invalid email or password';
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
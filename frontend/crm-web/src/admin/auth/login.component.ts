import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../app/core/services/auth.service';

@Component({
  selector: 'app-admin-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  credentials = {
    email: '',
    password: ''
  };

  isLoading = false;
  errorMessage = '';
  returnUrl = '/admin/dashboard';

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/admin/dashboard';
    if (this.authService.isAuthenticated()) {
      this.router.navigateByUrl(this.returnUrl);
    }
  }

  onSubmit(): void {
    if (!this.credentials.email || !this.credentials.password) {
      this.errorMessage = 'Please enter your email and password.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.authService.login(this.credentials).subscribe({
      next: () => {
        this.isLoading = false;
        this.router.navigateByUrl(this.returnUrl);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.error || 'Invalid credentials or account locked. Please check your details.';
      }
    });
  }

  fillAdmin(): void {
    this.credentials.email = 'admin@leadgen.com';
    this.credentials.password = 'Admin@123';
    this.errorMessage = '';
  }

  fillSales(): void {
    this.credentials.email = 'sales@leadgen.com';
    this.credentials.password = 'Sales@123';
    this.errorMessage = '';
  }
}

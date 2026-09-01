import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-change-password-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="modal-backdrop" *ngIf="isOpen" (click)="closeModal()">
      <div class="modal-card" (click)="$event.stopPropagation()">
        <div class="modal-header">
          <h3>🔒 Change Workspace Password</h3>
          <button type="button" class="btn-close" (click)="closeModal()">×</button>
        </div>

        <form [formGroup]="pwForm" (ngSubmit)="onSubmit()">
          <div class="form-group">
            <label>Current Password</label>
            <div class="pw-wrapper">
              <input [type]="showCurrent ? 'text' : 'password'" formControlName="currentPassword" placeholder="Enter current password" />
              <button type="button" class="btn-eye" (click)="showCurrent = !showCurrent">{{ showCurrent ? '🔒' : '👁️' }}</button>
            </div>
            <small class="err" *ngIf="pwForm.get('currentPassword')?.invalid && pwForm.get('currentPassword')?.touched">Current password is required</small>
          </div>

          <div class="form-group">
            <label>New Password</label>
            <div class="pw-wrapper">
              <input [type]="showNew ? 'text' : 'password'" formControlName="newPassword" placeholder="Minimum 6 characters" />
              <button type="button" class="btn-eye" (click)="showNew = !showNew">{{ showNew ? '🔒' : '👁️' }}</button>
            </div>
            <small class="err" *ngIf="pwForm.get('newPassword')?.invalid && pwForm.get('newPassword')?.touched">New password must be at least 6 characters</small>
          </div>

          <div class="alert-success" *ngIf="successMsg">{{ successMsg }}</div>
          <div class="alert-error" *ngIf="errorMsg">{{ errorMsg }}</div>

          <div class="modal-actions">
            <button type="button" class="btn-cancel" (click)="closeModal()">Cancel</button>
            <button type="submit" class="btn-submit" [disabled]="pwForm.invalid || isLoading">
              {{ isLoading ? 'Saving...' : 'Update Password' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  `,
  styles: [`
    .modal-backdrop {
      position: fixed;
      top: 0; left: 0; right: 0; bottom: 0;
      background: rgba(15, 23, 42, 0.6);
      backdrop-filter: blur(4px);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 2000;
      padding: 20px;
    }
    .modal-card {
      background: #ffffff;
      border-radius: 14px;
      padding: 28px;
      width: 100%;
      max-width: 420px;
      box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1);
    }
    .modal-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }
    .modal-header h3 {
      font-size: 1.15rem;
      font-weight: 700;
      color: #0f172a;
      margin: 0;
    }
    .btn-close {
      border: none;
      background: transparent;
      font-size: 1.5rem;
      color: #64748b;
      cursor: pointer;
    }
    .form-group {
      margin-bottom: 16px;
    }
    .form-group label {
      display: block;
      font-size: 0.85rem;
      font-weight: 600;
      color: #334155;
      margin-bottom: 6px;
    }
    .pw-wrapper {
      position: relative;
      display: flex;
      align-items: center;
    }
    .pw-wrapper input {
      width: 100%;
      padding: 10px 42px 10px 12px;
      border: 1px solid #cbd5e1;
      border-radius: 8px;
      font-size: 0.9rem;
      outline: none;
      box-sizing: border-box;
    }
    .pw-wrapper input:focus {
      border-color: #2563eb;
    }
    .btn-eye {
      position: absolute;
      right: 8px;
      background: transparent;
      border: none;
      cursor: pointer;
      font-size: 1rem;
    }
    .err {
      color: #ef4444;
      font-size: 0.75rem;
      margin-top: 4px;
      display: block;
    }
    .alert-success {
      background: #f0fdf4;
      color: #16a34a;
      padding: 8px 12px;
      border-radius: 6px;
      font-size: 0.85rem;
      margin-bottom: 14px;
    }
    .alert-error {
      background: #fef2f2;
      color: #dc2626;
      padding: 8px 12px;
      border-radius: 6px;
      font-size: 0.85rem;
      margin-bottom: 14px;
    }
    .modal-actions {
      display: flex;
      justify-content: flex-end;
      gap: 10px;
      margin-top: 24px;
    }
    .btn-cancel {
      padding: 9px 16px;
      background: #f1f5f9;
      color: #475569;
      border: none;
      border-radius: 8px;
      font-weight: 600;
      cursor: pointer;
    }
    .btn-submit {
      padding: 9px 18px;
      background: #2563eb;
      color: #ffffff;
      border: none;
      border-radius: 8px;
      font-weight: 600;
      cursor: pointer;
    }
  `]
})
export class ChangePasswordModalComponent {
  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();

  pwForm: FormGroup;
  showCurrent = false;
  showNew = false;
  isLoading = false;
  successMsg = '';
  errorMsg = '';

  constructor(private fb: FormBuilder, private authService: AuthService) {
    this.pwForm = this.fb.group({
      currentPassword: ['', [Validators.required]],
      newPassword: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  closeModal(): void {
    this.pwForm.reset();
    this.successMsg = '';
    this.errorMsg = '';
    this.close.emit();
  }

  onSubmit(): void {
    if (this.pwForm.invalid) return;
    this.isLoading = true;
    this.successMsg = '';
    this.errorMsg = '';

    this.authService.changePassword(this.pwForm.value).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.successMsg = res.message || 'Password changed successfully!';
        setTimeout(() => this.closeModal(), 1500);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMsg = err.error?.error || err.error?.message || 'Failed to change password.';
      }
    });
  }
}

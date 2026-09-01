import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService, ToastMessage } from '../../core/services/toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="toast-wrapper" *ngIf="toasts.length > 0">
      <div 
        *ngFor="let t of toasts" 
        class="toast-item" 
        [class]="'toast-' + t.type"
        (click)="removeToast(t.id)"
      >
        <span class="toast-icon">
          {{ t.type === 'success' ? '✓' : (t.type === 'error' ? '✕' : (t.type === 'warning' ? '⚠' : 'ℹ')) }}
        </span>
        <div class="toast-text">
          <strong *ngIf="t.title" class="toast-title">{{ t.title }}</strong>
          <span class="toast-msg">{{ t.message }}</span>
        </div>
        <button type="button" class="toast-close">×</button>
      </div>
    </div>
  `,
  styles: [`
    .toast-wrapper {
      position: fixed;
      bottom: 24px;
      right: 24px;
      z-index: 9999;
      display: flex;
      flex-direction: column;
      gap: 10px;
      pointer-events: none;
      max-width: 400px;
    }
    .toast-item {
      pointer-events: auto;
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 12px 18px;
      border-radius: 10px;
      background: #ffffff;
      box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.15), 0 8px 10px -6px rgba(0, 0, 0, 0.1);
      border: 1px solid #e2e8f0;
      cursor: pointer;
      animation: slideUp 0.25s cubic-bezier(0.4, 0, 0.2, 1);
      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
    }
    .toast-icon {
      font-weight: 800;
      font-size: 14px;
      width: 24px;
      height: 24px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }
    .toast-success {
      border-left: 4px solid #10b981;
    }
    .toast-success .toast-icon {
      background: #ecfdf5;
      color: #059669;
    }
    .toast-error {
      border-left: 4px solid #ef4444;
    }
    .toast-error .toast-icon {
      background: #fef2f2;
      color: #dc2626;
    }
    .toast-warning {
      border-left: 4px solid #f59e0b;
    }
    .toast-warning .toast-icon {
      background: #fffbeb;
      color: #d97706;
    }
    .toast-info {
      border-left: 4px solid #3b82f6;
    }
    .toast-info .toast-icon {
      background: #eff6ff;
      color: #2563eb;
    }
    .toast-text {
      flex: 1;
      display: flex;
      flex-direction: column;
    }
    .toast-title {
      font-size: 0.85rem;
      color: #0f172a;
    }
    .toast-msg {
      font-size: 0.85rem;
      color: #334155;
      line-height: 1.4;
    }
    .toast-close {
      border: none;
      background: transparent;
      font-size: 1.1rem;
      color: #94a3b8;
      cursor: pointer;
    }
    @keyframes slideUp {
      from { opacity: 0; transform: translateY(16px); }
      to { opacity: 1; transform: translateY(0); }
    }
  `]
})
export class ToastContainerComponent implements OnInit {
  toasts: ToastMessage[] = [];

  constructor(private toastService: ToastService) {}

  ngOnInit(): void {
    this.toastService.toasts$.subscribe(t => {
      this.toasts = t;
    });
  }

  removeToast(id: string): void {
    this.toastService.remove(id);
  }
}

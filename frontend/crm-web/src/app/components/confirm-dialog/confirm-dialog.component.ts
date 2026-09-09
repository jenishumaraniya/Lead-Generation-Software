import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { ConfirmDialogService, ConfirmDialogOptions } from '../../core/services/confirm-dialog.service';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './confirm-dialog.component.html',
  styleUrls: ['./confirm-dialog.component.css']
})
export class ConfirmDialogComponent implements OnInit, OnDestroy {
  state: {
    isOpen: boolean;
    options: ConfirmDialogOptions;
  } = {
    isOpen: false,
    options: { title: '', message: '' }
  };

  private sub?: Subscription;

  constructor(private confirmService: ConfirmDialogService) {}

  ngOnInit(): void {
    this.sub = this.confirmService.state$.subscribe((state) => {
      this.state = state;
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  onConfirm(): void {
    this.confirmService.close(true);
  }

  onCancel(): void {
    this.confirmService.close(false);
  }

  onBackdropClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('dialog-overlay')) {
      this.onCancel();
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.state.isOpen) {
      this.onCancel();
    }
  }
}

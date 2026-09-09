import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface ConfirmDialogOptions {
  title: string;
  message: string;
  detailNote?: string;
  confirmText?: string;
  cancelText?: string;
  type?: 'danger' | 'warning' | 'info' | 'primary';
  iconType?: 'trash' | 'alert' | 'check' | 'logout' | 'info';
}

interface DialogInternalState {
  isOpen: boolean;
  options: ConfirmDialogOptions;
  resolve?: (result: boolean) => void;
}

@Injectable({
  providedIn: 'root'
})
export class ConfirmDialogService {
  private stateSubject = new BehaviorSubject<DialogInternalState>({
    isOpen: false,
    options: { title: '', message: '' }
  });

  public state$ = this.stateSubject.asObservable();

  public confirm(options: ConfirmDialogOptions): Promise<boolean> {
    return new Promise<boolean>((resolve) => {
      this.stateSubject.next({
        isOpen: true,
        options: {
          confirmText: 'Confirm',
          cancelText: 'Cancel',
          type: 'danger',
          iconType: 'trash',
          ...options
        },
        resolve
      });
    });
  }

  public close(result: boolean): void {
    const current = this.stateSubject.value;
    if (current.resolve) {
      current.resolve(result);
    }
    this.stateSubject.next({
      isOpen: false,
      options: { title: '', message: '' }
    });
  }
}

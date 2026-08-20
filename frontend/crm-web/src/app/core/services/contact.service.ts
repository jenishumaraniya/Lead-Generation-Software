import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ContactService {
  private openFormSubject = new Subject<number | undefined>();
  openForm$ = this.openFormSubject.asObservable();

  openContactForm(productId?: number): void {
    this.openFormSubject.next(productId);
  }
}
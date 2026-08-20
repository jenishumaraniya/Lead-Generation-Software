import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-floating-contact-button',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './floating-contact-button.component.html',
  styleUrls: ['./floating-contact-button.component.css']
})
export class FloatingContactButtonComponent {
  @Output() openForm = new EventEmitter<void>();

  openContactForm(): void {
    this.openForm.emit();
  }
}
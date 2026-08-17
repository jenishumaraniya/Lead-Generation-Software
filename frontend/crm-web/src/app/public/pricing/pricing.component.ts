import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-pricing',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './pricing.component.html',
  styleUrl: './pricing.component.css'
})
export class PricingComponent {

  constructor(private router: Router) {}

  viewProducts(): void {
    this.router.navigate(['/products']);
  }

  interested(): void {
    alert(
      'Thank you for your interest. Our team will contact you soon.'
    );
  }
}
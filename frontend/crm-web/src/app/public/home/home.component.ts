import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent {

  constructor(private router: Router) {}

  // exploreProducts(): void {
  //   this.router.navigate(['/products']);
  // }
  exploreProducts(categoryId?: number): void {

  if (categoryId) {

    this.router.navigate(
      ['/products'],
      {
        queryParams: {
          categoryId: categoryId
        }
      }
    );

  } else {

    this.router.navigate(
      ['/products']
    );

  }

}

}
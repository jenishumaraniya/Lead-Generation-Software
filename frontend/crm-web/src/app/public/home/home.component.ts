import { Component, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, CommonModule],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit {

  categories: any[] = [];

  constructor(
    private router: Router,
    private apiService: ApiService
  ) {}

  ngOnInit(): void {
    this.apiService.getCategories().subscribe({
      next: (cats) => this.categories = cats,
      error: (err) => console.error('Failed to load categories on home:', err)
    });
  }

  exploreProducts(categoryId?: number): void {
    if (categoryId) {
      this.router.navigate(['/products'], { queryParams: { categoryId } });
    } else {
      this.router.navigate(['/products']);
    }
  }

  getCategoryIcon(name: string): string {
    const n = (name || '').toLowerCase();
    if (n.includes('laptop')) return '💻';
    if (n.includes('desktop')) return '🖥️';
    if (n.includes('server')) return '🗄️';
    if (n.includes('network')) return '🌐';
    if (n.includes('cloud')) return '☁️';
    return '📦';
  }
}
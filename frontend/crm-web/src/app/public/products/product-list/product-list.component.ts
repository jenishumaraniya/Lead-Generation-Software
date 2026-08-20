import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { Product } from '../../../core/models/product.model';
import { ApiService } from '../../../core/services/api.service';
import { VisitorTrackingService } from '../../../core/services/visitor-tracking.service';
import { ContactService } from '../../../core/services/contact.service';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [RouterLink, DecimalPipe],
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.css'
})
export class ProductListComponent implements OnInit {

  products: Product[] = [];
  selectedCategoryId: number | null = null;
  selectedCategoryName = 'All Products';

  categories = [
    { id: 1, name: 'Laptops' },
    { id: 2, name: 'Desktops' },
    { id: 3, name: 'Servers' },
    { id: 4, name: 'Networking' }
  ];

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private apiService: ApiService,
    private visitorTrackingService: VisitorTrackingService,
    private contactService: ContactService
  ) {}

  ngOnInit(): void {
    this.route.queryParamMap.subscribe(params => {
      const categoryId = params.get('categoryId');
      this.selectedCategoryId = categoryId !== null ? Number(categoryId) : null;

      const selectedCategory = this.categories.find(c => c.id === this.selectedCategoryId);
      this.selectedCategoryName = selectedCategory?.name ?? 'All Products';

      this.loadProducts();
    });
  }

  private loadProducts(): void {
    this.apiService.getProducts(this.selectedCategoryId ?? undefined).subscribe({
      next: (products: Product[]) => {
        // ✅ Parse features and specifications if they are JSON strings
        this.products = products.map(product => ({
          ...product,
          features: this.parseStringArray(product.features),
          specifications: this.parseStringArray(product.specifications)
        }));
      },
      error: (error) => {
        console.error('Failed to load products:', error);
        this.products = [];
      }
    });
  }

  // ✅ Helper to parse JSON strings into arrays
  private parseStringArray(value: string | string[] | null | undefined): string[] {
    if (!value) return [];
    if (Array.isArray(value)) return value;
    if (typeof value === 'string') {
      try {
        const parsed = JSON.parse(value);
        if (Array.isArray(parsed)) return parsed;
      } catch {}
      return [value];
    }
    return [];
  }

  // ✅ Open Contact Form with pre-selected product
  openContactForm(productId: number): void {
    // Track the event
    this.visitorTrackingService.trackActivity(
      'INTEREST_CLICK',
      productId,
      { source: 'product_list' }
    );
    
    // Open the contact form with pre-selected product
    this.contactService.openContactForm(productId);
  }

  selectCategory(categoryId: number): void {
    this.router.navigate(['/products'], { queryParams: { categoryId } });
  }

  showAllProducts(): void {
    this.router.navigate(['/products']);
  }

  viewProduct(productId: number): void {
    // ✅ Track product view from list
    this.visitorTrackingService.trackActivity(
      'PRODUCT_VIEW',
      productId,
      { source: 'product_list' }
    );
    this.router.navigate(['/products', productId]);
  }

  goToCompare(): void {
    this.router.navigate(['/compare']);
  }
}
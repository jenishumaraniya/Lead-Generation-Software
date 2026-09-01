import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { Product } from '../../../core/models/product.model';
import { ApiService } from '../../../core/services/api.service';
import { VisitorTrackingService } from '../../../core/services/visitor-tracking.service';
import { ContactService } from '../../../core/services/contact.service';
import { getProductImageUrl } from '../../../core/utils/product-image.util';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [RouterLink, DecimalPipe],
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.css'
})
export class ProductListComponent implements OnInit {

  products: Product[] = [];
  categories: any[] = [];
  selectedCategoryId: number | null = null;
  selectedCategoryName = 'All Products';

  getProductImage(prod: any): string {
    return getProductImageUrl(prod);
  }

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private apiService: ApiService,
    private visitorTrackingService: VisitorTrackingService,
    private contactService: ContactService
  ) {}

  ngOnInit(): void {
    // Load categories directly from database
    this.apiService.getCategories().subscribe({
      next: (cats) => {
        this.categories = cats;
        this.updateSelectedCategoryName();
      },
      error: (err) => console.error('Failed to load categories from database:', err)
    });

    this.route.queryParamMap.subscribe(params => {
      const categoryId = params.get('categoryId');
      this.selectedCategoryId = categoryId !== null ? Number(categoryId) : null;
      this.updateSelectedCategoryName();
      this.loadProducts();
    });
  }

  private updateSelectedCategoryName(): void {
    if (this.selectedCategoryId === null) {
      this.selectedCategoryName = 'All Products';
      return;
    }
    const selectedCategory = this.categories.find(c => (c.categoryId ?? c.id) === this.selectedCategoryId);
    this.selectedCategoryName = selectedCategory ? (selectedCategory.categoryName ?? selectedCategory.name) : 'All Products';
  }

  private loadProducts(): void {
    this.apiService.getProducts(this.selectedCategoryId ?? undefined).subscribe({
      next: (products: Product[]) => {
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

  openContactForm(productId: number): void {
    this.visitorTrackingService.trackActivity(
      'INTEREST_CLICK',
      productId,
      { source: 'product_list' }
    );
    this.contactService.openContactForm(productId);
  }

  selectCategory(categoryId: number): void {
    this.router.navigate(['/products'], { queryParams: { categoryId } });
  }

  showAllProducts(): void {
    this.router.navigate(['/products']);
  }

  viewProduct(productId: number): void {
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
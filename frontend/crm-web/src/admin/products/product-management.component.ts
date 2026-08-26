import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminApiService } from '../../app/core/services/admin-api.services';
import { AuthService } from '../../app/core/services/auth.service';

@Component({
  selector: 'app-product-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './product-management.component.html',
  styleUrls: ['./product-management.component.css']
})
export class ProductManagementComponent implements OnInit {
  products: any[] = [];
  categories: any[] = [];
  filteredProducts: any[] = [];
  searchTerm = '';
  selectedCategoryId: number | null = null;
  isLoading = false;

  showProductModal = false;
  isEditing = false;
  editingProductId: number | null = null;

  productForm = {
    name: '',
    description: '',
    pricing: 0,
    features: '',
    specifications: '',
    categoryId: null as number | null,
    status: 'ACTIVE'
  };

  constructor(
    private adminApi: AdminApiService,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadProducts();
    this.loadCategories();
  }

  loadProducts(): void {
    this.isLoading = true;
    this.adminApi.getProducts().subscribe({
      next: (data) => {
        this.products = data;
        this.applyFilter();
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
      }
    });
  }

  loadCategories(): void {
    this.adminApi.getCategories().subscribe({
      next: (data) => this.categories = data,
      error: () => {}
    });
  }

  applyFilter(): void {
    this.filteredProducts = this.products.filter(p => {
      const matchSearch = !this.searchTerm ||
        p.name.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        (p.description && p.description.toLowerCase().includes(this.searchTerm.toLowerCase()));

      const matchCat = this.selectedCategoryId === null || p.categoryId === this.selectedCategoryId;
      return matchSearch && matchCat;
    });
  }

  openCreateModal(): void {
    if (!this.authService.isAdmin()) return;
    this.isEditing = false;
    this.editingProductId = null;
    this.productForm = {
      name: '',
      description: '',
      pricing: 0,
      features: '',
      specifications: '',
      categoryId: this.categories.length > 0 ? this.categories[0].categoryId : null,
      status: 'ACTIVE'
    };
    this.showProductModal = true;
  }

  openEditModal(product: any): void {
    if (!this.authService.isAdmin()) return;
    this.isEditing = true;
    this.editingProductId = product.productId;
    this.productForm = {
      name: product.name,
      description: product.description || '',
      pricing: product.pricing || 0,
      features: product.features || '',
      specifications: product.specifications || '',
      categoryId: product.categoryId,
      status: product.status || 'ACTIVE'
    };
    this.showProductModal = true;
  }

  closeModal(): void {
    this.showProductModal = false;
  }

  saveProduct(): void {
    if (!this.authService.isAdmin()) return;

    if (!this.productForm.name) {
      alert('Product name is required.');
      return;
    }

    if (this.isEditing && this.editingProductId) {
      this.adminApi.updateProduct(this.editingProductId, this.productForm).subscribe({
        next: () => {
          this.closeModal();
          this.loadProducts();
        },
        error: (err) => alert(err.error?.error || 'Failed to update product')
      });
    } else {
      this.adminApi.createProduct(this.productForm).subscribe({
        next: () => {
          this.closeModal();
          this.loadProducts();
        },
        error: (err) => alert(err.error?.error || 'Failed to create product')
      });
    }
  }

  deleteProduct(product: any): void {
    if (!this.authService.isAdmin()) return;

    if (confirm(`Are you sure you want to delete product "${product.name}"?`)) {
      this.adminApi.deleteProduct(product.productId).subscribe({
        next: () => this.loadProducts(),
        error: (err) => alert(err.error?.error || 'Failed to delete product')
      });
    }
  }
}

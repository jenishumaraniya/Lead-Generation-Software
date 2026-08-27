import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../../../../core/services/product.service';
import { CategoryService, Category } from '../../../../../core/services/category.service';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './product-list.component.html',
  styleUrls: ['./product-list.component.css']
})
export class ProductListComponent implements OnInit {
  products: any[] = [];
  categories: Category[] = [];
  showModal = false;
  isEdit = false;
  formData: any = { name: '', pricing: 0, description: '', categoryId: null, status: 'ACTIVE' };
  editingId: number | null = null;
  loading = false;

  constructor(
    private productService: ProductService,
    private categoryService: CategoryService
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    this.productService.getProducts().subscribe({
      next: (data) => {
        this.products = data || [];
        this.loading = false;
      },
      error: () => {
        this.products = [];
        this.loading = false;
      }
    });

    this.categoryService.getCategories().subscribe({
      next: (cats) => this.categories = cats || [],
      error: () => this.categories = []
    });
  }

  openModal(): void {
    this.showModal = true;
    this.isEdit = false;
    this.editingId = null;
    this.formData = { 
      name: '', 
      pricing: 0, 
      description: '', 
      categoryId: this.categories.length > 0 ? this.categories[0].categoryId : null,
      status: 'ACTIVE' 
    };
  }

  editProduct(p: any): void {
    this.isEdit = true;
    this.editingId = p.productId;
    this.formData = { 
      name: p.name,
      pricing: p.pricing,
      description: p.description || '',
      categoryId: p.categoryId,
      status: p.status || 'ACTIVE'
    };
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
  }

  saveProduct(): void {
    if (!this.formData.name.trim()) return;
    this.loading = true;

    const payload = {
      name: this.formData.name.trim(),
      description: this.formData.description,
      pricing: Number(this.formData.pricing),
      categoryId: this.formData.categoryId ? Number(this.formData.categoryId) : null,
      status: this.formData.status
    };

    const obs = this.isEdit
      ? this.productService.updateProduct(this.editingId!, payload)
      : this.productService.createProduct(payload);

    obs.subscribe({
      next: () => {
        this.loadData();
        this.closeModal();
      },
      error: () => {
        alert('Failed to save product');
        this.loading = false;
      }
    });
  }

  deleteProduct(id: number): void {
    if (confirm('Are you sure you want to delete this product?')) {
      this.loading = true;
      this.productService.deleteProduct(id).subscribe({
        next: () => this.loadData(),
        error: () => {
          alert('Delete failed');
          this.loading = false;
        }
      });
    }
  }

  getCategoryName(catId?: number): string {
    if (!catId) return 'Uncategorized';
    const cat = this.categories.find(c => c.categoryId === catId);
    return cat ? cat.categoryName : 'Uncategorized';
  }
}
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../../../../core/services/product.service';
import { CategoryService, Category } from '../../../../../core/services/category.service';
import { PaginationComponent } from '../../../../../components/pagination/pagination.component';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, FormsModule, PaginationComponent],
  templateUrl: './product-list.component.html',
  styleUrls: ['./product-list.component.css']
})
export class ProductListComponent implements OnInit {
  products: any[] = [];
  filteredProducts: any[] = [];
  categories: Category[] = [];
  showModal = false;
  isEdit = false;
  formData: any = { name: '', pricing: 0, description: '', categoryId: null, status: 'ACTIVE' };
  editingId: number | null = null;
  loading = false;

  // View & Filters
  viewMode: 'cards' | 'table' = 'cards';
  searchTerm: string = '';
  selectedCategory: string = 'ALL';
  selectedStatus: string = 'ALL';

  currentPage = 1;
  pageSize = 10;

  get paginatedProducts(): any[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredProducts.slice(start, start + this.pageSize);
  }

  stats = {
    total: 0,
    active: 0,
    categoriesCount: 0,
    avgPrice: 0
  };

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
        this.calculateStats();
        this.applyFilter();
        this.loading = false;
      },
      error: () => {
        this.products = [];
        this.filteredProducts = [];
        this.calculateStats();
        this.loading = false;
      }
    });

    this.categoryService.getCategories().subscribe({
      next: (cats) => {
        this.categories = cats || [];
        this.calculateStats();
      },
      error: () => this.categories = []
    });
  }

  calculateStats(): void {
    this.stats.total = this.products.length;
    this.stats.active = this.products.filter(p => !p.status || p.status.toUpperCase() === 'ACTIVE').length;
    this.stats.categoriesCount = this.categories.length;
    const totalPricing = this.products.reduce((acc, p) => acc + (Number(p.pricing) || 0), 0);
    this.stats.avgPrice = this.products.length ? Math.round(totalPricing / this.products.length) : 0;
  }

  setViewMode(mode: 'cards' | 'table'): void {
    this.viewMode = mode;
  }

  applyFilter(): void {
    this.currentPage = 1;
    const term = this.searchTerm.trim().toLowerCase();

    this.filteredProducts = this.products.filter(p => {
      // Category filter
      if (this.selectedCategory !== 'ALL') {
        if (this.selectedCategory === 'UNCATEGORIZED') {
          if (p.categoryId) return false;
        } else {
          if (p.categoryId !== Number(this.selectedCategory)) return false;
        }
      }

      // Status filter
      if (this.selectedStatus !== 'ALL') {
        const pStatus = (p.status || 'ACTIVE').toUpperCase();
        if (pStatus !== this.selectedStatus) return false;
      }

      // Search term
      if (!term) return true;

      const nameMatch = p.name?.toLowerCase().includes(term);
      const descMatch = p.description?.toLowerCase().includes(term);
      const catMatch = this.getCategoryName(p.categoryId).toLowerCase().includes(term);

      return nameMatch || descMatch || catMatch;
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
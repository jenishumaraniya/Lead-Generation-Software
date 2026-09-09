import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../../../../core/services/product.service';
import { CategoryService, Category } from '../../../../../core/services/category.service';
import { PaginationComponent } from '../../../../../components/pagination/pagination.component';
import { getProductImageUrl } from '../../../../../core/utils/product-image.util';
import { ConfirmDialogService } from '../../../../../core/services/confirm-dialog.service';

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
  formData: any = { name: '', pricing: 0, description: '', categoryId: null, status: 'DRAFT', imageUrl: '' };
  editingId: number | null = null;
  loading = false;
  isUploadingImage = false;
  imageUploadError = '';

  // View & Filters
  viewMode: 'cards' | 'table' = 'cards';
  searchTerm: string = '';
  selectedCategory: string = 'ALL';
  selectedStatus: string = 'ALL';

  sortColumn: string = 'name';
  sortDirection: 'asc' | 'desc' = 'asc';

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
    private categoryService: CategoryService,
    private confirmService: ConfirmDialogService
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  getProductImage(p: any): string {
    return getProductImageUrl(p);
  }

  getImagePreviewUrl(url?: string): string {
    if (!url) return '';
    return getProductImageUrl({ imageUrl: url });
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

    let list = this.products.filter(p => {
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

    if (this.sortColumn) {
      list.sort((a: any, b: any) => {
        let valA = a[this.sortColumn];
        let valB = b[this.sortColumn];

        if (this.sortColumn === 'category') {
          valA = this.getCategoryName(a.categoryId);
          valB = this.getCategoryName(b.categoryId);
        }

        if (valA == null) valA = '';
        if (valB == null) valB = '';

        if (typeof valA === 'string') valA = valA.toLowerCase();
        if (typeof valB === 'string') valB = valB.toLowerCase();

        let comparison = 0;
        if (valA > valB) comparison = 1;
        else if (valA < valB) comparison = -1;

        return this.sortDirection === 'asc' ? comparison : -comparison;
      });
    }

    this.filteredProducts = list;
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.applyFilter();
  }

  toggleSort(column: string): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
    this.applyFilter();
  }

  openModal(): void {
    this.showModal = true;
    this.isEdit = false;
    this.editingId = null;
    this.imageUploadError = '';
    this.formData = { 
      name: '', 
      pricing: null, 
      description: '', 
      categoryId: this.categories.length > 0 ? this.categories[0].categoryId : null,
      status: 'DRAFT',
      imageUrl: ''
    };
  }

  async activateProduct(p: any): Promise<void> {
    const confirmed = await this.confirmService.confirm({
      title: 'Publish Product',
      message: `Are you sure you want to publish "${p.name}" to ACTIVE status? It will immediately become live in the public catalog.`,
      confirmText: 'Publish Live',
      cancelText: 'Keep Draft',
      type: 'primary',
      iconType: 'check'
    });

    if (!confirmed) {
      return;
    }
    this.loading = true;
    this.productService.updateProductStatus(p.productId, 'ACTIVE').subscribe({
      next: () => {
        this.loadData();
      },
      error: () => {
        // Fallback for full update
        const payload = {
          name: p.name,
          description: p.description,
          pricing: p.pricing,
          categoryId: p.categoryId,
          status: 'ACTIVE',
          imageUrl: p.imageUrl
        };
        this.productService.updateProduct(p.productId, payload).subscribe({
          next: () => this.loadData(),
          error: (err) => {
            alert(err.error?.error || 'Failed to publish product.');
            this.loading = false;
          }
        });
      }
    });
  }

  editProduct(p: any): void {
    this.isEdit = true;
    this.editingId = p.productId;
    this.imageUploadError = '';
    this.formData = { 
      name: p.name,
      pricing: p.pricing,
      description: p.description || '',
      categoryId: p.categoryId,
      status: p.status || 'ACTIVE',
      imageUrl: p.imageUrl || ''
    };
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.imageUploadError = '';
  }

  onImageFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;
    const file = input.files[0];

    // 1. Instant local preview using FileReader
    const reader = new FileReader();
    reader.onload = (e: any) => {
      if (e.target?.result) {
        this.formData.imageUrl = e.target.result;
      }
    };
    reader.readAsDataURL(file);

    this.isUploadingImage = true;
    this.imageUploadError = '';

    // 2. Upload file to server
    this.productService.uploadProductImage(file).subscribe({
      next: (res) => {
        this.isUploadingImage = false;
        if (res?.imageUrl) {
          this.formData.imageUrl = res.imageUrl;
        }
        input.value = '';
      },
      error: (err) => {
        this.isUploadingImage = false;
        // If server upload fails (e.g. backend restart pending), keep the base64 preview so saving still works!
        this.imageUploadError = err.error?.error || 'Server upload notice: Using local image encoding.';
        input.value = '';
      }
    });
  }

  clearImage(): void {
    this.formData.imageUrl = '';
    this.imageUploadError = '';
  }

  saveProduct(): void {
    if (!this.formData.name || !this.formData.name.trim()) {
      alert('Product name is required.');
      return;
    }

    const price = Number(this.formData.pricing);
    if (this.formData.pricing === null || this.formData.pricing === undefined || this.formData.pricing === '' || isNaN(price) || price <= 0) {
      alert('Product price must be greater than zero (cannot be 0 or negative).');
      return;
    }

    this.loading = true;

    const payload = {
      name: this.formData.name.trim(),
      description: this.formData.description,
      pricing: price,
      categoryId: this.formData.categoryId ? Number(this.formData.categoryId) : null,
      status: this.formData.status || (this.isEdit ? 'ACTIVE' : 'DRAFT'),
      imageUrl: this.formData.imageUrl ? this.formData.imageUrl.trim() : null
    };

    const obs = this.isEdit
      ? this.productService.updateProduct(this.editingId!, payload)
      : this.productService.createProduct(payload);

    obs.subscribe({
      next: () => {
        this.loadData();
        this.closeModal();
      },
      error: (err) => {
        const errorMsg = err.error?.error || 'Failed to save product';
        alert(errorMsg);
        this.loading = false;
      }
    });
  }

  async deleteProduct(id: number): Promise<void> {
    const prod = this.products.find(p => p.productId === id);
    const confirmed = await this.confirmService.confirm({
      title: 'Delete Product',
      message: `Are you sure you want to permanently delete "${prod?.name || 'this product'}"? This action cannot be undone.`,
      confirmText: 'Delete Product',
      cancelText: 'Cancel',
      type: 'danger',
      iconType: 'trash'
    });

    if (confirmed) {
      this.loading = true;
      this.productService.deleteProduct(id).subscribe({
        next: () => this.loadData(),
        error: () => {
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
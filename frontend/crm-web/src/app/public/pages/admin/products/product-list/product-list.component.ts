import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../../../../core/services/product.service';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './product-list.component.html',
  styleUrls: ['./product-list.component.css']
})
export class ProductListComponent implements OnInit {
  products: any[] = [];
  showModal = false;
  isEdit = false;
  formData: any = { name: '', pricing: 0, description: '', status: 'ACTIVE' };
  editingId: number | null = null;

  constructor(private productService: ProductService) {}

  ngOnInit() { this.loadProducts(); }

  loadProducts() {
    this.productService.getProducts().subscribe({
      next: (data) => this.products = data,
      error: () => this.products = []
    });
  }

  openModal() { this.showModal = true; this.isEdit = false; this.formData = { name: '', pricing: 0, description: '', status: 'ACTIVE' }; }

  editProduct(p: any) {
    this.isEdit = true;
    this.editingId = p.productId;
    this.formData = { ...p };
    this.showModal = true;
  }

  closeModal() { this.showModal = false; }

  saveProduct() {
    if (this.isEdit) {
      this.productService.updateProduct(this.editingId!, this.formData).subscribe({
        next: () => { this.loadProducts(); this.closeModal(); },
        error: () => alert('Update failed')
      });
    } else {
      this.productService.createProduct(this.formData).subscribe({
        next: () => { this.loadProducts(); this.closeModal(); },
        error: () => alert('Create failed')
      });
    }
  }

  deleteProduct(id: number) {
    if (confirm('Delete this product?')) {
      this.productService.deleteProduct(id).subscribe({
        next: () => this.loadProducts(),
        error: () => alert('Delete failed')
      });
    }
  }
}
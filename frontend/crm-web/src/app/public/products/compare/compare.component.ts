import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { Product } from '../../../core/models/product.model';
import { ContactService } from '../../../core/services/contact.service';
import { getProductImageUrl } from '../../../core/utils/product-image.util';

@Component({
  selector: 'app-compare',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './compare.component.html',
  styleUrl: './compare.component.css'
})
export class CompareComponent implements OnInit {
  allProducts: Product[] = [];
  selectedProduct1: Product | null = null;
  selectedProduct2: Product | null = null;
  selectedProduct3: Product | null = null;
  
  selectedId1: number | null = null;
  selectedId2: number | null = null;
  selectedId3: number | null = null;

  isLoading = true;

  private fallbackProducts: Product[] = [
    {
      productId: 1,
      name: 'Business Laptop Pro',
      description: 'High-performance laptop designed for modern business professionals.',
      pricing: 65000,
      features: ['16GB RAM', '512GB SSD', 'WiFi 6', 'Full HD Display'],
      specifications: ['Intel Core i5', '16GB RAM', '512GB SSD', '14 inch Display'],
      status: 'Available',
      categoryId: 1
    },
    {
      productId: 2,
      name: 'Business Desktop Pro',
      description: 'Powerful desktop solution for office productivity and business applications.',
      pricing: 55000,
      features: ['16GB RAM', '512GB SSD', 'High Performance', 'Multiple Ports'],
      specifications: ['Intel Core i5', '16GB RAM', '512GB SSD', 'Windows 11 Pro'],
      status: 'Available',
      categoryId: 2
    },
    {
      productId: 3,
      name: 'Business Server X1',
      description: 'Reliable server infrastructure for growing businesses.',
      pricing: 150000,
      features: ['64GB RAM', '2TB Storage', 'High Availability', 'Enterprise Security'],
      specifications: ['Intel Xeon', '64GB RAM', '2TB SSD', 'Rack Mount'],
      status: 'Available',
      categoryId: 3
    },
    {
      productId: 4,
      name: 'Enterprise Network Router',
      description: 'Secure and reliable networking solution for business environments.',
      pricing: 35000,
      features: ['High Speed', 'Enterprise Security', 'VPN Support', 'Multiple Ports'],
      specifications: ['1Gbps Speed', '8 Ethernet Ports', 'VPN Support', 'Firewall'],
      status: 'Available',
      categoryId: 4
    }
  ];

  constructor(
    private apiService: ApiService,
    private route: ActivatedRoute,
    private contactService: ContactService
  ) {}

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.isLoading = true;
    this.apiService.getProducts().subscribe({
      next: (products) => {
        this.allProducts = products && products.length ? products : this.fallbackProducts;
        this.isLoading = false;
        this.initializeSelections();
      },
      error: () => {
        this.allProducts = this.fallbackProducts;
        this.isLoading = false;
        this.initializeSelections();
      }
    });
  }

  private initializeSelections(): void {
    this.route.queryParams.subscribe(params => {
      const ids = params['ids'] ? params['ids'].split(',').map((id: string) => parseInt(id, 10)) : [];
      
      if (ids.length >= 1 && this.allProducts.length > 0) {
        this.selectedId1 = ids[0];
      } else if (this.allProducts.length >= 1) {
        this.selectedId1 = this.allProducts[0].productId;
      }
      this.onProduct1Change();

      if (ids.length >= 2 && this.allProducts.length > 1) {
        this.selectedId2 = ids[1];
      } else if (this.allProducts.length >= 2) {
        this.selectedId2 = this.allProducts[1].productId;
      }
      this.onProduct2Change();

      if (ids.length >= 3 && this.allProducts.length > 2) {
        this.selectedId3 = ids[2];
        this.onProduct3Change();
      }
    });
  }

  onProduct1Change(newId?: any): void {
    if (newId !== undefined) {
      this.selectedId1 = newId !== null ? Number(newId) : null;
    }
    this.selectedProduct1 = this.allProducts.find(p => Number(p.productId) === Number(this.selectedId1)) || null;
  }

  onProduct2Change(newId?: any): void {
    if (newId !== undefined) {
      this.selectedId2 = newId !== null ? Number(newId) : null;
    }
    this.selectedProduct2 = this.allProducts.find(p => Number(p.productId) === Number(this.selectedId2)) || null;
  }

  onProduct3Change(newId?: any): void {
    if (newId !== undefined) {
      this.selectedId3 = newId !== null ? Number(newId) : null;
    }
    this.selectedProduct3 = this.allProducts.find(p => Number(p.productId) === Number(this.selectedId3)) || null;
  }

  openContact(productId?: number): void {
    if (productId) {
      this.contactService.openContactForm(productId);
    }
  }

  getProductImage(prod?: any): string {
    return getProductImageUrl(prod);
  }

  getCategoryIcon(catId?: number): string {
    if (catId === 1) return '💻';
    if (catId === 2) return '🖥️';
    if (catId === 3) return '🗄️';
    if (catId === 4) return '🌐';
    return '📦';
  }
}
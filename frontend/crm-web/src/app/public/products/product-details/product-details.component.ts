import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { Product } from '../../../core/models/product.model';
import { ApiService } from '../../../core/services/api.service';
import { VisitorTrackingService } from '../../../core/services/visitor-tracking.service';
import { ContactService } from '../../../core/services/contact.service';

@Component({
  selector: 'app-product-details',
  standalone: true,
  imports: [RouterLink, DecimalPipe],
  templateUrl: './product-details.component.html',
  styleUrl: './product-details.component.css'
})
export class ProductDetailsComponent implements OnInit {

  product?: Product;

  private fallbackProducts: Product[] = [
    {
      productId: 1,
      name: 'Business Laptop Pro',
      description:
        'High-performance laptop designed for modern business professionals.',
      pricing: 65000,
      features: [
        '16GB RAM',
        '512GB SSD',
        'WiFi 6',
        'Full HD Display'
      ],
      specifications: [
        'Intel Core i5',
        '16GB RAM',
        '512GB SSD',
        '14 inch Display'
      ],
      status: 'Available'
    },
    {
      productId: 2,
      name: 'Business Desktop Pro',
      description:
        'Powerful desktop solution for office productivity and business applications.',
      pricing: 55000,
      features: [
        '16GB RAM',
        '512GB SSD',
        'High Performance',
        'Multiple Ports'
      ],
      specifications: [
        'Intel Core i5',
        '16GB RAM',
        '512GB SSD',
        'Windows 11 Pro'
      ],
      status: 'Available'
    },
    {
      productId: 3,
      name: 'Business Server X1',
      description:
        'Reliable server infrastructure for growing businesses.',
      pricing: 150000,
      features: [
        '64GB RAM',
        '2TB Storage',
        'High Availability',
        'Enterprise Security'
      ],
      specifications: [
        'Intel Xeon',
        '64GB RAM',
        '2TB SSD',
        'Rack Mount'
      ],
      status: 'Available'
    },
    {
      productId: 4,
      name: 'Enterprise Network Router',
      description:
        'Secure and reliable networking solution for business environments.',
      pricing: 35000,
      features: [
        'High Speed',
        'Enterprise Security',
        'VPN Support',
        'Multiple Ports'
      ],
      specifications: [
        '1Gbps Speed',
        '8 Ethernet Ports',
        'VPN Support',
        'Firewall'
      ],
      status: 'Available'
    }
  ];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private apiService: ApiService,
    private visitorTrackingService: VisitorTrackingService ,
     private contactService: ContactService  
  ) {}

  // ngOnInit(): void {
  //   const id = Number(this.route.snapshot.paramMap.get('id'));

  //   if (!id) {
  //     this.product = this.fallbackProducts[0];
  //     return;
  //   }

  //   this.apiService.getProduct(id).subscribe({
  //     next: (product) => {
  //       this.product = product;
  //     },
  //     error: () => {
  //       this.product = this.fallbackProducts.find(
  //         item => item.productId === id
  //       ) ?? this.fallbackProducts[0];
  //     }
  //   });

  //    if (this.product) {
  //   this.visitorTrackingService.trackActivity(
  //     'PRODUCT_VIEW',
  //     this.product.productId,
  //     { productName: this.product.name }
  //   );
  // }
  // }
  ngOnInit(): void {
  const id = Number(this.route.snapshot.paramMap.get('id'));

  const handleProduct = (product: Product) => {
    this.product = product;
    this.trackProductView(product);
  };

  if (!id) {
    handleProduct(this.fallbackProducts[0]);
    return;
  }

  this.apiService.getProduct(id).subscribe({
    next: (product) => handleProduct(product),
    error: () => {
      const fallback = this.fallbackProducts.find(p => p.productId === id) ?? this.fallbackProducts[0];
      handleProduct(fallback);
    }
  });
}

private trackProductView(product: Product): void {
  this.visitorTrackingService.trackActivity(
    'PRODUCT_VIEW',
    product.productId,
    { productName: product.name }
  );
}

  goBack(): void {
    this.router.navigate(['/products']);
  }

  // compare(): void {
  //   this.router.navigate(['/compare']);
  // }
compare(): void {
  if (this.product) {
    this.visitorTrackingService.trackActivity(
      'PRODUCT_COMPARE',
      this.product.productId,
      { source: 'product_details' }
    );
  }
  this.router.navigate(['/compare']);
}
  // interested(): void {
  //   alert(
  //     'Thank you for your interest. Our team will contact you soon.'
  //   );
  // }

  
  interested(): void {
  // Track the event
  if (this.product) {
    this.visitorTrackingService.trackActivity(
      'INTEREST_CLICK',
      this.product.productId,
      { source: 'product_page' }
    );
    // Open the contact form with pre-selected product
    this.contactService.openContactForm(this.product.productId);
  }
}

}
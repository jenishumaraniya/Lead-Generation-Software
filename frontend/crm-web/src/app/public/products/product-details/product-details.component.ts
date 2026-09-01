import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { Product } from '../../../core/models/product.model';
import { ApiService } from '../../../core/services/api.service';
import { VisitorTrackingService } from '../../../core/services/visitor-tracking.service';
import { ContactService } from '../../../core/services/contact.service';
import { getProductImageUrl } from '../../../core/utils/product-image.util';

@Component({
  selector: 'app-product-details',
  standalone: true,
  imports: [RouterLink, DecimalPipe],
  templateUrl: './product-details.component.html',
  styleUrl: './product-details.component.css'
})
export class ProductDetailsComponent implements OnInit {

  product?: Product;

  getProductImage(prod?: any): string {
    return getProductImageUrl(prod || this.product);
  }

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
    this.route.paramMap.subscribe(params => {
      const id = Number(params.get('id'));

      const handleProduct = (product: Product) => {
        this.product = {
          ...product,
          features: product.features && product.features.length > 0 ? product.features : this.getDefaultFeatures(product),
          specifications: product.specifications && product.specifications.length > 0 ? product.specifications : this.getDefaultSpecs(product)
        };
        this.trackProductView(this.product);
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
    });
  }

  private getDefaultFeatures(p: Product): string[] {
    const name = (p.name || '').toLowerCase();
    if (name.includes('laptop')) return ['High-Speed DDR5 RAM', 'PCIe Gen4 NVMe SSD', 'WiFi 6E & Bluetooth 5.3', 'FHD IPS Anti-Glare Display'];
    if (name.includes('server')) return ['Dual Intel Xeon / AMD EPYC', 'ECC Registered Memory', 'Hot-Swappable Redundant PSU', 'iDRAC / IPMI Remote Management'];
    if (name.includes('desktop')) return ['Intel Core 13th Gen / Ryzen 7', 'Ultra-Quiet Thermal Cooling', 'Dual DisplayPort & HDMI', 'Enterprise Windows 11 Pro'];
    if (name.includes('router') || name.includes('network')) return ['10Gbps SFP+ Uplinks', 'Hardware NAT & IPsec VPN', 'Enterprise Layer 3 Routing', 'Redundant Power Inputs'];
    if (name.includes('cloud') || name.includes('cluster')) return ['99.999% SLA Uptime', 'Automated Failover & Scaling', 'End-to-End Encryption', 'Dedicated VPC Peering'];
    if (name.includes('security') || name.includes('threat')) return ['AI Threat Intelligence', 'Zero-Trust MFA & SAML 2.0', 'Continuous Endpoint Telemetry', 'Automated Incident Response'];
    return ['Enterprise SLA Support', '3-Year Standard Hardware Warranty', 'ISO 27001 Certified', '24/7 Priority Support'];
  }

  private getDefaultSpecs(p: Product): string[] {
    const name = (p.name || '').toLowerCase();
    if (name.includes('laptop')) return ['Processor: High Performance Multi-Core', 'Display: 15.6" IPS 100% sRGB', 'Battery: 8+ Hours Fast-Charging', 'Chassis: Aluminum Alloy Unibody'];
    if (name.includes('server')) return ['Form Factor: 1U/2U Rackmount', 'Max RAM: Up to 1TB DDR5 ECC', 'Storage: 8x 2.5" / 3.5" Hot-Swap Bays', 'Network: 4x 10GbE SFP+'];
    if (name.includes('desktop')) return ['Form Factor: Small Form Factor / Tower', 'Power: 500W 80+ Platinum', 'Ports: 8x USB 3.2, 2x USB-C', 'OS: Windows 11 Pro 64-bit'];
    return ['Architecture: Enterprise Grade 64-bit', 'Standard: ISO 9001 / 27001', 'Warranty: 3 Years Onsite Replacement', 'Compliance: RoHS & Energy Star'];
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

  compare(): void {
    if (this.product) {
      this.visitorTrackingService.trackActivity(
        'PRODUCT_COMPARE',
        this.product.productId,
        { source: 'product_details' }
      );
      this.router.navigate(['/products/compare'], { queryParams: { ids: this.product.productId } });
    } else {
      this.router.navigate(['/products/compare']);
    }
  }

  interested(): void {
    if (this.product) {
      this.visitorTrackingService.trackActivity(
        'INTEREST_CLICK',
        this.product.productId,
        { source: 'product_page' }
      );
      this.contactService.openContactForm(this.product.productId);
    }
  }
}
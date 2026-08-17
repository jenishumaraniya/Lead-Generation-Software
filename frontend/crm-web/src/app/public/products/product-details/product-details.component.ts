import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { Product } from '../../../core/models/product.model';

@Component({
  selector: 'app-product-details',
  standalone: true,
  imports: [RouterLink, DecimalPipe],
  templateUrl: './product-details.component.html',
  styleUrl: './product-details.component.css'
})
export class ProductDetailsComponent implements OnInit {

  product?: Product;

  private products: Product[] = [

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
    private router: Router
  ) {}

  ngOnInit(): void {

    const id = Number(
      this.route.snapshot.paramMap.get('id')
    );

    this.product = this.products.find(
      product => product.productId === id
    );

  }

  goBack(): void {
    this.router.navigate(['/products']);
  }

  compare(): void {
    this.router.navigate(['/compare']);
  }

  interested(): void {

    alert(
      'Thank you for your interest. Our team will contact you soon.'
    );

  }

}
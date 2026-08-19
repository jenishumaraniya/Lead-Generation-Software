// import { Component, OnInit } from '@angular/core';
// import { Router, RouterLink } from '@angular/router';
// import { DecimalPipe } from '@angular/common';
// import { Product } from '../../../core/models/product.model';
// import { ApiService } from '../../../core/services/api.service';

// @Component({
//   selector: 'app-product-list',
//   standalone: true,
//   imports: [RouterLink, DecimalPipe],
//   templateUrl: './product-list.component.html',
//   styleUrl: './product-list.component.css'
// })
// export class ProductListComponent implements OnInit {

//   products: Product[] = [];

//   constructor(
//     private router: Router,
//     private apiService: ApiService
//   ) {}

//   ngOnInit(): void {
//     this.loadProducts();
//   }

//   private loadProducts(): void {
//     this.apiService.getProducts().subscribe({
//       next: (products) => {
//         this.products = products;
//       },
//       error: () => {
//         this.products = this.getFallbackProducts();
//       }
//     });
//   }

//   private getFallbackProducts(): Product[] {
//     return [
//       {
//         productId: 1,
//         name: 'Business Laptop Pro',
//         description:
//           'High-performance laptop designed for modern business professionals.',
//         pricing: 65000,
//         features: [
//           '16GB RAM',
//           '512GB SSD',
//           'WiFi 6',
//           'Full HD Display'
//         ],
//         specifications: [
//           'Intel Core i5',
//           '16GB RAM',
//           '512GB SSD',
//           '14 inch Display'
//         ],
//         status: 'Available'
//       },
//       {
//         productId: 2,
//         name: 'Business Desktop Pro',
//         description:
//           'Powerful desktop solution for office productivity and business applications.',
//         pricing: 55000,
//         features: [
//           '16GB RAM',
//           '512GB SSD',
//           'High Performance',
//           'Multiple Ports'
//         ],
//         specifications: [
//           'Intel Core i5',
//           '16GB RAM',
//           '512GB SSD',
//           'Windows 11 Pro'
//         ],
//         status: 'Available'
//       },
//       {
//         productId: 3,
//         name: 'Business Server X1',
//         description:
//           'Reliable server infrastructure for growing businesses.',
//         pricing: 150000,
//         features: [
//           '64GB RAM',
//           '2TB Storage',
//           'High Availability',
//           'Enterprise Security'
//         ],
//         specifications: [
//           'Intel Xeon',
//           '64GB RAM',
//           '2TB SSD',
//           'Rack Mount'
//         ],
//         status: 'Available'
//       },
//       {
//         productId: 4,
//         name: 'Enterprise Network Router',
//         description:
//           'Secure and reliable networking solution for business environments.',
//         pricing: 35000,
//         features: [
//           'High Speed',
//           'Enterprise Security',
//           'VPN Support',
//           'Multiple Ports'
//         ],
//         specifications: [
//           '1Gbps Speed',
//           '8 Ethernet Ports',
//           'VPN Support',
//           'Firewall'
//         ],
//         status: 'Available'
//       }
//     ];
//   }

//   viewProduct(productId: number): void {
//     this.router.navigate(['/products', productId]);
//   }

//   goToCompare(): void {
//     this.router.navigate(['/compare']);
//   }
// }

import { Component, OnInit } from '@angular/core';
import {
  ActivatedRoute,
  Router,
  RouterLink
} from '@angular/router';

import { DecimalPipe } from '@angular/common';

import { Product } from '../../../core/models/product.model';
import { ApiService } from '../../../core/services/api.service';


@Component({
  selector: 'app-product-list',

  standalone: true,

  imports: [
    RouterLink,
    DecimalPipe
  ],

  templateUrl: './product-list.component.html',

  styleUrl: './product-list.component.css'
})
export class ProductListComponent implements OnInit {

  // ==================================================
  // PRODUCTS
  // ==================================================

  products: Product[] = [];


  // ==================================================
  // SELECTED CATEGORY
  // ==================================================

  selectedCategoryId: number | null = null;

  selectedCategoryName = 'All Products';


  // ==================================================
  // CATEGORIES
  // ==================================================
  //
  // IMPORTANT:
  // These IDs MUST match your database CategoryId.
  //
  // Currently assuming:
  //
  // 1 = Laptop
  // 2 = Desktop
  // 3 = Server
  // 4 = Networking
  //
  // If your DB uses different IDs, change them here.
  // ==================================================

  categories = [
    {
      id: 1,
      name: 'Laptops'
    },
    {
      id: 2,
      name: 'Desktops'
    },
    {
      id: 3,
      name: 'Servers'
    },
    {
      id: 4,
      name: 'Networking'
    }
  ];


  // ==================================================
  // CONSTRUCTOR
  // ==================================================

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private apiService: ApiService
  ) {}


  // ==================================================
  // INITIALIZE COMPONENT
  // ==================================================

  ngOnInit(): void {

    this.route.queryParamMap.subscribe(params => {

      // Get categoryId from URL
      //
      // Example:
      // /products?categoryId=1
      //
      // categoryId = "1"

      const categoryId = params.get('categoryId');


      // Convert categoryId from string to number

      this.selectedCategoryId =
        categoryId !== null
          ? Number(categoryId)
          : null;


      // Find category name

      const selectedCategory =
        this.categories.find(
          category =>
            category.id === this.selectedCategoryId
        );


      this.selectedCategoryName =
        selectedCategory?.name ?? 'All Products';


      // Load products according to category

      this.loadProducts();

    });

  }


  // ==================================================
  // LOAD PRODUCTS
  // ==================================================

  private loadProducts(): void {

    this.apiService
      .getProducts(
        this.selectedCategoryId ?? undefined
      )
      .subscribe({

        next: (products: Product[]) => {

          this.products = products;

        },

        error: (error) => {

          console.error(
            'Failed to load products:',
            error
          );

          this.products = [];

        }

      });

  }


  // ==================================================
  // SELECT CATEGORY
  // ==================================================

  selectCategory(categoryId: number): void {

    this.router.navigate(
      ['/products'],
      {
        queryParams: {
          categoryId: categoryId
        }
      }
    );

  }


  // ==================================================
  // SHOW ALL PRODUCTS
  // ==================================================

  showAllProducts(): void {

    this.router.navigate(
      ['/products']
    );

  }


  // ==================================================
  // VIEW PRODUCT DETAILS
  // ==================================================

  viewProduct(productId: number): void {

    this.router.navigate(
      ['/products', productId]
    );

  }


  // ==================================================
  // COMPARE PRODUCTS
  // ==================================================

  goToCompare(): void {

    this.router.navigate(
      ['/compare']
    );

  }

}
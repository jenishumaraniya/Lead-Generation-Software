import { Component, EventEmitter, Input, Output, OnInit, ElementRef, ViewChild, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { Product } from '../../core/models/product.model';

export interface ContactFormData {
  companyName: string;
  fullName: string;
  email: string;
  jobTitle: string;
  domain: string;
  industry: string;
  country: string;
  phone: string;
  products: number[];
  quantity: number | null;
  timeline: string;
  businessRequirement: string;
}

@Component({
  selector: 'app-contact-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './contact-form.component.html',
  styleUrls: ['./contact-form.component.css']
})
export class ContactFormComponent implements OnInit {
  @Input() isOpen = false;
  @Input() preselectedProductId?: number;
  @Output() close = new EventEmitter<void>();
  @Output() submit = new EventEmitter<ContactFormData>();

  @ViewChild('dropdownContainer') dropdownContainer!: ElementRef;

  availableProducts: Product[] = [];
  isLoading = false;
  isDropdownOpen = false;

  formData: ContactFormData = {
    companyName: '',
    fullName: '',
    email: '',
    jobTitle: '',
    domain: '',
    industry: '',
    country: '',
    phone: '',
    products: [],
    quantity: null,
    timeline: '',
    businessRequirement: ''
  };

  timelineOptions = [
    'Immediately',
    'Within 1 Week',
    'This Month',
    'Next 3 Months',
    'Just Researching'
  ];

  selectedProductNames: string = 'Select products...';

  // Validation errors
  formErrors: any = {};

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.loadProducts();
  }

  private loadProducts(): void {
    this.apiService.getProducts().subscribe({
      next: (products) => {
        this.availableProducts = products;
        if (this.preselectedProductId) {
          this.formData.products = [this.preselectedProductId];
          this.updateSelectedProductNames();
        }
      },
      error: () => {
        this.availableProducts = [];
      }
    });
  }

  // =============================================
  // DROPDOWN METHODS
  // =============================================

  toggleDropdown(event: Event): void {
    event.stopPropagation();
    this.isDropdownOpen = !this.isDropdownOpen;
  }

  openDropdown(): void {
    this.isDropdownOpen = true;
  }

  closeDropdown(): void {
    this.isDropdownOpen = false;
  }

  // =============================================
  // PRODUCT SELECTION METHODS
  // =============================================

  toggleProductSelection(productId: number, event: Event): void {
    event.stopPropagation();
    const index = this.formData.products.indexOf(productId);
    
    if (index > -1) {
      this.formData.products.splice(index, 1);
    } else {
      this.formData.products.push(productId);
    }
    
    this.updateSelectedProductNames();
    this.validateField('products');
  }

  isProductSelected(productId: number): boolean {
    return this.formData.products.includes(productId);
  }

  getProductName(productId: number): string {
    const product = this.availableProducts.find(p => p.productId === productId);
    return product ? product.name : '';
  }

  private updateSelectedProductNames(): void {
    if (this.formData.products.length === 0) {
      this.selectedProductNames = 'Select products...';
    } else if (this.formData.products.length === 1) {
      const product = this.availableProducts.find(p => p.productId === this.formData.products[0]);
      this.selectedProductNames = product ? product.name : '1 product selected';
    } else {
      this.selectedProductNames = `${this.formData.products.length} products selected`;
    }
  }

  removeProduct(productId: number, event: Event): void {
    event.stopPropagation();
    const index = this.formData.products.indexOf(productId);
    if (index > -1) {
      this.formData.products.splice(index, 1);
      this.updateSelectedProductNames();
      this.validateField('products');
    }
  }

  clearAllProducts(event: Event): void {
    event.stopPropagation();
    this.formData.products = [];
    this.updateSelectedProductNames();
    this.validateField('products');
  }

  // =============================================
  // VALIDATION METHODS
  // =============================================

  validateField(fieldName: string): void {
    const value = this.getFieldValue(fieldName);
    
    switch(fieldName) {
      case 'companyName':
        this.formErrors.companyName = !value ? 'Company name is required' : '';
        break;
      case 'fullName':
        this.formErrors.fullName = !value ? 'Full name is required' : '';
        break;
      case 'email':
        if (!value) {
          this.formErrors.email = 'Email is required';
        } else if (!this.isValidEmail(value)) {
          this.formErrors.email = 'Please enter a valid email address';
        } else {
          this.formErrors.email = '';
        }
        break;
      case 'jobTitle':
        this.formErrors.jobTitle = !value ? 'Job title is required' : '';
        break;
      case 'domain':
        this.formErrors.domain = !value ? 'Domain is required' : '';
        break;
      case 'industry':
        this.formErrors.industry = !value ? 'Industry is required' : '';
        break;
      case 'country':
        this.formErrors.country = !value ? 'Country is required' : '';
        break;
      case 'phone':
        if (!value) {
          this.formErrors.phone = 'Phone number is required';
        } else if (!this.isValidPhone(value)) {
          this.formErrors.phone = 'Please enter a valid phone number';
        } else {
          this.formErrors.phone = '';
        }
        break;
      case 'products':
        this.formErrors.products = this.formData.products.length === 0 ? 'Please select at least one product' : '';
        break;
      case 'quantity':
        if (!value) {
          this.formErrors.quantity = 'Quantity is required';
        } else if (value < 1) {
          this.formErrors.quantity = 'Quantity must be at least 1';
        } else {
          this.formErrors.quantity = '';
        }
        break;
      case 'timeline':
        this.formErrors.timeline = !value ? 'Please select a timeline' : '';
        break;
      case 'businessRequirement':
        if (!value) {
          this.formErrors.businessRequirement = 'Business requirement is required';
        } else if (value.length < 20) {
          this.formErrors.businessRequirement = 'Please provide at least 20 characters';
        } else {
          this.formErrors.businessRequirement = '';
        }
        break;
    }
  }

  private getFieldValue(fieldName: string): any {
    switch(fieldName) {
      case 'companyName': return this.formData.companyName;
      case 'fullName': return this.formData.fullName;
      case 'email': return this.formData.email;
      case 'jobTitle': return this.formData.jobTitle;
      case 'domain': return this.formData.domain;
      case 'industry': return this.formData.industry;
      case 'country': return this.formData.country;
      case 'phone': return this.formData.phone;
      case 'quantity': return this.formData.quantity;
      case 'timeline': return this.formData.timeline;
      case 'businessRequirement': return this.formData.businessRequirement;
      default: return null;
    }
  }

  private isValidEmail(email: string): boolean {
    const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    return emailRegex.test(email);
  }

  private isValidPhone(phone: string): boolean {
    // At least 10 digits, allow +, spaces, dashes, parentheses
    const phoneRegex = /^[\+\d\s\-\(\)]{10,}$/;
    return phoneRegex.test(phone.replace(/\s/g, ''));
  }

  isFormValid(): boolean {
    // Check all fields for errors
    this.validateAllFields();
    
    // Check if any errors exist
    for (const key in this.formErrors) {
      if (this.formErrors[key]) {
        return false;
      }
    }
    
    // Check if required fields have values
    return !!(this.formData.companyName && 
              this.formData.fullName && 
              this.formData.email && 
              this.isValidEmail(this.formData.email) &&
              this.formData.jobTitle && 
              this.formData.domain && 
              this.formData.industry && 
              this.formData.country && 
              this.formData.phone && 
              this.isValidPhone(this.formData.phone) &&
              this.formData.products.length > 0 &&
              this.formData.quantity && 
              this.formData.quantity >= 1 &&
              this.formData.timeline && 
              this.formData.businessRequirement && 
              this.formData.businessRequirement.length >= 20);
  }

  private validateAllFields(): void {
    const fields = ['companyName', 'fullName', 'email', 'jobTitle', 'domain', 
                    'industry', 'country', 'phone', 'products', 'quantity', 
                    'timeline', 'businessRequirement'];
    fields.forEach(field => this.validateField(field));
  }

  // =============================================
  // FORM ACTIONS
  // =============================================

  closeModal(): void {
    this.closeDropdown();
    this.close.emit();
  }

  onSubmit(form: any): void {
    this.validateAllFields();
    
    if (this.isFormValid()) {
      this.isLoading = true;
      
      // Simulate API call
      setTimeout(() => {
        this.isLoading = false;
        this.submit.emit(this.formData);
        this.closeModal();
        // Reset form after submission
        this.resetForm();
      }, 1000);
    } else {
      // Mark all fields as touched to show errors
      Object.keys(form.controls).forEach(key => {
        form.controls[key].markAsTouched();
      });
    }
  }

  resetForm(): void {
    this.formData = {
      companyName: '',
      fullName: '',
      email: '',
      jobTitle: '',
      domain: '',
      industry: '',
      country: '',
      phone: '',
      products: [],
      quantity: null,
      timeline: '',
      businessRequirement: ''
    };
    this.selectedProductNames = 'Select products...';
    this.isDropdownOpen = false;
    this.formErrors = {};
  }

  // =============================================
  // HOST LISTENER FOR CLICK OUTSIDE
  // =============================================

  @HostListener('document:click', ['$event'])
  handleClickOutside(event: Event): void {
    if (this.dropdownContainer && 
        !this.dropdownContainer.nativeElement.contains(event.target) &&
        this.isDropdownOpen) {
      this.closeDropdown();
    }
  }
}
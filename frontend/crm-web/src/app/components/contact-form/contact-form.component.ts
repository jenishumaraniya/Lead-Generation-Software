import { Component, EventEmitter, Input, Output, OnInit, ElementRef, ViewChild, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ApiService } from '../../core/services/api.service';
import { Product } from '../../core/models/product.model';
import { VisitorTrackingService } from '../../core/services/visitor-tracking.service';
import { COUNTRY_DATA, CountryCodeItem } from './country-data';

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
  productIds?: number[];
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
  @Output() leadSubmitted = new EventEmitter<any>();
  @Output() showToast = new EventEmitter<string>();

  @ViewChild('dropdownContainer') dropdownContainer!: ElementRef;

  availableProducts: Product[] = [];
  countries: CountryCodeItem[] = COUNTRY_DATA;
  selectedCountry: CountryCodeItem = COUNTRY_DATA[0]; // Default India (+91)
  phoneLocalNumber = '';

  isLoading = false;
  isDropdownOpen = false;
  isCountryDropdownOpen = false;
  submitError = '';
  searchQuery = '';

  formData: ContactFormData = {
    companyName: '',
    fullName: '',
    email: '',
    jobTitle: '',
    domain: '',
    industry: '',
    country: 'India',
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
  formErrors: any = {};

  constructor(
    private http: HttpClient,
    private apiService: ApiService,
    private visitorTrackingService: VisitorTrackingService
  ) {}

  ngOnInit(): void {
    this.loadProducts();
    this.detectUserCountry();
  }

  private detectUserCountry(): void {
    // 1. Primary: Use free external IP geolocation API
    this.http.get<any>('https://ipapi.co/json/').subscribe({
      next: (res) => {
        if (res && res.country_code) {
          const matched = this.countries.find(
            c => c.code.toUpperCase() === res.country_code.toUpperCase() ||
                 (res.country_calling_code && c.dialCode === res.country_calling_code)
          );
          if (matched) {
            this.selectedCountry = matched;
            this.formData.country = matched.name === 'Other / International' ? res.country_name || '' : matched.name;
          } else if (res.country_name) {
            const dynamicItem: CountryCodeItem = {
              name: res.country_name,
              code: res.country_code,
              dialCode: res.country_calling_code || '+',
              flag: '🌐',
              patternLength: 10
            };
            this.countries.unshift(dynamicItem);
            this.selectedCountry = dynamicItem;
            this.formData.country = res.country_name;
          }
          this.updateFullPhoneNumber();
        }
      },
      error: () => {
        // 2. Secondary Fallback: Browser timezone detection
        try {
          const timezone = Intl.DateTimeFormat().resolvedOptions().timeZone || '';
          if (timezone.includes('Calcutta') || timezone.includes('Kolkata') || timezone.includes('Asia/Colombo')) {
            this.selectCountryByCode('IN');
          } else if (timezone.includes('New_York') || timezone.includes('Chicago') || timezone.includes('Los_Angeles') || timezone.includes('America')) {
            this.selectCountryByCode('US');
          } else if (timezone.includes('London') || timezone.includes('Europe/London')) {
            this.selectCountryByCode('GB');
          } else if (timezone.includes('Dubai')) {
            this.selectCountryByCode('AE');
          } else if (timezone.includes('Singapore')) {
            this.selectCountryByCode('SG');
          }
        } catch {
          // Default India
        }
      }
    });
  }

  selectCountryByCode(code: string): void {
    const found = this.countries.find(c => c.code === code);
    if (found) {
      this.selectedCountry = found;
      this.formData.country = found.name === 'Other / International' ? '' : found.name;
      this.updateFullPhoneNumber();
    }
  }

  get maxPhoneDigits(): number {
    return this.selectedCountry && this.selectedCountry.patternLength
      ? this.selectedCountry.patternLength
      : 15;
  }

  onCountryChange(): void {
    const found = this.countries.find(c => c.name.toLowerCase() === (this.formData.country || '').toLowerCase());
    if (found) {
      this.selectedCountry = found;
    }
    this.enforcePhoneDigitLimit();
    this.updateFullPhoneNumber();
    this.validateField('country');
    this.validateField('phone');
  }

  onCountryCodeSelect(country: CountryCodeItem): void {
    this.selectedCountry = country;
    if (country.code !== 'OTHER') {
      this.formData.country = country.name;
    }
    this.isCountryDropdownOpen = false;
    this.enforcePhoneDigitLimit();
    this.updateFullPhoneNumber();
    this.validateField('country');
    this.validateField('phone');
  }

  private enforcePhoneDigitLimit(): void {
    let rawDigits = this.phoneLocalNumber.replace(/\D/g, '');
    const maxDigits = this.maxPhoneDigits;
    if (rawDigits.length > maxDigits) {
      this.phoneLocalNumber = rawDigits.slice(0, maxDigits);
    }
  }

  onPhoneKeyDown(event: KeyboardEvent): void {
    const allowedKeys = ['Backspace', 'Tab', 'Delete', 'ArrowLeft', 'ArrowRight', 'Home', 'End', 'Enter'];
    if (allowedKeys.includes(event.key) || event.ctrlKey || event.metaKey) {
      return;
    }
    // Block any non-numeric key press
    if (!/^[0-9]$/.test(event.key)) {
      event.preventDefault();
    }
  }

  onPhonePaste(event: ClipboardEvent): void {
    event.preventDefault();
    const pastedText = event.clipboardData?.getData('text') || '';
    let digits = pastedText.replace(/[^0-9]/g, '');
    const maxDigits = this.maxPhoneDigits;
    if (digits.length > maxDigits) {
      digits = digits.slice(0, maxDigits);
    }
    this.phoneLocalNumber = digits;
    this.updateFullPhoneNumber();
    this.validateField('phone');
  }

  onPhoneInput(): void {
    let rawDigits = this.phoneLocalNumber.replace(/[^0-9]/g, '');
    const maxDigits = this.maxPhoneDigits;
    if (rawDigits.length > maxDigits) {
      rawDigits = rawDigits.slice(0, maxDigits);
    }
    this.phoneLocalNumber = rawDigits;
    this.updateFullPhoneNumber();
    this.validateField('phone');
  }

  private updateFullPhoneNumber(): void {
    const rawLocal = this.phoneLocalNumber.replace(/\D/g, '');
    if (rawLocal) {
      if (this.selectedCountry.dialCode === '+') {
        this.formData.phone = '+' + rawLocal;
      } else {
        this.formData.phone = `${this.selectedCountry.dialCode} ${rawLocal}`;
      }
    } else {
      this.formData.phone = '';
    }
  }

  private loadProducts(): void {
    this.apiService.getProducts().subscribe({
      next: (products) => {
        this.availableProducts = products || [];
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

  toggleDropdown(event: Event): void {
    event.stopPropagation();
    this.isDropdownOpen = !this.isDropdownOpen;
    this.isCountryDropdownOpen = false;
  }

  toggleCountryDropdown(event: Event): void {
    event.stopPropagation();
    this.isCountryDropdownOpen = !this.isCountryDropdownOpen;
    this.isDropdownOpen = false;
  }

  closeDropdown(): void {
    this.isDropdownOpen = false;
    this.isCountryDropdownOpen = false;
  }

  get filteredProducts(): Product[] {
    if (!this.searchQuery) return this.availableProducts;
    return this.availableProducts.filter(product =>
      product.name.toLowerCase().includes(this.searchQuery.toLowerCase())
    );
  }

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
    }
    this.updateSelectedProductNames();
    this.validateField('products');
  }

  clearAllProducts(event: Event): void {
    event.stopPropagation();
    this.formData.products = [];
    this.updateSelectedProductNames();
    this.validateField('products');
  }

  validateField(fieldName: string): void {
    const value = this.getFieldValue(fieldName);
    switch(fieldName) {
      case 'companyName':
        if (!value || !value.trim()) {
          this.formErrors.companyName = 'Company name is required';
        } else if (value.trim().length < 2) {
          this.formErrors.companyName = 'Company name must be at least 2 characters';
        } else {
          this.formErrors.companyName = '';
        }
        break;

      case 'fullName':
        if (!value || !value.trim()) {
          this.formErrors.fullName = 'Full name is required';
        } else if (value.trim().length < 2) {
          this.formErrors.fullName = 'Please enter a valid full name';
        } else {
          this.formErrors.fullName = '';
        }
        break;

      case 'email':
        if (!value || !value.trim()) {
          this.formErrors.email = 'Email address is required';
        } else if (!this.isValidEmail(value.trim())) {
          this.formErrors.email = 'Please enter a valid business email address (e.g. name@company.com)';
        } else {
          this.formErrors.email = '';
        }
        break;

      case 'jobTitle':
        if (!value || !value.trim()) {
          this.formErrors.jobTitle = 'Job title is required';
        } else {
          this.formErrors.jobTitle = '';
        }
        break;

      case 'domain':
        if (!value || !value.trim()) {
          this.formErrors.domain = 'Domain is required (e.g. Technology, Finance, Healthcare)';
        } else {
          this.formErrors.domain = '';
        }
        break;

      case 'industry':
        if (!value || !value.trim()) {
          this.formErrors.industry = 'Industry is required';
        } else {
          this.formErrors.industry = '';
        }
        break;

      case 'country':
        if (!value || !value.trim()) {
          this.formErrors.country = 'Country is required';
        } else {
          this.formErrors.country = '';
        }
        break;

      case 'phone': {
        const rawDigits = this.phoneLocalNumber.replace(/\D/g, '');
        const country = this.selectedCountry;

        if (!rawDigits) {
          this.formErrors.phone = 'Phone number is required';
        } else if (country && country.code === 'IN') {
          if (rawDigits.length !== 10) {
            this.formErrors.phone = 'Indian phone number must be exactly 10 digits';
          } else if (!/^[6-9]\d{9}$/.test(rawDigits)) {
            this.formErrors.phone = 'Indian mobile number must start with 6, 7, 8, or 9';
          } else {
            this.formErrors.phone = '';
          }
        } else if (country && country.patternLength && country.code !== 'OTHER') {
          if (rawDigits.length !== country.patternLength) {
            this.formErrors.phone = `${country.name} phone number must be exactly ${country.patternLength} digits`;
          } else {
            this.formErrors.phone = '';
          }
        } else {
          if (rawDigits.length < 7 || rawDigits.length > 15) {
            this.formErrors.phone = 'Please enter a valid phone number (7-15 digits)';
          } else {
            this.formErrors.phone = '';
          }
        }
        break;
      }

      case 'products':
        this.formErrors.products = this.formData.products.length === 0 ? 'Please select at least one product' : '';
        break;

      case 'quantity':
        if (!value) {
          this.formErrors.quantity = 'Quantity is required';
        } else if (value < 1) {
          this.formErrors.quantity = 'Quantity must be at least 1 unit';
        } else {
          this.formErrors.quantity = '';
        }
        break;

      case 'timeline':
        this.formErrors.timeline = !value ? 'Please specify your implementation timeline' : '';
        break;

      case 'businessRequirement':
        if (!value || !value.trim()) {
          this.formErrors.businessRequirement = 'Business requirement description is required';
        } else if (value.trim().length < 20) {
          this.formErrors.businessRequirement = `Please provide at least 20 characters (${value.trim().length}/20)`;
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

  isFormValid(): boolean {
    this.validateAllFields();
    for (const key in this.formErrors) {
      if (this.formErrors[key]) {
        return false;
      }
    }
    const rawPhoneDigits = this.phoneLocalNumber.replace(/\D/g, '');
    return !!(
      this.formData.companyName?.trim() &&
      this.formData.fullName?.trim() &&
      this.formData.email?.trim() &&
      this.isValidEmail(this.formData.email.trim()) &&
      this.formData.jobTitle?.trim() &&
      this.formData.domain?.trim() &&
      this.formData.industry?.trim() &&
      this.formData.country?.trim() &&
      rawPhoneDigits.length >= 7 &&
      this.formData.products.length > 0 &&
      this.formData.quantity &&
      this.formData.quantity >= 1 &&
      this.formData.timeline &&
      this.formData.businessRequirement?.trim() &&
      this.formData.businessRequirement.trim().length >= 20
    );
  }

  private validateAllFields(): void {
    const fields = ['companyName', 'fullName', 'email', 'jobTitle', 'domain',
                    'industry', 'country', 'phone', 'products', 'quantity',
                    'timeline', 'businessRequirement'];
    fields.forEach(field => this.validateField(field));
  }

  closeModal(): void {
    this.closeDropdown();
    this.close.emit();
  }

  onSubmit(form: any): void {
    this.submitError = '';
    this.updateFullPhoneNumber();
    this.validateAllFields();

    if (this.isFormValid()) {
      this.isLoading = true;
      const anonymousId = this.visitorTrackingService.getVisitorId() || 'temp-' + Date.now();

      if (this.formData.products.length > 0) {
        this.visitorTrackingService.trackActivity(
          'LEAD_SUBMITTED',
          this.formData.products[0],
          { 
            source: 'contact_form_submission',
            products: this.formData.products,
            timeline: this.formData.timeline
          }
        );
      }

      const payload = {
        companyName: this.formData.companyName.trim(),
        fullName: this.formData.fullName.trim(),
        email: this.formData.email.trim(),
        jobTitle: this.formData.jobTitle.trim(),
        domain: this.formData.domain.trim(),
        industry: this.formData.industry.trim(),
        country: this.formData.country.trim(),
        phone: this.formData.phone.trim(),
        products: this.formData.products,
        productIds: this.formData.products,
        quantity: this.formData.quantity,
        timeline: this.formData.timeline,
        businessRequirement: this.formData.businessRequirement.trim(),
        source: 'WEBSITE_FORM',
        visitorId: anonymousId
      };

      this.apiService.submitLead(payload).subscribe({
        next: (response) => {
          this.isLoading = false;
          this.leadSubmitted.emit(response);
          this.submit.emit(this.formData);
          this.closeModal();
          this.resetForm();
          this.showToast.emit('Thank you! We will contact you shortly.');
        },
        error: (err) => {
          this.isLoading = false;
          if (err.status === 409) {
            this.showToast.emit(err.error?.message || 'This email is already registered. We will contact you shortly.');
          } else if (err.status === 404) {
            this.submitError = 'Backend not reachable. Please check your connection.';
          } else {
            this.submitError = 'Something went wrong. Please try again later.';
            console.error('Lead submission failed:', err);
          }
        }
      });
    } else {
      Object.keys(form.controls || {}).forEach(key => {
        form.controls[key]?.markAsTouched();
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
      country: 'India',
      phone: '',
      products: [],
      quantity: null,
      timeline: '',
      businessRequirement: ''
    };
    this.phoneLocalNumber = '';
    this.selectedProductNames = 'Select products...';
    this.isDropdownOpen = false;
    this.isCountryDropdownOpen = false;
    this.formErrors = {};
    this.submitError = '';
    this.searchQuery = '';
  }

  @HostListener('document:click', ['$event'])
  handleClickOutside(event: Event): void {
    if (this.dropdownContainer &&
        !this.dropdownContainer.nativeElement.contains(event.target) &&
        this.isDropdownOpen) {
      this.closeDropdown();
    }
  }
}
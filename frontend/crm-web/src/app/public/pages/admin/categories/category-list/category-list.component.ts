import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Category } from '../../../../../core/models/category.model';
import { CategoryService } from '../../../../../core/services/category.service';
import { EmployeeService } from '../../../../../core/services/employee.service';

@Component({
  selector: 'app-category-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './category-list.component.html',
  styleUrls: ['./category-list.component.css']
})
export class CategoryListComponent implements OnInit {
  categories: Category[] = [];
  filteredCategories: Category[] = [];
  salespersons: any[] = [];
  salespersonsCount = 0;
  
  searchTerm: string = '';
  selectedFilter: string = 'ALL';
  viewMode: 'cards' | 'table' = 'cards';

  stats = {
    totalCategories: 0,
    coveredCount: 0,
    unassignedCount: 0,
    totalReps: 0
  };

  showModal = false;
  isEdit = false;
  editingId: number | null = null;
  categoryName = '';
  loading = false;
  noticeMessage = '';

  constructor(
    private categoryService: CategoryService,
    private employeeService: EmployeeService
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    this.categoryService.getCategories().subscribe({
      next: (data) => {
        this.categories = data || [];
        this.employeeService.getEmployees().subscribe({
          next: (users) => {
            this.salespersons = (users || []).filter(u => u.role === 'SALES_REP' && u.isActive);
            this.salespersonsCount = this.salespersons.length;
            this.calculateStats();
            this.applyFilter();
            this.loading = false;
          },
          error: () => {
            this.salespersons = [];
            this.salespersonsCount = 0;
            this.calculateStats();
            this.applyFilter();
            this.loading = false;
          }
        });
      },
      error: () => {
        this.categories = [];
        this.filteredCategories = [];
        this.calculateStats();
        this.loading = false;
      }
    });
  }

  calculateStats(): void {
    this.stats.totalCategories = this.categories.length;
    this.stats.totalReps = this.salespersonsCount;
    
    const assignedCategoryIds = new Set(this.salespersons.filter(s => s.categoryId).map(s => s.categoryId));
    this.stats.coveredCount = this.categories.filter(c => assignedCategoryIds.has(c.categoryId)).length;
    this.stats.unassignedCount = this.categories.filter(c => !assignedCategoryIds.has(c.categoryId)).length;
  }

  getAssignedSalesperson(categoryId: number): any {
    return this.salespersons.find(s => s.categoryId === categoryId);
  }

  setViewMode(mode: 'cards' | 'table'): void {
    this.viewMode = mode;
  }

  applyFilter(): void {
    const term = this.searchTerm.trim().toLowerCase();

    this.filteredCategories = this.categories.filter(cat => {
      const assignedRep = this.getAssignedSalesperson(cat.categoryId);

      if (this.selectedFilter === 'ASSIGNED' && !assignedRep) return false;
      if (this.selectedFilter === 'UNASSIGNED' && assignedRep) return false;

      if (!term) return true;

      const nameMatch = cat.categoryName?.toLowerCase().includes(term);
      const repMatch = assignedRep?.fullName?.toLowerCase().includes(term);
      return nameMatch || repMatch;
    });
  }

  openAddModal(): void {
    this.isEdit = false;
    this.editingId = null;
    this.categoryName = '';
    this.showModal = true;
  }

  openEditModal(category: Category): void {
    this.isEdit = true;
    this.editingId = category.categoryId;
    this.categoryName = category.categoryName;
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.categoryName = '';
  }

  saveCategory(): void {
    if (!this.categoryName.trim()) return;
    this.loading = true;
    const data = { categoryName: this.categoryName.trim() };
    const obs = this.isEdit
      ? this.categoryService.updateCategory(this.editingId!, data)
      : this.categoryService.createCategory(data);

    obs.subscribe({
      next: () => {
        this.loadData();
        this.closeModal();
        this.loading = false;
        if (!this.isEdit) {
          this.noticeMessage = 'Category created successfully. Reminder: ensure a Sales Representative is available to handle leads in this category.';
          setTimeout(() => this.noticeMessage = '', 6000);
        }
      },
      error: () => {
        alert('Failed to save category');
        this.loading = false;
      }
    });
  }

  deleteCategory(id: number): void {
    if (confirm('Are you sure you want to delete this category?')) {
      this.loading = true;
      this.categoryService.deleteCategory(id).subscribe({
        next: () => {
          this.loadData();
          this.loading = false;
        },
        error: () => {
          alert('Failed to delete category');
          this.loading = false;
        }
      });
    }
  }
}
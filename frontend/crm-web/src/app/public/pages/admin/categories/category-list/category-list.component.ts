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
  salespersonsCount = 0;
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
        this.loading = false;
      },
      error: () => {
        this.categories = [];
        this.loading = false;
      }
    });

    this.employeeService.getEmployees().subscribe({
      next: (users) => {
        this.salespersonsCount = users.filter(u => u.role === 'SALES_REP').length;
      },
      error: () => this.salespersonsCount = 0
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
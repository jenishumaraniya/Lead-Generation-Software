import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminApiService } from '../../app/core/services/admin-api.services';
import { AuthService } from '../../app/core/services/auth.service';

@Component({
  selector: 'app-category-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './category-management.component.html',
  styleUrls: ['./category-management.component.css']
})
export class CategoryManagementComponent implements OnInit {
  categories: any[] = [];
  isLoading = false;

  showModal = false;
  isEditing = false;
  editingCategoryId: number | null = null;
  categoryName = '';

  constructor(
    private adminApi: AdminApiService,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.isLoading = true;
    this.adminApi.getCategories().subscribe({
      next: (data) => {
        this.categories = data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
      }
    });
  }

  openCreateModal(): void {
    if (!this.authService.isAdmin()) return;
    this.isEditing = false;
    this.editingCategoryId = null;
    this.categoryName = '';
    this.showModal = true;
  }

  openEditModal(cat: any): void {
    if (!this.authService.isAdmin()) return;
    this.isEditing = true;
    this.editingCategoryId = cat.categoryId;
    this.categoryName = cat.categoryName;
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
  }

  saveCategory(): void {
    if (!this.authService.isAdmin()) return;
    if (!this.categoryName.trim()) {
      alert('Category name is required.');
      return;
    }

    if (this.isEditing && this.editingCategoryId) {
      this.adminApi.updateCategory(this.editingCategoryId, { categoryName: this.categoryName }).subscribe({
        next: () => {
          this.closeModal();
          this.loadCategories();
        },
        error: (err) => alert(err.error?.error || 'Failed to update category')
      });
    } else {
      this.adminApi.createCategory({ categoryName: this.categoryName }).subscribe({
        next: () => {
          this.closeModal();
          this.loadCategories();
        },
        error: (err) => alert(err.error?.error || 'Failed to create category')
      });
    }
  }

  deleteCategory(cat: any): void {
    if (!this.authService.isAdmin()) return;
    if (confirm(`Are you sure you want to delete category "${cat.categoryName}"?`)) {
      this.adminApi.deleteCategory(cat.categoryId).subscribe({
        next: () => this.loadCategories(),
        error: (err) => alert(err.error?.error || 'Failed to delete category')
      });
    }
  }
}

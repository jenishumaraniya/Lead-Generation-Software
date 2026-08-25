import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Category } from '../../../../../core/models/category.model';
import { CategoryService } from '../../../../../core/services/category.service';

@Component({
  selector: 'app-category-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './category-list.component.html',
  styleUrls: ['./category-list.component.css']
})
export class CategoryListComponent implements OnInit {
  categories: Category[] = [];
  showModal = false;
  isEdit = false;
  editingId: number | null = null;
  categoryName = '';
  loading = false;

  constructor(private categoryService: CategoryService) {}

  ngOnInit() {
    this.loadCategories();
  }

  loadCategories() {
    this.loading = true;
    this.categoryService.getCategories().subscribe({
      next: (data) => {
        this.categories = data;
        this.loading = false;
      },
      error: () => {
        this.categories = [];
        this.loading = false;
      }
    });
  }

  openAddModal() {
    this.isEdit = false;
    this.editingId = null;
    this.categoryName = '';
    this.showModal = true;
  }

  openEditModal(category: Category) {
    this.isEdit = true;
    this.editingId = category.categoryId;
    this.categoryName = category.categoryName;
    this.showModal = true;
  }

  closeModal() {
    this.showModal = false;
    this.categoryName = '';
  }

  saveCategory() {
    if (!this.categoryName.trim()) return;
    this.loading = true;
    const data = { categoryName: this.categoryName.trim() };
    const obs = this.isEdit
      ? this.categoryService.updateCategory(this.editingId!, data)
      : this.categoryService.createCategory(data);

    obs.subscribe({
      next: () => {
        this.loadCategories();
        this.closeModal();
        this.loading = false;
      },
      error: () => {
        alert('Failed to save category');
        this.loading = false;
      }
    });
  }

  deleteCategory(id: number) {
    if (confirm('Delete this category?')) {
      this.loading = true;
      this.categoryService.deleteCategory(id).subscribe({
        next: () => {
          this.loadCategories();
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
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="pagination-bar" *ngIf="totalItems > 0">
      <div class="pagination-info">
        Showing <strong>{{ startIndex + 1 }}</strong> to <strong>{{ endIndex }}</strong> of <strong>{{ totalItems }}</strong> entries
      </div>

      <div class="pagination-controls">
        <button 
          type="button" 
          class="page-btn" 
          [disabled]="currentPage === 1"
          (click)="onPage(currentPage - 1)"
        >
          ← Prev
        </button>

        <div class="page-numbers">
          <button 
            *ngFor="let p of visiblePages" 
            type="button" 
            class="page-num-btn" 
            [class.active]="p === currentPage"
            (click)="onPage(p)"
          >
            {{ p }}
          </button>
        </div>

        <button 
          type="button" 
          class="page-btn" 
          [disabled]="currentPage >= totalPages"
          (click)="onPage(currentPage + 1)"
        >
          Next →
        </button>
      </div>
    </div>
  `,
  styles: [`
    .pagination-bar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 16px 20px;
      background: #ffffff;
      border-top: 1px solid #e2e8f0;
      border-radius: 0 0 12px 12px;
      font-size: 0.85rem;
      color: #64748b;
      flex-wrap: wrap;
      gap: 12px;
    }
    .pagination-controls {
      display: flex;
      align-items: center;
      gap: 6px;
    }
    .page-btn, .page-num-btn {
      padding: 6px 12px;
      background: #ffffff;
      border: 1px solid #cbd5e1;
      border-radius: 6px;
      color: #334155;
      font-size: 0.85rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.15s ease;
    }
    .page-btn:hover:not(:disabled), .page-num-btn:hover {
      background: #f1f5f9;
      border-color: #94a3b8;
    }
    .page-num-btn.active {
      background: #2563eb;
      border-color: #2563eb;
      color: #ffffff;
      font-weight: 700;
    }
    .page-btn:disabled {
      opacity: 0.4;
      cursor: not-allowed;
    }
    .page-numbers {
      display: flex;
      gap: 4px;
    }
  `]
})
export class PaginationComponent {
  @Input() totalItems = 0;
  @Input() pageSize = 10;
  @Input() currentPage = 1;
  @Output() pageChange = new EventEmitter<number>();

  get totalPages(): number {
    return Math.ceil(this.totalItems / this.pageSize) || 1;
  }

  get startIndex(): number {
    return (this.currentPage - 1) * this.pageSize;
  }

  get endIndex(): number {
    return Math.min(this.startIndex + this.pageSize, this.totalItems);
  }

  get visiblePages(): number[] {
    const pages: number[] = [];
    const total = this.totalPages;
    let start = Math.max(1, this.currentPage - 2);
    let end = Math.min(total, start + 4);
    if (end - start < 4) {
      start = Math.max(1, end - 4);
    }
    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    return pages;
  }

  onPage(page: number): void {
    if (page >= 1 && page <= this.totalPages && page !== this.currentPage) {
      this.currentPage = page;
      this.pageChange.emit(page);
    }
  }
}

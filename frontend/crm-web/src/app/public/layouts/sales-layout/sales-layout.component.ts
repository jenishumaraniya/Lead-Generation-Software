import { Component, ElementRef, HostListener, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { SidebarService } from '../../../core/services/sidebar.service';
import { NotificationService, NotificationItem } from '../../../core/services/notification.service';
import { ChangePasswordModalComponent } from '../../../components/change-password-modal/change-password-modal.component';

@Component({
  selector: 'app-sales-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, ChangePasswordModalComponent],
  templateUrl: './sales-layout.component.html',
  styleUrls: ['./sales-layout.component.css']
})
export class SalesLayoutComponent implements OnInit {
  isSidebarCollapsed = false;
  showChangePassword = false;
  showNotifications = false;
  unreadCount = 0;
  notifications: NotificationItem[] = [];

  constructor(
    private authService: AuthService,
    private sidebarService: SidebarService,
    private notificationService: NotificationService,
    private router: Router,
    private elementRef: ElementRef
  ) {}

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.showNotifications) return;
    const clickedInside = this.elementRef.nativeElement.querySelector('.notification-center')?.contains(event.target as Node);
    if (!clickedInside) {
      this.showNotifications = false;
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.showNotifications = false;
  }

  ngOnInit(): void {
    this.sidebarService.isCollapsed$.subscribe(collapsed => {
      this.isSidebarCollapsed = collapsed;
    });

    this.notificationService.initPolling();
    this.notificationService.notifications$.subscribe(items => {
      this.notifications = items;
    });
    this.notificationService.unreadCount$.subscribe(count => {
      this.unreadCount = count;
    });
  }

  toggleSidebar(): void {
    this.sidebarService.toggle();
  }

  toggleNotifications(): void {
    this.showNotifications = !this.showNotifications;
  }

  closeNotifications(): void {
    this.showNotifications = false;
  }

  markAllRead(): void {
    this.notificationService.markAllAsRead().subscribe();
  }

  clearNotification(event: MouseEvent, item: NotificationItem): void {
    event.stopPropagation();
    this.notificationService.clearNotification(item.notificationId).subscribe();
  }

  clearAllNotifications(): void {
    this.notificationService.clearAll().subscribe();
  }

  onNotificationClick(item: NotificationItem): void {
    if (!item.isRead) {
      this.notificationService.markAsRead(item.notificationId).subscribe();
    }
    if (item.leadId) {
      this.router.navigate(['/sales/leads', item.leadId]);
      this.showNotifications = false;
    }
  }

  formatTime(dateStr: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffSec = Math.floor(diffMs / 1000);
    const diffMin = Math.floor(diffSec / 60);
    const diffHr = Math.floor(diffMin / 60);
    const diffDays = Math.floor(diffHr / 24);

    if (diffSec < 60) return 'Just now';
    if (diffMin < 60) return `${diffMin}m ago`;
    if (diffHr < 24) return `${diffHr}h ago`;
    return `${diffDays}d ago`;
  }

  get user() {
    return this.authService.getCurrentUser();
  }

  logout(): void { 
    this.authService.logout(); 
  }

  get welcomeMessage(): string {
    return this.user?.fullName || 'Sales Representative';
  }
}
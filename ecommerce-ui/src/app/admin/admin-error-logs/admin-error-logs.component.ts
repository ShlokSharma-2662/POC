import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { AdminFooterComponent } from '../../admin-footer/admin-footer.component';
import { LoaderComponent } from '../../shared/loader/loader.component';
import { AdminMetricsService, ApiAlert, PagedResult } from '../../services/admin-metrics.service';

interface ErrorLog {
  id: number;
  message: string;
  stackTrace?: string;
  path: string;
  timestamp: string;
  severity: string;
  userAgent?: string;
  ipAddress?: string;
}

@Component({
  selector: 'app-admin-error-logs',
  standalone: true,
  imports: [CommonModule, FormsModule, AdminFooterComponent, LoaderComponent],
  templateUrl: './admin-error-logs.component.html',
  styleUrls: ['./admin-error-logs.component.scss']
})
export class AdminErrorLogsComponent implements OnInit {
  // Component state
  loading = false;
  errorMessage = '';
  
  // Pagination
  currentPage = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;

  // Data
  errorLogs: ApiAlert[] = [];
  
  // Filters
  severityFilter = '';
  searchTerm = '';
  
  // Modal state
  selectedError: ApiAlert | null = null;
  Math = Math;

  constructor(private adminMetricsService: AdminMetricsService) {}

  ngOnInit(): void {
    this.loadErrorLogs();
    
    // Add event listener for escape key to close modal
    document.addEventListener('keydown', (event) => {
      if (event.key === 'Escape') {
        this.hideModal();
      }
    });
  }

  loadErrorLogs(): void {
    this.loading = true;
    this.errorMessage = '';
    
    this.adminMetricsService.getErrorLogs(
      this.currentPage,
      this.pageSize,
      this.severityFilter || undefined,
      this.searchTerm || undefined
    ).subscribe({
      next: (result: PagedResult<ApiAlert>) => {
        this.errorLogs = result.items;
        this.totalCount = result.totalCount;
        this.totalPages = Math.ceil(this.totalCount / this.pageSize);
        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = 'Failed to load error logs. Please try again.';
        this.loading = false;
      }
    });
  }

  // Pagination methods
  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.loadErrorLogs();
    }
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadErrorLogs();
    }
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.loadErrorLogs();
    }
  }

  getTotalPages(): number {
    return this.totalPages;
  }

  // Filter methods
  onSeverityFilterChange(): void {
    this.currentPage = 1;
    this.loadErrorLogs();
  }

  onSearchChange(): void {
    this.currentPage = 1;
    this.loadErrorLogs();
  }

  clearFilters(): void {
    this.severityFilter = '';
    this.searchTerm = '';
    this.currentPage = 1;
    this.loadErrorLogs();
  }

  getRowClass(error: ApiAlert): string {
    if (error.severity === 'Error') return 'table-danger';
    if (error.severity === 'Warning') return 'table-warning';
    if (error.severity === 'Info') return 'table-info';
    return 'table-success';
  }

  // Modal methods
  viewErrorDetails(error: ApiAlert): void {
    this.selectedError = error;
    this.showModal();
  }

  closeErrorDetails(): void {
    this.selectedError = null;
    this.hideModal();
  }

  private showModal(): void {
    // Use Bootstrap modal API to show the modal
    const modal = document.getElementById('errorDetailsModal');
    if (modal) {
      // Check if Bootstrap is available
      if (typeof (window as any).bootstrap !== 'undefined') {
        const bootstrapModal = new (window as any).bootstrap.Modal(modal);
        bootstrapModal.show();
      } else {
        // Fallback: show modal using CSS
        modal.style.display = 'block';
        modal.classList.add('show');
        document.body.classList.add('modal-open');
      }
    }
  }

  private hideModal(): void {
    const modal = document.getElementById('errorDetailsModal');
    if (modal) {
      if (typeof (window as any).bootstrap !== 'undefined') {
        const bootstrapModal = (window as any).bootstrap.Modal.getInstance(modal);
        if (bootstrapModal) {
          bootstrapModal.hide();
        }
      } else {
        // Fallback: hide modal using CSS
        modal.style.display = 'none';
        modal.classList.remove('show');
        document.body.classList.remove('modal-open');
      }
    }
  }

  onModalBackdropClick(event: Event): void {
    // Close modal when clicking on the backdrop
    if (event.target === event.currentTarget) {
      this.hideModal();
    }
  }

  // Utility methods
  getSeverityClass(severity: string): string {
    switch (severity.toLowerCase()) {
      case 'critical': return 'severity-critical';
      case 'error': return 'severity-error';
      case 'warning': return 'severity-warning';
      case 'info': return 'severity-info';
      default: return 'severity-info';
    }
  }

  getSeverityIcon(severity: string): string {
    switch (severity.toLowerCase()) {
      case 'critical': return 'fas fa-exclamation-triangle';
      case 'error': return 'fas fa-times-circle';
      case 'warning': return 'fas fa-exclamation-circle';
      case 'info': return 'fas fa-info-circle';
      default: return 'fas fa-info-circle';
    }
  }

  formatTimestamp(timestamp: string): string {
    return new Date(timestamp).toLocaleString();
  }

  // Get page numbers for pagination display
  getPageNumbers(): number[] {
    const pages: number[] = [];
    const maxPages = 5;
    let start = Math.max(1, this.currentPage - Math.floor(maxPages / 2));
    let end = Math.min(this.totalPages, start + maxPages - 1);
    
    if (end - start + 1 < maxPages) {
      start = Math.max(1, end - maxPages + 1);
    }
    
    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    
    return pages;
  }
}
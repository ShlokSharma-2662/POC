import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { NgChartsModule } from 'ng2-charts';
import { ChartDataset, ChartOptions } from 'chart.js';

import { AdminFooterComponent } from '../../admin-footer/admin-footer.component';
import { LoaderComponent } from '../../shared/loader/loader.component';
import { MonitoringService } from '../../services/monitoring.service';
import { AdminMetricsService, SystemMetric, PagedResult } from '../../services/admin-metrics.service';
import { ToastrService } from 'ngx-toastr';

interface EndpointPerformance {
  name: string;
  avgResponseTime: number;
  requestCount: number;
  errorRate: number;
  successRate: number;
  minTime: number;
  maxTime: number;
  healthStatus: string;
  lastCalled: Date;
}

@Component({
  selector: 'app-performance-dashboard',
  standalone: true,
  imports: [CommonModule, NgChartsModule, AdminFooterComponent, FormsModule, LoaderComponent],
  templateUrl: './performance-dashboard.component.html',
  styleUrls: ['./performance-dashboard.component.scss'],
})

export class PerformanceDashboardComponent implements OnInit, OnDestroy {
  // Chart data
  chartData: ChartDataset<'line' | 'bar'>[] = []
  chartLabels: string[] = [];
  errorChartData: ChartDataset<'line'>[] = [];
  errorChartLabels: string[] = [];

  // Chart options
  chartOptions: ChartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: true,
        position: 'top',
      },
    },
    scales: {
      y: {
        beginAtZero: true,
      },
    },
  };

  errorChartOptions: ChartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: true,
        position: 'top',
      },
    },
    scales: {
      y: {
        beginAtZero: true,
      },
    },
  };

  // Component state
  loading = false;
  errorMessage = '';
  lastUpdated: Date | null = null;
  nextRefresh: Date | null = null;
  Math = Math;

  // Controls
  selectedTimeRange = '24h';
  refreshInterval = 0; // in seconds
  endpointFilter = '';
  chartType: 'line' | 'bar' = 'line';

  // Metrics
  avgResponseTime = 0;
  responseTrend = 0;
  errorRate = 0;
  errorTrend = 0;
  throughput = 0;
  throughputTrend = 0;
  uptime = 99.9;

  // Endpoint data
  endpoints: EndpointPerformance[] = [];
  
  // Detailed view and pagination
  showDetailedView = false;
  currentPage = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;
  paginatedRequests: SystemMetric[] = [];
  
  // Modal data
  selectedEndpoint: EndpointPerformance | null = null;
  endpointRequests: SystemMetric[] = [];
  
  // Auto refresh
  private refreshTimer: any = null;
  private countdownTimer: any = null;
  private timeToRefresh = 0;

  constructor(
    private http: HttpClient, 
    private monitoring: MonitoringService,
    private adminMetricsService: AdminMetricsService,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.loadMetrics();
    
    // Add event listener for escape key to close modal
    document.addEventListener('keydown', (event) => {
      if (event.key === 'Escape') {
        this.hideModal();
      }
    });
  }

  ngOnDestroy(): void {
    this.clearTimers();
  }

  get filteredEndpoints(): EndpointPerformance[] {
    if (!this.endpointFilter) return this.endpoints;
    return this.endpoints.filter(ep => 
      ep.name.toLowerCase().includes(this.endpointFilter.toLowerCase())
    );
  }

  loadMetrics(): void {
    this.loading = true;
    this.errorMessage = '';
    
    // Add a timeout to prevent infinite loading
    const timeout = setTimeout(() => {
      if (this.loading) {
        this.errorMessage = 'Request timeout. Please try again.';
        this.loading = false;
      }
    }, 30000); // 30 seconds timeout
    
    this.adminMetricsService.getSystemMetrics(
      this.currentPage,
      this.pageSize,
      this.endpointFilter || undefined
    ).subscribe({
      next: (result: PagedResult<SystemMetric>) => {
        clearTimeout(timeout);
        this.processMetricsData(result.items);
        this.totalCount = result.totalCount;
        this.totalPages = Math.ceil(this.totalCount / this.pageSize);
        this.loading = false;
        this.lastUpdated = new Date();
      },
      error: (err) => {
        clearTimeout(timeout);
        
        if (err.status === 401) {
          this.errorMessage = 'Authentication required. Please log in as an admin.';
        } else if (err.status === 403) {
          this.errorMessage = 'Access denied. Admin privileges required.';
        } else if (err.status === 404) {
          this.errorMessage = 'Performance metrics endpoint not found.';
        } else if (err.status === 500) {
          this.errorMessage = 'Server error occurred while loading metrics.';
        } else {
          this.errorMessage = `Failed to load performance metrics: ${err.message || 'Unknown error'}`;
        }
        
        this.loading = false;
      }
    });
  }

  private processMetricsData(data: SystemMetric[]): void {
    if (data.length === 0) {
      // Reset all metrics to default values when no data
      this.avgResponseTime = 0;
      this.errorRate = 0;
      this.throughput = 0;
      this.endpoints = [];
      this.chartData = [];
      this.chartLabels = [];
      this.errorChartData = [];
      this.errorChartLabels = [];
      return;
    }

    // Process chart data
    this.chartData = [
      { 
        label: 'API Response Time (ms)', 
        data: data.map(m => m.responseTimeMs),
        borderColor: '#667eea',
        backgroundColor: 'rgba(102, 126, 234, 0.1)',
        tension: 0.4
      }
    ];
  
    this.chartLabels = data.map(m =>
      new Date(m.timestamp).toLocaleTimeString()
    );

    // Calculate metrics
    const responseTimes = data.map(m => m.responseTimeMs);
    this.avgResponseTime = responseTimes.reduce((a, b) => a + b, 0) / responseTimes.length;
    
    // Calculate error rate
    const errorCount = data.filter(m => m.statusCode >= 400).length;
    this.errorRate = (errorCount / data.length) * 100;

    // Generate error chart data
    this.generateErrorChartData(data);

    // Generate endpoint performance data
    this.generateEndpointData(data);
  }

  private generateEndpointData(data: SystemMetric[]): void {
    const endpointMap = new Map<string, { responseTimes: number[], count: number, errors: number }>();
    
    data.forEach(metric => {
      const key = metric.endpoint;
      if (!endpointMap.has(key)) {
        endpointMap.set(key, { responseTimes: [], count: 0, errors: 0 });
      }
      
      const entry = endpointMap.get(key)!;
      entry.responseTimes.push(metric.responseTimeMs);
      entry.count++;
      if (metric.statusCode >= 400) entry.errors++;
    });

    this.endpoints = Array.from(endpointMap.entries()).map(([name, data]) => ({
      name,
      avgResponseTime: data.responseTimes.reduce((a, b) => a + b, 0) / data.responseTimes.length,
      requestCount: data.count,
      errorRate: (data.errors / data.count) * 100,
      successRate: ((data.count - data.errors) / data.count) * 100,
      minTime: Math.min(...data.responseTimes),
      maxTime: Math.max(...data.responseTimes),
      healthStatus: data.errors > 0 ? 'Warning' : 'Healthy',
      lastCalled: new Date()
    })).sort((a, b) => b.requestCount - a.requestCount);
  }

  private generateErrorChartData(data: SystemMetric[]): void {
    // Group data by time intervals (e.g., every 5 minutes)
    const timeGroups = new Map<string, { total: number, errors: number }>();
    
    data.forEach(metric => {
      const timestamp = new Date(metric.timestamp);
      const timeKey = timestamp.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
      
      if (!timeGroups.has(timeKey)) {
        timeGroups.set(timeKey, { total: 0, errors: 0 });
      }
      
      const group = timeGroups.get(timeKey)!;
      group.total++;
      if (metric.statusCode >= 400) {
        group.errors++;
      }
    });

    // Convert to chart data
    const sortedTimes = Array.from(timeGroups.keys()).sort();
    
    if (sortedTimes.length === 0) {
      this.errorChartData = [];
      this.errorChartLabels = [];
      return;
    }
    
    const errorRates = sortedTimes.map(time => {
      const group = timeGroups.get(time)!;
      return group.total > 0 ? (group.errors / group.total) * 100 : 0;
    });
    
    this.errorChartData = [
      {
        label: 'Error Rate (%)',
        data: errorRates,
        borderColor: '#ef4444',
        backgroundColor: 'rgba(239, 68, 68, 0.1)',
        tension: 0.4,
        fill: false
      }
    ];
    
    this.errorChartLabels = sortedTimes;
  }

  // Pagination methods
  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.loadMetrics();
    }
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadMetrics();
    }
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.loadMetrics();
    }
  }

  getTotalPages(): number {
    return this.totalPages;
  }

  // Filter methods
  onEndpointFilterChange(): void {
    this.currentPage = 1;
    this.loadMetrics();
  }

  clearEndpointFilter(): void {
    this.endpointFilter = '';
    this.currentPage = 1;
    this.loadMetrics();
  }

  // View toggle methods
  toggleDetailedView(): void {
    this.showDetailedView = !this.showDetailedView;
    if (this.showDetailedView) {
      this.currentPage = 1; // Reset to first page when switching to detailed view
      this.loadMetrics();
    } else {
      // Clear endpoint filter when switching back to summary view
      this.endpointFilter = '';
      this.currentPage = 1;
      this.loadMetrics();
    }
  }

  // Auto refresh methods
  startAutoRefresh(intervalSeconds: number): void {
    this.clearTimers();
    this.refreshInterval = intervalSeconds;
    this.timeToRefresh = intervalSeconds;
    
    this.refreshTimer = setInterval(() => {
      this.loadMetrics();
    }, intervalSeconds * 1000);
    
    this.countdownTimer = setInterval(() => {
      this.timeToRefresh--;
      if (this.timeToRefresh <= 0) {
        this.timeToRefresh = intervalSeconds;
      }
    }, 1000);
  }

  stopAutoRefresh(): void {
    this.clearTimers();
    this.refreshInterval = 0;
    this.timeToRefresh = 0;
  }

  private clearTimers(): void {
    if (this.refreshTimer) {
      clearInterval(this.refreshTimer);
      this.refreshTimer = null;
    }
    if (this.countdownTimer) {
      clearInterval(this.countdownTimer);
      this.countdownTimer = null;
    }
  }

  getTimeToRefresh(): number {
    return this.timeToRefresh;
  }

  // Utility methods
  formatResponseTime(ms: number): string {
    return `${ms.toFixed(2)}ms`;
  }

  formatPercentage(value: number): string {
    return `${value.toFixed(1)}%`;
  }

  getStatusClass(statusCode: number | string): string {
    if (typeof statusCode === 'string') {
      if (statusCode === 'Error') return 'status-error';
      if (statusCode === 'Warning') return 'status-warning';
      if (statusCode === 'Info') return 'status-info';
      return 'status-success';
    }
    if (statusCode >= 500) return 'status-error';
    if (statusCode >= 400) return 'status-warning';
    if (statusCode >= 300) return 'status-info';
    return 'status-success';
  }

  getRequestRowClass(request: SystemMetric): string {
    if (request.statusCode >= 500) return 'table-danger';
    if (request.statusCode >= 400) return 'table-warning';
    if (request.statusCode >= 300) return 'table-info';
    return 'table-success';
  }

  getResponseTimeClass(responseTime: number): string {
    if (responseTime > 1000) return 'response-slow';
    if (responseTime > 500) return 'response-medium';
    return 'response-fast';
  }

  getStatusCodeClass(statusCode: number): string {
    if (statusCode >= 500) return 'status-error';
    if (statusCode >= 400) return 'status-warning';
    if (statusCode >= 300) return 'status-info';
    return 'status-success';
  }

  getRequestStatusClass(statusCode: number): string {
    if (statusCode >= 500) return 'status-error';
    if (statusCode >= 400) return 'status-warning';
    if (statusCode >= 300) return 'status-info';
    return 'status-success';
  }

  getRequestStatus(statusCode: number): string {
    if (statusCode >= 500) return 'Server Error';
    if (statusCode >= 400) return 'Client Error';
    if (statusCode >= 300) return 'Redirect';
    return 'Success';
  }

  onTimeRangeChange(): void {
    this.loadMetrics();
  }

  onRefreshIntervalChange(): void {
    if (this.refreshInterval > 0) {
      this.startAutoRefresh(this.refreshInterval);
    } else {
      this.stopAutoRefresh();
    }
  }

  refreshData(): void {
    this.loadMetrics();
  }



  seedMetrics(): void {
    this.adminMetricsService.seedMetrics().subscribe({
      next: (result) => {
        this.toastr.success(`Metrics seeded successfully: ${result.message}`, 'Success');
        this.loadMetrics(); // Reload after seeding
      },
      error: (err) => {
        this.toastr.error(`Seed Error: ${err.message || 'Unknown error'}`, 'Error');
      }
    });
  }

  setChartType(type: 'line' | 'bar'): void {
    this.chartType = type;
  }

  viewEndpointDetails(endpoint: EndpointPerformance): void {
    // Set the selected endpoint for the modal
    this.selectedEndpoint = endpoint;
    
    // Load the specific endpoint's request data
    this.loadEndpointRequests(endpoint.name);
    
    // Show the modal
    this.showModal();
  }

  private loadEndpointRequests(endpointName: string): void {
    // Filter the current data to get requests for this specific endpoint
    this.endpointRequests = this.paginatedRequests.filter(request => 
      request.endpoint === endpointName
    );
    
    // If we don't have enough data, load more
    if (this.endpointRequests.length < 5) {
      this.adminMetricsService.getSystemMetrics(
        1,
        50, // Get more data
        endpointName
      ).subscribe({
        next: (result: PagedResult<SystemMetric>) => {
          this.endpointRequests = result.items.slice(0, 10); // Show first 10 requests
        },
        error: (err) => {
          this.endpointRequests = [];
        }
      });
    }
  }

  private showModal(): void {
    // Use Bootstrap modal API to show the modal
    const modal = document.getElementById('endpointDetailsModal');
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
    const modal = document.getElementById('endpointDetailsModal');
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

  viewDetailedData(): void {
    // Close the modal
    this.hideModal();
    
    // Set the endpoint filter and switch to detailed view
    if (this.selectedEndpoint) {
      this.endpointFilter = this.selectedEndpoint.name;
      this.currentPage = 1;
      this.showDetailedView = true;
      this.loadMetrics();
    }
  }

  onModalBackdropClick(event: Event): void {
    // Close modal when clicking on the backdrop
    if (event.target === event.currentTarget) {
      this.hideModal();
    }
  }

  getStatusIcon(statusCode: number): string {
    if (statusCode >= 500) return 'fas fa-exclamation-triangle';
    if (statusCode >= 400) return 'fas fa-exclamation-circle';
    if (statusCode >= 300) return 'fas fa-info-circle';
    return 'fas fa-check-circle';
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

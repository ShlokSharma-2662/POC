import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgChartsModule } from 'ng2-charts';
import { ChartDataset, ChartOptions } from 'chart.js';
import { Subject, takeUntil } from 'rxjs';

import { AdminFooterComponent } from '../../admin-footer/admin-footer.component';
import { LoaderComponent } from '../../shared/loader/loader.component';
import { RevenueService, RevenueReport, RevenueSummary, RevenuePeriod } from '../../services/revenue.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-revenue-reporting',
  standalone: true,
  imports: [CommonModule, NgChartsModule, AdminFooterComponent, FormsModule, LoaderComponent],
  templateUrl: './revenue-reporting.component.html',
  styleUrls: ['./revenue-reporting.component.scss']
})
export class RevenueReportingComponent implements OnInit, OnDestroy {
  // Chart data
  chartData: ChartDataset<'line' | 'bar'>[] = [];
  chartLabels: string[] = [];
  chartType: 'line' | 'bar' = 'line';

  // Chart options
  chartOptions: ChartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: true,
        position: 'top',
        labels: {
          usePointStyle: true,
          padding: 20,
          font: {
            size: 12,
            weight: 'bold'
          }
        }
      },
      tooltip: {
        backgroundColor: 'rgba(0, 0, 0, 0.8)',
        titleColor: '#fff',
        bodyColor: '#fff',
        borderColor: '#667eea',
        borderWidth: 1,
        cornerRadius: 8,
        displayColors: true,
        callbacks: {
          title: function(context) {
            return context[0].label;
          },
          label: function(context) {
            const value = context.parsed.y;
            return `Revenue: ₹${value.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
          }
        }
      }
    },
    scales: {
      x: {
        display: true,
        grid: {
          display: false
        },
        ticks: {
          font: {
            size: 11
          },
          color: '#666'
        }
      },
      y: {
        display: true,
        beginAtZero: true,
        grid: {
          color: 'rgba(0, 0, 0, 0.05)'
        },
        ticks: {
          callback: function(value) {
            return '₹' + value.toLocaleString('en-IN');
          },
          font: {
            size: 11
          },
          color: '#666'
        }
      }
    },
    elements: {
      point: {
        radius: 4,
        hoverRadius: 6,
        borderWidth: 2
      },
      line: {
        borderWidth: 3
      }
    }
  };

  // Component state
  loading = false;
  errorMessage = '';
  lastUpdated: Date | null = null;

  // Data
  revenueReport: RevenueReport | null = null;
  revenueSummary: RevenueSummary | null = null;

  // Filters
  selectedReportType = '6months';
  selectedMonth: number | null = null;
  selectedYear: number | null = null;
  customStartDate = '';
  customEndDate = '';

  // Available options
  reportTypes = [
    { value: '15day', label: '15-Day Intervals' },
    { value: 'monthly', label: 'Monthly' },
    { value: '6months', label: 'Last 6 Months' },
    { value: '12months', label: 'Last 12 Months' }
  ];

  months = [
    { value: 1, label: 'January' },
    { value: 2, label: 'February' },
    { value: 3, label: 'March' },
    { value: 4, label: 'April' },
    { value: 5, label: 'May' },
    { value: 6, label: 'June' },
    { value: 7, label: 'July' },
    { value: 8, label: 'August' },
    { value: 9, label: 'September' },
    { value: 10, label: 'October' },
    { value: 11, label: 'November' },
    { value: 12, label: 'December' }
  ];

  years: number[] = [];
  currentYear = new Date().getFullYear();

  // View modes
  viewMode: 'chart' | 'table' = 'chart';
  showFilters = false;

  private destroy$ = new Subject<void>();

  constructor(
    private revenueService: RevenueService,
    private toastr: ToastrService
  ) {
    // Generate years (current year and previous 5 years)
    for (let i = 0; i < 6; i++) {
      this.years.push(this.currentYear - i);
    }
  }

  ngOnInit(): void {
    this.loadRevenueSummary();
    this.loadRevenueReport();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadRevenueSummary(): void {
    this.revenueService.getRevenueSummary('30days')
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (summary) => {
          this.revenueSummary = summary;
        },
        error: (error) => {
          console.error('Error loading revenue summary:', error);
        }
      });
  }

  loadRevenueReport(): void {
    this.loading = true;
    this.errorMessage = '';

    const params: any = {
      reportType: this.selectedReportType
    };

    if (this.selectedReportType === 'monthly' && this.selectedMonth && this.selectedYear) {
      params.month = this.selectedMonth;
      params.year = this.selectedYear;
    }

    if (this.customStartDate && this.customEndDate) {
      params.startDate = this.customStartDate;
      params.endDate = this.customEndDate;
    }

    this.revenueService.getRevenueReport(params)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (report) => {
          this.revenueReport = report;
          this.updateChartData();
          this.lastUpdated = new Date();
          this.loading = false;
        },
        error: (error) => {
          this.errorMessage = 'Failed to load revenue report. Please try again.';
          this.loading = false;
          this.toastr.error(this.errorMessage);
        }
      });
  }

  updateChartData(): void {
    if (!this.revenueReport || !this.revenueReport.periods.length) {
      this.chartData = [];
      this.chartLabels = [];
      return;
    }

    this.chartData = [this.getChartData()];
    this.chartLabels = this.revenueReport.periods.map(p => p.periodLabel);
  }

  onReportTypeChange(): void {
    this.loadRevenueReport();
  }

  onMonthYearChange(): void {
    if (this.selectedReportType === 'monthly') {
      this.loadRevenueReport();
    }
  }

  onCustomDateChange(): void {
    if (this.customStartDate && this.customEndDate) {
      this.loadRevenueReport();
    }
  }

  toggleFilters(): void {
    this.showFilters = !this.showFilters;
  }

  resetFilters(): void {
    this.selectedReportType = '15day';
    this.selectedMonth = null;
    this.selectedYear = null;
    this.customStartDate = '';
    this.customEndDate = '';
    this.loadRevenueReport();
  }

  exportToCSV(): void {
    if (!this.revenueReport) return;

    const csvContent = this.generateCSV();
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `Revenue-Report-${this.selectedReportType}-${new Date().toISOString().split('T')[0]}.csv`;
    link.click();
    window.URL.revokeObjectURL(url);
    
    this.toastr.success('Revenue report exported successfully!', 'Export Complete');
  }

  private generateCSV(): string {
    if (!this.revenueReport) return '';

    const lines: string[] = [];
    
    // Report Header
    lines.push('REVENUE REPORT');
    lines.push(`Report Type: ${this.selectedReportType.toUpperCase()}`);
    lines.push(`Generated: ${new Date().toLocaleString()}`);
    lines.push(`Period: ${this.formatDateForCSV(this.revenueReport.reportStartDate)} - ${this.formatDateForCSV(this.revenueReport.reportEndDate)}`);
    lines.push(''); // Empty line
    
    // Summary Section
    lines.push('SUMMARY');
    lines.push(`Total Revenue: ₹${this.revenueReport.totalRevenue.toFixed(2)}`);
    lines.push(`Total Orders: ${this.revenueReport.totalOrders}`);
    lines.push(`Average Order Value: ₹${this.getAverageOrderValue().toFixed(2)}`);
    lines.push(''); // Empty line
    
    // Data Headers
    const headers = ['Period', 'Start Date', 'End Date', 'Revenue (₹)', 'Orders', 'Average Order Value (₹)'];
    lines.push(this.escapeCSVRow(headers));
    
    // Data Rows
    this.revenueReport.periods.forEach(period => {
      const row = [
        period.periodLabel,
        this.formatDateForCSV(period.startDate),
        this.formatDateForCSV(period.endDate),
        period.revenue.toFixed(2),
        period.orderCount.toString(),
        period.averageOrderValue.toFixed(2)
      ];
      lines.push(this.escapeCSVRow(row));
    });
    
    // Empty line
    lines.push('');
    
    // Total Row
    const totalRow = [
      'TOTAL',
      this.formatDateForCSV(this.revenueReport.reportStartDate),
      this.formatDateForCSV(this.revenueReport.reportEndDate),
      this.revenueReport.totalRevenue.toFixed(2),
      this.revenueReport.totalOrders.toString(),
      this.getAverageOrderValue().toFixed(2)
    ];
    lines.push(this.escapeCSVRow(totalRow));
    
    return lines.join('\n');
  }

  private escapeCSVRow(row: string[]): string {
    return row.map(cell => {
      // Escape quotes and wrap in quotes if contains comma, quote, or newline
      if (cell.includes(',') || cell.includes('"') || cell.includes('\n')) {
        return `"${cell.replace(/"/g, '""')}"`;
      }
      return cell;
    }).join(',');
  }

  private formatDateForCSV(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit'
    });
  }

  getTotalRevenue(): number {
    return this.revenueReport?.totalRevenue || 0;
  }

  getTotalOrders(): number {
    return this.revenueReport?.totalOrders || 0;
  }

  getAverageOrderValue(): number {
    return this.getTotalOrders() > 0 ? this.getTotalRevenue() / this.getTotalOrders() : 0;
  }

  private createGradient(): string {
    // For now, return a solid color. In a real implementation, you'd create a canvas gradient
    return 'rgba(102, 126, 234, 0.1)';
  }

  onChartTypeChange(type: 'line' | 'bar'): void {
    this.chartType = type;
    this.updateChartData();
  }

  private getChartData(): any {
    const data = this.revenueReport?.periods.map(p => p.revenue) || [];
    
    if (this.chartType === 'bar') {
      return {
        label: 'Revenue',
        data: data,
        backgroundColor: 'rgba(102, 126, 234, 0.8)',
        borderColor: '#667eea',
        borderWidth: 2,
        borderRadius: 4,
        borderSkipped: false
      };
    }
    
    return {
      label: 'Revenue',
      data: data,
      borderColor: '#667eea',
      backgroundColor: this.createGradient(),
      tension: 0.4,
      fill: true,
      pointBackgroundColor: '#667eea',
      pointBorderColor: '#ffffff',
      pointHoverBackgroundColor: '#5a67d8',
      pointHoverBorderColor: '#ffffff',
      pointHoverBorderWidth: 3
    };
  }
}

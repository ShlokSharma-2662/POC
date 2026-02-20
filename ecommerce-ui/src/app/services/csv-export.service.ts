import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class CsvExportService {

  constructor() { }

  /**
   * Exports data to CSV format and triggers download
   * @param data Array of objects to export
   * @param filename Name of the file to download
   * @param headers Optional custom headers mapping
   * @param metadata Optional metadata to include in the CSV
   */
  exportToCsv<T>(data: T[], filename: string, headers?: { [key: string]: string }, metadata?: any): void {
    if (!data || data.length === 0) {
      console.warn('No data to export');
      return;
    }

    // Get headers from first object or use provided headers
    const csvHeaders = headers || this.getHeadersFromData(data[0]);
    
    // Convert data to CSV format with metadata
    const csvContent = this.convertToCsvWithMetadata(data, csvHeaders, metadata);
    
    // Create and trigger download
    this.downloadFile(csvContent, filename);
  }

  /**
   * Gets headers from the first object in the data array
   */
  private getHeadersFromData(obj: any): { [key: string]: string } {
    const headers: { [key: string]: string } = {};
    for (const key in obj) {
      if (obj.hasOwnProperty(key)) {
        // Convert camelCase to Title Case
        headers[key] = this.toTitleCase(key);
      }
    }
    return headers;
  }

  /**
   * Converts camelCase string to Title Case
   */
  private toTitleCase(str: string): string {
    return str
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, str => str.toUpperCase())
      .trim();
  }

  /**
   * Converts array of objects to CSV string with metadata
   */
  private convertToCsvWithMetadata<T>(data: T[], headers: { [key: string]: string }, metadata?: any): string {
    const headerKeys = Object.keys(headers);
    const headerValues = headerKeys.map(key => headers[key]);
    
    const csvRows: string[] = [];
    
    // Add metadata header if provided
    if (metadata) {
      csvRows.push('# EXPORT REPORT');
      csvRows.push(`# Generated: ${new Date().toLocaleString()}`);
      csvRows.push(`# Total Records: ${data.length}`);
      if (metadata.title) csvRows.push(`# Report: ${metadata.title}`);
      if (metadata.description) csvRows.push(`# Description: ${metadata.description}`);
      csvRows.push('#');
      csvRows.push('');
    }
    
    // Create header row
    csvRows.push(this.escapeCsvRow(headerValues));
    
    // Create data rows
    data.forEach((item, index) => {
      const row = headerKeys.map(key => {
        const value = this.getNestedValue(item, key);
        return this.formatValue(value);
      });
      csvRows.push(this.escapeCsvRow(row));
    });
    
    return csvRows.join('\n');
  }

  /**
   * Converts array of objects to CSV string (legacy method)
   */
  private convertToCsv<T>(data: T[], headers: { [key: string]: string }): string {
    return this.convertToCsvWithMetadata(data, headers);
  }

  /**
   * Gets nested value from object using dot notation
   */
  private getNestedValue(obj: any, path: string): any {
    return path.split('.').reduce((current, key) => {
      return current && current[key] !== undefined ? current[key] : '';
    }, obj);
  }

  /**
   * Formats value for CSV (handles dates, numbers, etc.)
   */
  private formatValue(value: any): string {
    if (value === null || value === undefined) {
      return '';
    }
    
    if (value instanceof Date) {
      return value.toLocaleDateString();
    }
    
    if (typeof value === 'number') {
      return value.toString();
    }
    
    if (typeof value === 'boolean') {
      return value ? 'Yes' : 'No';
    }
    
    return String(value);
  }

  /**
   * Escapes CSV row values
   */
  private escapeCsvRow(values: string[]): string {
    return values.map(value => {
      // Escape quotes and wrap in quotes if contains comma, quote, or newline
      const escaped = value.replace(/"/g, '""');
      if (escaped.includes(',') || escaped.includes('"') || escaped.includes('\n')) {
        return `"${escaped}"`;
      }
      return escaped;
    }).join(',');
  }

  /**
   * Creates and triggers file download
   */
  private downloadFile(content: string, filename: string): void {
    const blob = new Blob([content], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    
    if (link.download !== undefined) {
      const url = URL.createObjectURL(blob);
      link.setAttribute('href', url);
      link.setAttribute('download', filename);
      link.style.visibility = 'hidden';
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    }
  }

  /**
   * Exports products to CSV with custom formatting
   */
  exportProducts(products: any[], filename: string = 'products.csv'): void {
    const headers = {
      'productId': 'Product ID',
      'name': 'Product Name',
      'description': 'Description',
      'price': 'Price (USD)',
      'stock': 'Stock Quantity',
      'categoryName': 'Category',
      'imageUrl': 'Image URL',
      'status': 'Status',
      'createdAt': 'Created Date',
      'updatedAt': 'Last Updated'
    };

    // Transform data for better CSV formatting
    const transformedData = products.map(product => ({
      productId: product.productId,
      name: product.name || 'N/A',
      description: product.description || 'No description',
      price: product.price ? product.price.toFixed(2) : '0.00',
      stock: product.stock || 0,
      categoryName: product.categoryName || 'Uncategorized',
      imageUrl: product.imageUrl || 'No image',
      status: product.isDeleted ? 'Deleted' : 'Active',
      createdAt: product.createdAt ? new Date(product.createdAt).toLocaleDateString() : 'N/A',
      updatedAt: product.updatedAt ? new Date(product.updatedAt).toLocaleDateString() : 'N/A'
    }));

    const metadata = {
      title: 'Products Export Report',
      description: 'Complete product catalog with pricing, inventory, and status information'
    };

    this.exportToCsv(transformedData, filename, headers, metadata);
  }

  /**
   * Exports orders to CSV with custom formatting
   */
  exportOrders(orders: any[], filename: string = 'orders.csv'): void {
    const headers = {
      'id': 'Order ID',
      'orderNumber': 'Order Number',
      'customerName': 'Customer Name',
      'phone': 'Phone Number',
      'shippingAddress': 'Shipping Address',
      'status': 'Order Status',
      'createdAt': 'Order Date',
      'itemsCount': 'Items Count',
      'totalAmount': 'Total Amount (USD)',
      'itemsDetails': 'Items Details'
    };

    // Transform data for better CSV formatting
    const transformedData = orders.map(order => {
      const itemsCount = order.items ? order.items.length : 0;
      const totalAmount = order.items ? 
        order.items.reduce((total: number, item: any) => total + (item.price * item.quantity), 0) : 0;
      
      // Create detailed items description
      const itemsDetails = order.items ? 
        order.items.map((item: any) => 
          `${item.productName} (Qty: ${item.quantity}, Price: ₹${item.price.toFixed(2)})`
        ).join('; ') : 'No items';

      return {
        id: order.id,
        orderNumber: order.orderNumber || order.id,
        customerName: order.customerName || 'N/A',
        phone: order.phone || 'N/A',
        shippingAddress: order.shippingAddress || 'N/A',
        status: order.status || 'Unknown',
        createdAt: order.createdAt ? new Date(order.createdAt).toLocaleDateString() : 'N/A',
        itemsCount: itemsCount,
        totalAmount: totalAmount.toFixed(2),
        itemsDetails: itemsDetails
      };
    });

    const metadata = {
      title: 'Orders Export Report',
      description: 'Complete order history with customer details, items, and financial information'
    };

    this.exportToCsv(transformedData, filename, headers, metadata);
  }

  /**
   * Exports users to CSV with custom formatting
   */
  exportUsers(users: any[], filename: string = 'users.csv'): void {
    const headers = {
      'id': 'User ID',
      'username': 'Username',
      'email': 'Email Address',
      'firstName': 'First Name',
      'lastName': 'Last Name',
      'role': 'User Role',
      'status': 'Account Status',
      'createdAt': 'Registration Date',
      'lastLoginAt': 'Last Login',
      'isEmailConfirmed': 'Email Confirmed'
    };

    // Transform data for better CSV formatting
    const transformedData = users.map(user => ({
      id: user.id,
      username: user.username || 'N/A',
      email: user.email || 'N/A',
      firstName: user.firstName || 'N/A',
      lastName: user.lastName || 'N/A',
      role: user.role || 'User',
      status: user.status || 'Unknown',
      createdAt: user.createdAt ? new Date(user.createdAt).toLocaleDateString() : 'N/A',
      lastLoginAt: user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleDateString() : 'Never',
      isEmailConfirmed: user.isEmailConfirmed ? 'Yes' : 'No'
    }));

    const metadata = {
      title: 'Users Export Report',
      description: 'Complete user accounts with roles, status, and activity information'
    };

    this.exportToCsv(transformedData, filename, headers, metadata);
  }

  /**
   * Creates a comprehensive summary report combining all data
   */
  exportSummaryReport(products: any[], orders: any[], users: any[], filename: string = 'ecommerce_summary_report.csv'): void {
    const summaryData = [
      {
        section: 'PRODUCTS SUMMARY',
        totalProducts: products.length,
        activeProducts: products.filter(p => !p.isDeleted).length,
        deletedProducts: products.filter(p => p.isDeleted).length,
        totalValue: products.reduce((sum, p) => sum + (p.price * p.stock), 0).toFixed(2),
        averagePrice: products.length > 0 ? (products.reduce((sum, p) => sum + p.price, 0) / products.length).toFixed(2) : '0.00'
      },
      {
        section: 'ORDERS SUMMARY',
        totalOrders: orders.length,
        pendingOrders: orders.filter(o => o.status === 'Pending').length,
        shippedOrders: orders.filter(o => o.status === 'Shipped').length,
        deliveredOrders: orders.filter(o => o.status === 'Delivered').length,
        totalRevenue: orders.reduce((sum, o) => {
          const orderTotal = o.items ? o.items.reduce((total: number, item: any) => total + (item.price * item.quantity), 0) : 0;
          return sum + orderTotal;
        }, 0).toFixed(2)
      },
      {
        section: 'USERS SUMMARY',
        totalUsers: users.length,
        activeUsers: users.filter(u => u.status === 'Active').length,
        deactivatedUsers: users.filter(u => u.status === 'Deactivated').length,
        adminUsers: users.filter(u => u.role === 'Admin').length,
        regularUsers: users.filter(u => u.role === 'User').length
      }
    ];

    const headers = {
      'section': 'Report Section',
      'totalProducts': 'Total Products',
      'activeProducts': 'Active Products',
      'deletedProducts': 'Deleted Products',
      'totalValue': 'Total Inventory Value (USD)',
      'averagePrice': 'Average Price (USD)',
      'totalOrders': 'Total Orders',
      'pendingOrders': 'Pending Orders',
      'shippedOrders': 'Shipped Orders',
      'deliveredOrders': 'Delivered Orders',
      'totalRevenue': 'Total Revenue (USD)',
      'totalUsers': 'Total Users',
      'activeUsers': 'Active Users',
      'deactivatedUsers': 'Deactivated Users',
      'adminUsers': 'Admin Users',
      'regularUsers': 'Regular Users'
    };

    const metadata = {
      title: 'E-Commerce Summary Report',
      description: 'Comprehensive overview of products, orders, and users with key metrics and statistics'
    };

    this.exportToCsv(summaryData, filename, headers, metadata);
  }

  /**
   * Formats currency values consistently
   */
  private formatCurrency(value: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'INR'
    }).format(value);
  }

  /**
   * Formats dates consistently
   */
  private formatDate(date: string | Date): string {
    const dateObj = typeof date === 'string' ? new Date(date) : date;
    return dateObj.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: '2-digit'
    });
  }
}

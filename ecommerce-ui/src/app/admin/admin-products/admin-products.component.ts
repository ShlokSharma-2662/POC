import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Product } from '../../models/product';
import { ProductService } from '../../services/product.service';
import { CategoryService, Category } from '../../services/category.service';
import { ToastrService } from 'ngx-toastr';
import { CsvExportService } from '../../services/csv-export.service';

import { AdminFooterComponent } from '../../admin-footer/admin-footer.component';
import { LoaderComponent } from '../../shared/loader/loader.component';

@Component({
  selector: 'app-admin-products',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, AdminFooterComponent, LoaderComponent],
  templateUrl: './admin-products.component.html',
  styleUrls: ['./admin-products.component.scss']
})
export class AdminProductsComponent implements OnInit {
  products: Product[] = [];
  categories: Category[] = [];
  totalCount = 0;
  pageNumber = 1;
  pageSize = 10;
  isDeletedFilter: string = 'all';
  search = '';

  editing: Product | null = null;
  draft: Partial<Product> = {};
  selectedFile: File | null = null;
  imagePreviewUrl: string | null = null;
  confirming: { product: Product; action: 'delete' | 'restore' } | null = null;
  Math = Math;

  loading = false;
  error: string | null = null;

  constructor(
    private productService: ProductService,
    private categoryService: CategoryService,
    private toastr: ToastrService,
    private csvExportService: CsvExportService
  ) {}

  ngOnInit(): void {
    this.load();
    this.loadCategories();
  }

  private getIsDeletedParam(): boolean | undefined {
    if (this.isDeletedFilter === 'active') return false;
    if (this.isDeletedFilter === 'deleted') return true;
    return undefined;
  }

  loadCategories(): void {
    this.categoryService.getCategories().subscribe({
      next: (categories) => {
        this.categories = categories || [];
      },
      error: (error) => {
        this.categories = [];
      }
    });
  }

  getCategoryName(categoryId: number): string {
    const category = this.categories.find(c => c.id === categoryId);
    return category ? category.name : 'Unknown Category';
  }

  load(): void {
    this.loading = true;
    this.error = null;
    this.productService
      .getAllProductsAdmin(this.pageNumber, this.pageSize, {
        isDeleted: this.getIsDeletedParam(),
        search: this.search || undefined,
      })
      .subscribe({
        next: (res) => {
          this.products = res.items;
          this.totalCount = res.totalCount;
          this.loading = false;
        },
        error: () => {
          this.error = 'Something went wrong while loading products.';
          this.loading = false;
        }
      });
  }

  changePage(delta: number) {
    const newPage = this.pageNumber + delta;
    const totalPages = Math.ceil(this.totalCount / this.pageSize) || 1;
    if (newPage < 1 || newPage > totalPages) return;
    this.pageNumber = newPage;
    this.load();
  }

  clearFilters() {
    this.search = '';
    this.isDeletedFilter = 'all';
    this.pageNumber = 1;
    this.load();
  }

  startCreate() {
    this.editing = null;
    this.draft = { 
      name: '', 
      description: '', 
      price: 0, 
      stock: 0, 
      imageUrl: '', 
      categoryId: this.categories.length > 0 ? this.categories[0].id : 0 
    };
    this.selectedFile = null;
    this.imagePreviewUrl = null;
  }

  startEdit(p: Product) {
    this.editing = p;
    this.draft = { ...p };
    this.selectedFile = null;
    this.imagePreviewUrl = p.imageUrl || null;
  }

  cancelEdit() {
    this.editing = null;
    this.draft = {};
    this.selectedFile = null;
    this.imagePreviewUrl = null;
  }

  saveDraft() {
    if (this.editing) {
      // EDIT MODE - Optimistic update for existing product
      const updated: Product = {
        ...this.editing,
        ...this.draft,
      } as Product;
      
      // Store original product for rollback
      const originalProduct = { ...this.editing };
      
      // Optimistic update - update UI immediately
      const index = this.products.findIndex(p => p.productId === this.editing!.productId);
      if (index !== -1) {
        this.products[index] = { ...this.products[index], ...updated };
      }
      
      if (this.selectedFile) {
        const formData = new FormData();
        formData.append('productId', String(this.editing.productId));
        formData.append('name', this.draft.name || '');
        formData.append('description', this.draft.description || '');
        formData.append('price', String(this.draft.price ?? 0));
        formData.append('stock', String(this.draft.stock ?? 0));
        formData.append('categoryId', String(this.draft.categoryId ?? 0));
        if (this.draft.imageUrl) formData.append('imageUrl', this.draft.imageUrl);
        formData.append('image', this.selectedFile);
        
        this.productService.updateProductWithImage(this.editing.productId, formData).subscribe({
          next: (res) => {
            console.log('Product update with image response:', res); // Debug log
            this.cancelEdit();
            this.toastr.success('Product updated successfully!', 'Success');
            // Refresh the list to ensure UI shows updated data
            this.load();
          },
          error: (err) => {
            console.error('Product update with image error:', err);
            // Rollback on error
            if (index !== -1) {
              this.products[index] = originalProduct;
            }
            this.toastr.error(`Failed to update product: ${err.message}`, 'Error');
          }
        });
      } else {
        // Create proper update payload with only required fields
        const updatePayload = {
          productId: updated.productId,
          name: updated.name,
          description: updated.description,
          price: updated.price,
          imageUrl: updated.imageUrl,
          stock: updated.stock,
          categoryId: updated.categoryId
        };
        
        this.productService.updateProduct(updatePayload).subscribe({
          next: (res) => {
            console.log('Product update response:', res); // Debug log
            this.cancelEdit();
            this.toastr.success('Product updated successfully!', 'Success');
            // Refresh the list to ensure UI shows updated data
            this.load();
          },
          error: (err) => {
            console.error('Product update error:', err);
            // Rollback on error
            if (index !== -1) {
              this.products[index] = originalProduct;
            }
            this.toastr.error(`Failed to update product: ${err.message}`, 'Error');
          }
        });
      }
    } else {
      // CREATE MODE - No optimistic update needed for new products
      if (this.selectedFile) {
        const formData = new FormData();
        formData.append('name', this.draft.name || '');
        formData.append('description', this.draft.description || '');
        formData.append('price', String(this.draft.price ?? 0));
        formData.append('stock', String(this.draft.stock ?? 0));
        formData.append('categoryId', String(this.draft.categoryId ?? 0));
        formData.append('image', this.selectedFile);
        
        this.productService.createProductWithImage(formData).subscribe({
          next: () => {
            this.cancelEdit();
            this.toastr.success('Product created successfully!', 'Success');
            this.load();
          },
          error: (err) => {
            this.toastr.error('Failed to create product. Please try again.', 'Error');
          }
        });
      } else {
        this.productService.createProduct(this.draft).subscribe({
          next: () => {
            this.cancelEdit();
            this.toastr.success('Product created successfully!', 'Success');
            this.load();
          },
          error: (err) => {
            this.toastr.error('Failed to create product. Please try again.', 'Error');
          }
        });
      }
    }
  }

  softDelete(p: Product) {
    // Store original state for rollback
    const originalIsDeleted = p.isDeleted;
    
    // Optimistic update - update UI immediately
    const index = this.products.findIndex(product => product.productId === p.productId);
    if (index !== -1) {
      this.products[index].isDeleted = true;
    }
    
    this.productService.softDeleteProduct(p.productId).subscribe({
      next: (res) => {
        console.log('Product delete response:', res); // Debug log
        this.toastr.success('Product deleted successfully!', 'Success');
        // Refresh the list to ensure UI shows updated data
        this.load();
      },
      error: (err) => {
        console.error('Product delete error:', err);
        // Rollback on error
        if (index !== -1) {
          this.products[index].isDeleted = originalIsDeleted;
        }
        this.toastr.error(`Failed to delete product: ${err.message}`, 'Error');
      }
    });
  }

  restore(p: Product) {
    // Store original state for rollback
    const originalIsDeleted = p.isDeleted;
    
    // Optimistic update - update UI immediately
    const index = this.products.findIndex(product => product.productId === p.productId);
    if (index !== -1) {
      this.products[index].isDeleted = false;
    }
    
    this.productService.restoreProduct(p.productId).subscribe({
      next: (res) => {
        console.log('Product restore response:', res); // Debug log
        this.toastr.success('Product restored successfully!', 'Success');
        // Refresh the list to ensure UI shows updated data
        this.load();
      },
      error: (err) => {
        console.error('Product restore error:', err);
        // Rollback on error
        if (index !== -1) {
          this.products[index].isDeleted = originalIsDeleted;
        }
        this.toastr.error(`Failed to restore product: ${err.message}`, 'Error');
      }
    });
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) {
      this.selectedFile = null;
      return;
    }
    const file = input.files[0];
    this.selectedFile = file;
    const reader = new FileReader();
    reader.onload = () => {
      this.imagePreviewUrl = reader.result as string;
    };
    reader.readAsDataURL(file);
  }

  deleteOrRestore(p: Product) {
    if (p.isDeleted) {
      this.restore(p);
    } else {
      this.softDelete(p);
    }
  }

  openConfirm(p: Product, action: 'delete' | 'restore') {
    this.confirming = { product: p, action };
  }

  confirmProceed() {
    if (!this.confirming) return;
    const { product, action } = this.confirming;
    if (action === 'delete') {
      this.softDelete(product);
    } else {
      this.restore(product);
    }
    this.confirming = null;
  }

  // Method to refresh list instantly
  refreshList() {
    this.load();
  }

  // Method to refresh list after successful operations
  private refreshAfterSuccess() {
    // Small delay to ensure server has processed the request
    setTimeout(() => {
      this.load();
    }, 500);
  }

  // CSV Export functionality
  exportToCsv(): void {
    if (!this.products || this.products.length === 0) {
      this.toastr.warning('No products to export', 'Export Warning');
      return;
    }

    try {
      const timestamp = new Date().toISOString().split('T')[0];
      const filename = `products_export_${timestamp}.csv`;
      
      // Transform products to include category names and additional metadata
      const productsWithCategoryNames = this.products.map(product => ({
        ...product,
        categoryName: this.getCategoryName(product.categoryId),
        createdAt: (product as any).createdAt || new Date().toISOString(),
        updatedAt: (product as any).updatedAt || new Date().toISOString()
      }));

      this.csvExportService.exportProducts(productsWithCategoryNames, filename);
      this.toastr.success(`Exported ${this.products.length} products successfully!`, 'Export Complete');
    } catch (error) {
      console.error('Export error:', error);
      this.toastr.error('Failed to export products', 'Export Error');
    }
  }
}




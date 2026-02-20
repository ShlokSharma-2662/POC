import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { ProductService, PagedResult } from '../services/product.service';
import { Product } from '../models/product';
import { CommonModule } from '@angular/common';
import { ProductDetailComponent } from '../product-detail/product-detail.component';
import { FormsModule } from '@angular/forms';
import { CartService } from '../services/cart.service';
import { WishlistService } from '../services/wishlist.service';
import { ToastrService } from 'ngx-toastr';
import { ActivatedRoute, Router } from '@angular/router';
import { CategoryService, Category } from '../services/category.service';
import { finalize, debounceTime, distinctUntilChanged } from 'rxjs';
import { LoaderComponent } from '../shared/loader/loader.component';
import { AuthService } from '../auth.service';
import { Subject } from 'rxjs';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [CommonModule, ProductDetailComponent, FormsModule, LoaderComponent],
  templateUrl: './products.component.html',
  styleUrls: ['./products.component.scss'],
})
export class ProductsComponent implements OnInit, OnDestroy {
  products: Product[] = [];
  categoriesLst: Category[] = [];
  selectedProduct!: Product;

  // Pagination
  currentPage = 1;
  pageSize = 9;
  totalCount = 0;
  totalPages = 0;

  // Filters
  searchText: string = '';
  selectedCategory: number = 0;
  minPrice: number | null = null;
  maxPrice: number | null = null;
  categoryName: string = '';
  showAdvancedSearch: boolean = false;
  
  // UI State
  loading: boolean = false;
  currentSlide: number = 0;
  private slideInterval: any;
  private searchSubject = new Subject<string>();
  Math = Math; // Make Math available in template

  @ViewChild('detailModal') detailModal!: ProductDetailComponent;

  constructor(
    private productService: ProductService,
    private cartService: CartService,
    private wishlistService: WishlistService,
    private toastr: ToastrService,
    private route: ActivatedRoute,
    private categoryService: CategoryService,
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      const categoryId = +params['category'] || 0;
      this.selectedCategory = categoryId;
      this.currentPage = 1;
      
      this.loadCategories();
      this.loadProducts();
    });

    // Setup search debouncing
    this.searchSubject.pipe(
      debounceTime(500),
      distinctUntilChanged()
    ).subscribe(() => {
      this.currentPage = 1;
      this.loadProducts();
    });

    this.startAutoSlide();
    this.loadWishlistStatuses();
  }

  ngOnDestroy(): void {
    if (this.slideInterval) {
      clearInterval(this.slideInterval);
    }
  }

  // Load products with server-side pagination
  loadProducts(): void {
    
    this.loading = true;
    this.productService.getProducts(
      this.currentPage, 
      this.pageSize, 
      this.selectedCategory, 
      this.searchText,
      this.minPrice ?? undefined,
      this.maxPrice ?? undefined,
      this.categoryName
    )
    .pipe(finalize(() => this.loading = false))
    .subscribe({
      next: (result: PagedResult<Product>) => {
        
        if (result && result.items) {
          this.products = result.items.map(p => ({ ...p, quantity: 1 }));
          this.totalCount = result.totalCount;
          this.totalPages = Math.ceil(this.totalCount / this.pageSize);
        } else {
          this.products = [];
          this.totalCount = 0;
          this.totalPages = 0;
        }
      },
      error: (err) => {
        this.toastr.error('Failed to load products. Please try again.');
      }
    });
  }

  // Pagination methods
  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.loadProducts();
    }
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadProducts();
    }
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.loadProducts();
    }
  }

  // Search and filter methods
  onSearchChange(): void {
    this.searchSubject.next(this.searchText);
  }

  filterByCategory(categoryName: string): void {
    // Find category ID by name
    const category = this.categoriesLst.find(c => c.name === categoryName);
    if (category) {
      this.selectedCategory = category.id;
      this.currentPage = 1;
      this.loadProducts();
    }
  }

  goToSlide(index: number): void {
    this.currentSlide = index;
  }

  previousSlide(): void {
    this.currentSlide = this.currentSlide > 0 ? this.currentSlide - 1 : 3;
  }

  nextSlide(): void {
    this.currentSlide = this.currentSlide < 3 ? this.currentSlide + 1 : 0;
  }

  clearSearch(): void {
    this.searchText = '';
    this.currentPage = 1;
    this.loadProducts();
  }

  onCategoryChange(): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { category: this.selectedCategory },
      queryParamsHandling: 'merge',
    });
    this.currentPage = 1;
    this.loadProducts();
  }

  resetFilters(): void {
    this.searchText = '';
    this.selectedCategory = 0;
    this.minPrice = null;
    this.maxPrice = null;
    this.categoryName = '';
    this.showAdvancedSearch = false;
    this.currentPage = 1;
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { category: 0 },
      queryParamsHandling: 'merge',
    });
    this.loadProducts();
  }

  // Advanced search methods
  toggleAdvancedSearch(): void {
    this.showAdvancedSearch = !this.showAdvancedSearch;
  }

  onPriceRangeChange(): void {
    this.currentPage = 1;
    this.loadProducts();
  }

  onCategoryNameChange(): void {
    this.currentPage = 1;
    this.loadProducts();
  }

  clearPriceFilters(): void {
    this.minPrice = null;
    this.maxPrice = null;
    this.currentPage = 1;
    this.loadProducts();
  }

  clearCategoryNameFilter(): void {
    this.categoryName = '';
    this.currentPage = 1;
    this.loadProducts();
  }

  // Open product detail modal
  openDetail(product: Product): void {
    this.selectedProduct = product;
    setTimeout(() => {
      this.detailModal.showModal();
    }, 0);
  }

  // Add product to the cart
  addProductToCart(product: Product): void {
    const quantity = product.quantity || 1;

    // Add the product to the cart with the selected quantity
    // The cart service will handle success/error notifications
    this.cartService.addToCart({ ...product, quantity });
  }

  // Increase product quantity in the cart
  increaseQty(product: Product): void {
    if (product.quantity < product.stock) {
      product.quantity++;
      this.cartService.updateCart({
        product: product,
        quantity: product.quantity,
        price: product.price,
      });
    }
  }

  // Decrease product quantity in the cart
  decreaseQty(product: Product): void {
    if (product.quantity > 1) {
      product.quantity--;
      this.cartService.updateCart({
        product: product,
        quantity: product.quantity,
        price: product.price,
      });
    }
  }

  // Add/remove from wishlist
  toggleWishlist(product: Product): void {
    if (product.isInWishlist) {
      this.wishlistService.removeFromWishlist(product.productId).subscribe({
        next: () => {
          product.isInWishlist = false;
          this.toastr.success('Removed from wishlist', 'Wishlist Updated');
        },
        error: (err) => {
          this.toastr.error('Failed to remove from wishlist');
        }
      });
    } else {
      this.wishlistService.addToWishlist(product.productId, product).subscribe({
        next: () => {
          product.isInWishlist = true;
          this.toastr.success('Added to wishlist', 'Wishlist Updated');
        },
        error: (err) => {
          this.toastr.error('Failed to add to wishlist');
        }
      });
    }
  }

  // Load categories
  loadCategories(): void {
    this.categoryService.getCategories().subscribe({
      next: (data) => {
        this.categoriesLst = data;
      },
      error: (err) => {
        // Silent fail for category loading
      }
    });
  }

  // Load wishlist statuses for all products
  loadWishlistStatuses(): void {
    if (!this.authService.isLoggedIn()) {
      // For guest users, check local storage
      this.products.forEach(product => {
        product.isInWishlist = this.wishlistService.getWishlistStatus(product.productId);
      });
      return;
    }

    this.wishlistService.getUserWishlist().subscribe({
      next: (wishlistItems: any[]) => {
        const wishlistProductIds = wishlistItems.map((item: any) => item.productId);
        this.products.forEach(product => {
          product.isInWishlist = wishlistProductIds.includes(product.productId);
        });
      },
      error: (err: any) => {
        // Silent fail for wishlist status loading
      }
    });
  }

  // Auto slide functionality
  startAutoSlide(): void {
    this.slideInterval = setInterval(() => {
      this.currentSlide = (this.currentSlide + 1) % 3; // Assuming 3 slides
    }, 5000);
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

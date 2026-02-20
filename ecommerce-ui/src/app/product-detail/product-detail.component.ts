import { Component, Input, HostListener } from '@angular/core';
import { Product } from '../models/product';
import { CommonModule } from '@angular/common';
import { CartService } from '../services/cart.service';
import { ToastrService } from 'ngx-toastr';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule,FormsModule],
  templateUrl: './product-detail.component.html',
  styleUrls: ['./product-detail.component.scss']
})
export class ProductDetailComponent {
  @Input() product!: Product;
  quantity: number = 1;
  isModalVisible: boolean = false;
  
  constructor(
    private cartService: CartService, 
    private toastr: ToastrService,
    private authService: AuthService,
    private router: Router
  ) { }

  showModal() {
    this.quantity = 1;
    this.isModalVisible = true;
    // Prevent body scroll when modal is open
    document.body.style.overflow = 'hidden';
  }
  

  addToCart(): void {
    const qty = this.quantity || 1;
    //this.cartService.addToCart(this.product, qty);
    this.cartService.addToCart(this.product);
    
    // Close modal
    this.closeModal();
    this.quantity = 1; // Reset quantity
  }
  
  increaseQty(): void {
    if (this.quantity < this.product.stock) {
      this.quantity++;
    }
  }
  
  decreaseQty(): void {
    if (this.quantity > 1) {
      this.quantity--;
    }
  }
  
  validateQuantity(): void {
    if (this.quantity < 1) {
      this.quantity = 1;
    } else if (this.quantity > this.product.stock) {
      this.quantity = 1;
    }
  }

  closeModal(): void {
    this.isModalVisible = false;
    // Restore body scroll
    document.body.style.overflow = 'auto';
  }

  // Handle backdrop click
  onBackdropClick(event: Event): void {
    if (event.target === event.currentTarget) {
      this.closeModal();
    }
  }

  // Handle escape key
  @HostListener('document:keydown.escape', ['$event'])
  onEscapeKey(event: KeyboardEvent): void {
    if (this.isModalVisible) {
      this.closeModal();
    }
  }
}

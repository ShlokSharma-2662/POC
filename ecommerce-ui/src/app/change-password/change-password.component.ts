import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../auth.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './change-password.component.html',
  styleUrls: ['./change-password.component.scss']
})
export class ChangePasswordComponent implements OnInit {
  changePasswordData = {
    oldPassword: '',
    newPassword: '',
    confirmPassword: ''
  };

  showOldPassword = false;
  showNewPassword = false;
  showConfirmPassword = false;
  isLoading = false;

  constructor(
    private authService: AuthService,
    private router: Router,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    // Check if user is logged in
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/login']);
    }
  }

  togglePasswordVisibility(field: 'old' | 'new' | 'confirm'): void {
    switch (field) {
      case 'old':
        this.showOldPassword = !this.showOldPassword;
        break;
      case 'new':
        this.showNewPassword = !this.showNewPassword;
        break;
      case 'confirm':
        this.showConfirmPassword = !this.showConfirmPassword;
        break;
    }
  }

  onSubmit(): void {
    // Validate form
    if (!this.validateForm()) {
      return;
    }

    this.isLoading = true;

    this.authService.changePassword(this.changePasswordData).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.toastr.success('Password changed successfully!', 'Success');
        this.router.navigate(['/profile']);
      },
      error: (error) => {
        this.isLoading = false;
        const errorMessage = error.error?.message || 'Failed to change password. Please try again.';
        this.toastr.error(errorMessage, 'Error');
      }
    });
  }

  private validateForm(): boolean {
    if (!this.changePasswordData.oldPassword.trim()) {
      this.toastr.error('Please enter your current password', 'Validation Error');
      return false;
    }

    if (!this.changePasswordData.newPassword.trim()) {
      this.toastr.error('Please enter a new password', 'Validation Error');
      return false;
    }

    if (this.changePasswordData.newPassword.length < 8) {
      this.toastr.error('New password must be at least 8 characters long', 'Validation Error');
      return false;
    }

    if (this.changePasswordData.newPassword !== this.changePasswordData.confirmPassword) {
      this.toastr.error('New password and confirm password do not match', 'Validation Error');
      return false;
    }

    if (this.changePasswordData.oldPassword === this.changePasswordData.newPassword) {
      this.toastr.error('New password must be different from current password', 'Validation Error');
      return false;
    }

    return true;
  }

  onCancel(): void {
    this.router.navigate(['/profile']);
  }
}

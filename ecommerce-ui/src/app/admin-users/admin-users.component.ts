import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { AdminFooterComponent } from '../admin-footer/admin-footer.component';
import { AdminUsersService } from '../services/admin-users.service';
import { AdminUser } from '../models/admin-user.model';
import { LoaderComponent } from '../shared/loader/loader.component';
import { CsvExportService } from '../services/csv-export.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule, AdminFooterComponent, LoaderComponent],
  templateUrl: './admin-users.component.html',
  styleUrls: ['./admin-users.component.scss']
})
export class AdminUsersComponent implements OnInit {
  users: AdminUser[] = [];
  totalCount = 0;
  pageNumber = 1;
  pageSize = 10;
  searchTerm = '';
  statusFilter = '';
  roleFilter = '';
  loading = false;
  error = '';

  // modal state
  selectedUser: AdminUser | null = null;
  newRole = '';
  newPassword = '';
  successMessage = '';
  errorMessage = '';
  // 'none' | 'assign' | 'reset' | 'deactivate' | 'activate'
  modalMode: string = 'none';
  pendingUser: AdminUser | null = null;
  pendingRole: string = '';

  constructor(
    private adminUsers: AdminUsersService,
    private csvExportService: CsvExportService,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading = true;
    this.error = '';
    this.errorMessage = '';
    
    this.adminUsers.getUsers({
      pageNumber: this.pageNumber,
      pageSize: this.pageSize,
      searchTerm: this.searchTerm,
      status: this.statusFilter,
      role: this.roleFilter
    }).subscribe({
      next: res => { 
        this.users = res.items; 
        this.totalCount = res.totalCount; 
        this.loading = false;
      },
      error: err => { 
        this.errorMessage = 'Failed to load users.'; 
        this.error = 'Failed to load users. Please try again.';
        this.loading = false;
      }
    });
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.statusFilter = '';
    this.roleFilter = '';
    this.pageNumber = 1;
    this.loadUsers();
  }

  openAssignRole(user: AdminUser) { this.selectedUser = user; this.newRole = user.role; this.modalMode = 'assign'; }
  openResetPassword(user: AdminUser) { this.selectedUser = user; this.newPassword = ''; this.modalMode = 'reset'; }
  openDeactivate(user: AdminUser) { this.selectedUser = user; this.modalMode = 'deactivate'; }
  openActivate(user: AdminUser) { this.selectedUser = user; this.modalMode = 'activate'; }
  closeModals() { this.selectedUser = null; this.newRole = ''; this.newPassword = ''; this.modalMode = 'none'; }

  confirmAssignRole() {
    const targetUser = this.pendingUser ?? this.selectedUser;
    const targetRole = this.pendingRole || this.newRole;
    if (!targetUser || !targetRole) return;
    this.adminUsers.assignRole(targetUser.id, targetRole).subscribe({
      next: (res) => { 
        console.log('User assign role response:', res); // Debug log
        targetUser.role = targetRole; 
        this.successMessage = `Role changed to ${targetRole}.`; 
        this.closeAfterDelay();
        // Refresh the users list to ensure UI shows updated data
        this.loadUsers();
      },
      error: (err) => { 
        console.error('User assign role error:', err);
        this.errorMessage = 'Failed to assign role.'; 
      }
    });
  }

  confirmResetPassword() {
    if (!this.selectedUser) return;
    this.adminUsers.resetPassword(this.selectedUser.id, this.newPassword).subscribe({
      next: (res) => { 
        console.log('User reset password response:', res); // Debug log
        this.successMessage = 'Password reset successfully.'; 
        this.closeAfterDelay();
        // Refresh the users list to ensure UI shows updated data
        this.loadUsers();
      },
      error: (err) => { 
        console.error('User reset password error:', err);
        this.errorMessage = 'Failed to reset password.'; 
      }
    });
  }

  confirmDeactivate() {
    const targetUser = this.pendingUser ?? this.selectedUser;
    if (!targetUser) return;
    this.adminUsers.deactivate(targetUser.id).subscribe({
      next: (res) => { 
        console.log('User deactivate response:', res); // Debug log
        targetUser.status = 'Deactivated'; 
        this.successMessage = 'User deactivated.'; 
        this.closeAfterDelay();
        // Refresh the users list to ensure UI shows updated data
        this.loadUsers();
      },
      error: (err) => { 
        console.error('User deactivate error:', err);
        this.errorMessage = 'Failed to deactivate user.'; 
      }
    });
  }

  confirmActivate() {
    const targetUser = this.pendingUser ?? this.selectedUser;
    if (!targetUser) return;
    this.adminUsers.activate(targetUser.id).subscribe({
      next: (res) => { 
        console.log('User activate response:', res); // Debug log
        targetUser.status = 'Active'; 
        this.successMessage = 'User activated.'; 
        this.closeAfterDelay();
        // Refresh the users list to ensure UI shows updated data
        this.loadUsers();
      },
      error: (err) => { 
        console.error('User activate error:', err);
        this.errorMessage = 'Failed to activate user.'; 
      }
    });
  }

  nextPage() { if (this.pageNumber * this.pageSize < this.totalCount) { this.pageNumber++; this.loadUsers(); } }
  prevPage() { if (this.pageNumber > 1) { this.pageNumber--; this.loadUsers(); } }

  private closeAfterDelay() {
    setTimeout(() => { this.successMessage = ''; this.errorMessage = ''; this.closeModals(); }, 1200);
  }

  getMin(a: number, b: number): number { return Math.min(a, b); }

  // Toggle helpers (with confirmation)
  requestToggleRole(user: AdminUser) {
    if (user.status === 'Deactivated') return;
    this.pendingUser = user;
    this.pendingRole = user.role === 'Admin' ? 'User' : 'Admin';
    this.selectedUser = user; // ensure modal opens
    this.newRole = this.pendingRole; // preselect new role in dropdown
    this.modalMode = 'assign';
  }

  requestToggleStatus(user: AdminUser) {
    this.pendingUser = user;
    this.selectedUser = user; // ensure modal opens
    this.modalMode = user.status === 'Active' ? 'deactivate' : 'activate';
  }

  private autoClearMessage() {
    setTimeout(() => { this.successMessage = ''; this.errorMessage = ''; }, 1500);
  }

  getTotalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  // CSV Export functionality
  exportToCsv(): void {
    if (!this.users || this.users.length === 0) {
      this.toastr.warning('No users to export', 'Export Warning');
      return;
    }

    try {
      const timestamp = new Date().toISOString().split('T')[0];
      const filename = `users_export_${timestamp}.csv`;
      
      // Transform users to include additional metadata
      const usersWithMetadata = this.users.map(user => ({
        ...user,
        firstName: (user as any).firstName || '',
        lastName: (user as any).lastName || '',
        lastLoginAt: (user as any).lastLoginAt || null,
        isEmailConfirmed: (user as any).isEmailConfirmed || false
      }));

      this.csvExportService.exportUsers(usersWithMetadata, filename);
      this.toastr.success(`Exported ${this.users.length} users successfully!`, 'Export Complete');
    } catch (error) {
      console.error('Export error:', error);
      this.toastr.error('Failed to export users', 'Export Error');
    }
  }
}



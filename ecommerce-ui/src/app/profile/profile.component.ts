import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ProfileService, UserProfile } from '../services/profile.service';
import { LoaderComponent } from '../shared/loader/loader.component';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, RouterModule, LoaderComponent],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss']
})
export class ProfileComponent implements OnInit {
  profile: UserProfile | null = null;
  loading = false;
  error: string | null = null;

  constructor(
    private profileService: ProfileService,
    private toastr: ToastrService
  ) { }

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.loading = true;
    this.error = null;

    this.profileService.getUserProfile().subscribe({
      next: (profile) => {
        this.profile = profile;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load profile. Please try again.';
        this.loading = false;
        this.toastr.error('Failed to load profile', 'Error');
      }
    });
  }

  getFullName(): string {
    if (!this.profile) return '';
    return `${this.profile.firstName} ${this.profile.lastName}`;
  }

  getFormattedDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  }

  getRoleDisplayName(role: string): string {
    return role === 'Admin' ? 'Administrator' : 'Customer';
  }

  getStatusBadgeClass(status: string): string {
    return status === 'Active' ? 'badge-success' : 'badge-danger';
  }
}

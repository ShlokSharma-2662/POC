import { Component, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { CategoryService, Category } from '../services/category.service';
import { MonitoringService } from '../services/monitoring.service';
import { LoaderComponent } from '../shared/loader/loader.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterModule, LoaderComponent],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
})
export class HomeComponent implements OnInit {
  categories: Category[] = [];
  loading: boolean = true;

  constructor(
    private router: Router,
    private categoryService: CategoryService,
    private monitoring: MonitoringService
  ) {}

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.loading = true;
    const start = performance.now();
    
    this.categoryService.getCategories().subscribe({
      next: (categories: Category[]) => {
        this.categories = categories;
        this.loading = false;
        const responseTime = performance.now() - start;
        //this.monitoring.logMetric('/api/categories', responseTime);
      },
      error: (error: any) => {
        this.loading = false;
      }
    });
  }

  goToCategory(categoryId: number): void {
    this.router.navigate(['/products'], {
      queryParams: categoryId ? { category: categoryId } : {},
    });
  }

  getCategoryIcon(categoryName: string): string {
    const iconMap: { [key: string]: string } = {
      'Smartphones': 'fas fa-mobile-alt',
      'Laptops': 'fas fa-laptop',
      'Headphones': 'fas fa-headphones',
      'Smart Watches': 'fas fa-clock',
      'Tablets': 'fas fa-tablet-alt',
      'Cameras': 'fas fa-camera',
      'Gaming': 'fas fa-gamepad',
      'Audio': 'fas fa-music',
      'Accessories': 'fas fa-tools',
      'Wearables': 'fas fa-watch'
    };
    return iconMap[categoryName] || 'fas fa-box';
  }

  getCategoryDescription(categoryName: string): string {
    const descriptionMap: { [key: string]: string } = {
      'Smartphones': 'Latest mobile devices with cutting-edge technology',
      'Laptops': 'Powerful computing solutions for work and entertainment',
      'Headphones': 'Premium audio experience with crystal clear sound',
      'Smart Watches': 'Stay connected and track your fitness goals',
      'Tablets': 'Portable computing for productivity and entertainment',
      'Cameras': 'Capture life\'s moments with professional quality',
      'Gaming': 'Immerse yourself in the world of gaming',
      'Audio': 'High-quality sound systems and speakers',
      'Accessories': 'Essential add-ons to enhance your devices',
      'Wearables': 'Smart technology you can wear'
    };
    return descriptionMap[categoryName] || 'Quality products for every need';
  }
}

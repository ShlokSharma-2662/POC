import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-loader',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="loader-container" [class.full-page]="fullPage">
      <div class="loader-content">
        <div class="spinner"></div>
        <div class="loader-text" *ngIf="message">
          <h3>{{ message }}</h3>
          <p *ngIf="subMessage">{{ subMessage }}</p>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./loader.component.scss']
})
export class LoaderComponent {
  @Input() message: string = 'Loading...';
  @Input() subMessage: string = '';
  @Input() fullPage: boolean = false;
}

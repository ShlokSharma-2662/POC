import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription, interval } from 'rxjs';
import { RateLimitingService, RateLimitError } from '../../services/rate-limiting.service';

@Component({
  selector: 'app-rate-limit-notification',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div *ngIf="rateLimitError" class="rate-limit-notification">
      <div class="notification-content">
        <div class="notification-icon">
          <i class="fas fa-clock"></i>
        </div>
        <div class="notification-body">
          <h4>Rate Limit Exceeded</h4>
          <p>{{ rateLimitError.message }}</p>
          <div class="rate-limit-details">
            <span class="remaining">
              <i class="fas fa-chart-line"></i>
              {{ rateLimitError.remaining }}/{{ rateLimitError.limit }} requests remaining
            </span>
            <span class="countdown">
              <i class="fas fa-hourglass-half"></i>
              Retry in {{ timeUntilReset }}
            </span>
          </div>
        </div>
        <button class="close-btn" (click)="dismiss()" aria-label="Dismiss notification">
          <i class="fas fa-times"></i>
        </button>
      </div>
    </div>
  `,
  styles: [`
    .rate-limit-notification {
      position: fixed;
      top: 20px;
      right: 20px;
      z-index: var(--z-notification);
      max-width: 400px;
      animation: slideIn 0.3s ease-out;
    }

    .notification-content {
      background: linear-gradient(135deg, #ff6b6b, #ee5a52);
      color: white;
      border-radius: 12px;
      padding: 16px;
      box-shadow: 0 8px 32px rgba(255, 107, 107, 0.3);
      display: flex;
      align-items: flex-start;
      gap: 12px;
    }

    .notification-icon {
      font-size: 24px;
      margin-top: 2px;
    }

    .notification-body {
      flex: 1;
    }

    .notification-body h4 {
      margin: 0 0 8px 0;
      font-size: 16px;
      font-weight: 600;
    }

    .notification-body p {
      margin: 0 0 12px 0;
      font-size: 14px;
      opacity: 0.9;
    }

    .rate-limit-details {
      display: flex;
      flex-direction: column;
      gap: 6px;
      font-size: 12px;
    }

    .remaining, .countdown {
      display: flex;
      align-items: center;
      gap: 6px;
      opacity: 0.9;
    }

    .close-btn {
      background: none;
      border: none;
      color: white;
      font-size: 18px;
      cursor: pointer;
      padding: 4px;
      border-radius: 4px;
      transition: background-color 0.2s;
    }

    .close-btn:hover {
      background-color: rgba(255, 255, 255, 0.1);
    }

    @keyframes slideIn {
      from {
        transform: translateX(100%);
        opacity: 0;
      }
      to {
        transform: translateX(0);
        opacity: 1;
      }
    }

    @media (max-width: 480px) {
      .rate-limit-notification {
        left: 10px;
        right: 10px;
        max-width: none;
      }
    }
  `]
})
export class RateLimitNotificationComponent implements OnInit, OnDestroy {
  rateLimitError: RateLimitError | null = null;
  timeUntilReset = '';
  private subscription: Subscription = new Subscription();
  private countdownInterval?: Subscription;

  constructor(private rateLimitingService: RateLimitingService) {}

  ngOnInit(): void {
    // Subscribe to rate limit errors
    this.subscription.add(
      this.rateLimitingService.rateLimitError$.subscribe(error => {
        this.rateLimitError = error;
        if (error) {
          this.startCountdown();
        } else {
          this.stopCountdown();
        }
      })
    );
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
    this.stopCountdown();
  }

  private startCountdown(): void {
    this.updateCountdown();
    this.countdownInterval = interval(1000).subscribe(() => {
      this.updateCountdown();
    });
  }

  private stopCountdown(): void {
    if (this.countdownInterval) {
      this.countdownInterval.unsubscribe();
      this.countdownInterval = undefined;
    }
  }

  private updateCountdown(): void {
    if (this.rateLimitError) {
      this.timeUntilReset = this.rateLimitingService.getTimeUntilReset();
      
      // Auto-dismiss when countdown reaches 0
      if (this.timeUntilReset === 'Rate limit has been reset') {
        this.dismiss();
      }
    }
  }

  dismiss(): void {
    this.rateLimitingService.clearRateLimitError();
  }
}

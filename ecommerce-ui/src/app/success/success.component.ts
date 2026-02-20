import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-success',
  standalone: true,
  templateUrl: './success.component.html',
  styleUrls: ['./success.component.scss'],
  imports: [CommonModule, RouterModule],
})
export class SuccessComponent implements OnInit {
  orderId: string | null = null;
  orderSummary: any = null;

  constructor(private route: ActivatedRoute) {} // ✅ inject route

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.orderId = params['orderId'];
    });
    try {
      const stored = localStorage.getItem('lastOrderSummary');
      this.orderSummary = stored ? JSON.parse(stored) : null;
    } catch {}
  }
}

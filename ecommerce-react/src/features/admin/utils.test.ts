import { describe, expect, it } from 'vitest';
import type { AdminOrder, SystemMetric } from './types';
import {
  aggregateEndpoints,
  buildQuery,
  metricsFromRange,
  orderTotal,
  toCsv,
  totalPages,
} from './utils';

describe('admin utilities', () => {
  it('builds query strings without blank optional filters', () => {
    expect(
      buildQuery({
        PageNumber: 2,
        SearchTerm: 'red shoes',
        Status: '',
        IsDeleted: false,
      }),
    ).toBe('?PageNumber=2&SearchTerm=red+shoes&IsDeleted=false');
  });

  it('calculates order totals and page counts', () => {
    const order: AdminOrder = {
      id: 'order-1',
      customerName: 'Ada',
      phone: '123',
      shippingAddress: 'Example street',
      createdAt: '2026-01-01T00:00:00Z',
      status: 'Pending',
      items: [
        {
          productId: 1,
          productName: 'Book',
          description: '',
          price: 125.5,
          quantity: 2,
        },
        {
          productId: 2,
          productName: 'Pen',
          description: '',
          price: 20,
          quantity: 3,
        },
      ],
    };

    expect(orderTotal(order)).toBe(311);
    expect(totalPages(21, 10)).toBe(3);
    expect(totalPages(0, 10)).toBe(1);
  });

  it('aggregates endpoint health from request metrics', () => {
    const metrics: SystemMetric[] = [
      metric(1, '/api/products', 100, 200, '2026-01-01T10:00:00Z'),
      metric(2, '/api/products', 300, 500, '2026-01-01T10:01:00Z'),
      metric(3, '/api/users', 50, 200, '2026-01-01T10:02:00Z'),
    ];

    const endpoints = aggregateEndpoints(metrics);
    expect(endpoints[0]).toMatchObject({
      name: '/api/products',
      avgResponseTime: 200,
      requestCount: 2,
      errorRate: 50,
      healthStatus: 'Critical',
    });
    expect(endpoints[1]).toMatchObject({
      name: '/api/users',
      healthStatus: 'Healthy',
    });
  });

  it('escapes commas, quotes, and new lines in CSV output', () => {
    const csv = toCsv(
      [{ name: 'A, "quoted"\nvalue', count: 2 }],
      [
        { label: 'Name', value: (row) => row.name },
        { label: 'Count', value: (row) => row.count },
      ],
    );

    expect(csv).toBe('Name,Count\r\n"A, ""quoted""\nvalue",2');
  });

  it('converts performance ranges to an ISO start timestamp', () => {
    const now = new Date('2026-07-28T12:00:00.000Z');
    expect(metricsFromRange('24h', now)).toBe('2026-07-27T12:00:00.000Z');
    expect(metricsFromRange('1h', now)).toBe('2026-07-28T11:00:00.000Z');
  });
});

function metric(
  id: number,
  endpoint: string,
  responseTimeMs: number,
  statusCode: number,
  timestamp: string,
): SystemMetric {
  return {
    id,
    endpoint,
    method: 'GET',
    responseTimeMs,
    statusCode,
    timestamp,
  };
}

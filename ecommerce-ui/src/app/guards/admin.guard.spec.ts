import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';

import { AdminGuard } from './admin.guard';
import { AuthService } from '../auth.service';

describe('AdminGuard', () => {
  let guard: AdminGuard;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    const spy = jasmine.createSpyObj<Router>('Router', ['navigate']);

    TestBed.configureTestingModule({
      providers: [
        AdminGuard,
        { provide: Router, useValue: spy },
        { provide: AuthService, useValue: {} },
      ],
    });

    guard = TestBed.inject(AdminGuard);
    router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
    router.navigate.calls.reset();
  });

  it('should be created', () => {
    expect(guard).toBeTruthy();
  });

  it('should allow admin role token', () => {
    const payload = {
      'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'admin',
    };
    const token = `${btoa('{}')}.${btoa(JSON.stringify(payload))}.${btoa('{}')}`;
    localStorage.setItem('authToken', token);

    expect(guard.canActivate()).toBeTrue();
  });

  it('should deny missing token and navigate to login', () => {
    expect(guard.canActivate()).toBeFalse();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });
});

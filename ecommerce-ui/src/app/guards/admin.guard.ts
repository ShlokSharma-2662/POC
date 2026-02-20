import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { AuthService } from '../auth.service'; // adjust path if needed

@Injectable({
  providedIn: 'root'
})
export class AdminGuard implements CanActivate {

  constructor(private authService: AuthService, private router: Router) {}

  canActivate(): boolean {
    const token = localStorage.getItem('authToken') || localStorage.getItem('accessToken');
    if (!token) {
      this.router.navigate(['/login']);
      return false;
    }
  
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const possibleRoles: any =
        payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
        payload['role'] ??
        payload['roles'];

      const roles = Array.isArray(possibleRoles) ? possibleRoles : [possibleRoles];

      if (roles && roles.some((r: string) => String(r).toLowerCase() === 'admin')) {
        return true;
      }
  
      // Role is not admin
      this.router.navigate(['/home']); // safer fallback
      return false;
  
    } catch (e) {
      this.router.navigate(['/login']);
      return false;
    }
  }  
}

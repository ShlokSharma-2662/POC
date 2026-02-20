import { Routes } from '@angular/router';
import { ProductsComponent } from './products/products.component';
import { HomeComponent } from './home/home.component';
import { CartComponent } from './cart/cart.component';
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';
import { SuccessComponent } from './success/success.component';
import { MyOrdersComponent } from './my-orders/my-orders.component';
import { AuthGuard } from './guards/auth.guard';
import { AdminGuard } from './guards/admin.guard';
import { AdminDashboardComponent } from './admin-dashboard/admin-dashboard.component';
import { UnauthorizedComponent } from './unauthorized/unauthorized.component';
import { AdminOrdersComponent } from './admin-orders/admin-orders.component';
import { PerformanceDashboardComponent } from './admin/performance-dashboard/performance-dashboard.component';
import { AdminErrorLogsComponent } from './admin/admin-error-logs/admin-error-logs.component';
import { AdminUsersComponent } from './admin-users/admin-users.component';
import { AdminProductsComponent } from './admin/admin-products/admin-products.component';
import { RevenueReportingComponent } from './admin/revenue-reporting/revenue-reporting.component';
import { WishlistComponent } from './wishlist/wishlist.component';
import { ProfileComponent } from './profile/profile.component';
import { OAuthCallbackComponent } from './oauth-callback/oauth-callback.component';
import { ChangePasswordComponent } from './change-password/change-password.component';

export const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'oauth/callback', component: OAuthCallbackComponent },
  // ✅ Public pages (accessible to guests)
  { path: 'home', component: HomeComponent },
  { path: 'products', component: ProductsComponent },
  { path: 'cart', component: CartComponent }, // Allow guest access to cart
  { path: 'wishlist', component: WishlistComponent }, // Allow guest access to wishlist
  // ✅ Protected pages (require authentication)
  { path: 'success', component: SuccessComponent, canActivate: [AuthGuard] },
  { path: 'my-orders', component: MyOrdersComponent, canActivate: [AuthGuard] },
  { path: 'profile', component: ProfileComponent, canActivate: [AuthGuard] },
  { path: 'change-password', component: ChangePasswordComponent, canActivate: [AuthGuard] },
  // ✅ Admin pages
  { path: 'admin', component: AdminDashboardComponent, canActivate: [AdminGuard] },
  { path: 'unauthorized', component: UnauthorizedComponent },
  { path: 'admin/orders', component: AdminOrdersComponent, canActivate: [AdminGuard] },
  {path : 'admin/performance', component: PerformanceDashboardComponent, canActivate: [AdminGuard]},
  { path: 'admin/errors', component: AdminErrorLogsComponent, canActivate: [AdminGuard] },
  { path: 'admin/users', component: AdminUsersComponent, canActivate: [AdminGuard] },
  { path: 'admin/products', component: AdminProductsComponent, canActivate: [AdminGuard] },
  { path: 'admin/revenue', component: RevenueReportingComponent, canActivate: [AdminGuard] },
  { path: '**', redirectTo: 'home' }
];

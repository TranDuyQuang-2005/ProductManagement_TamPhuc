import { Routes } from '@angular/router';
import { ProductListComponent } from './features/products/product-list.component';
import { ProductFormComponent } from './features/products/product-form.component';
import { CategoryListComponent } from './features/categories/category-list.component';
import { CategoryFormComponent } from './features/categories/category-form.component';
import { LoginComponent } from './features/auth/login.component';
import { AuditLogListComponent } from './features/audit-logs/audit-log-list.component';
import { authGuard, guestGuard } from './core/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'products' },
  { path: 'login', component: LoginComponent, canActivate: [guestGuard] },
  { path: 'products', component: ProductListComponent, canActivate: [authGuard] },
  { path: 'products/new', component: ProductFormComponent, canActivate: [authGuard] },
  { path: 'products/:id/edit', component: ProductFormComponent, canActivate: [authGuard] },
  { path: 'categories', component: CategoryListComponent, canActivate: [authGuard] },
  { path: 'categories/new', component: CategoryFormComponent, canActivate: [authGuard] },
  { path: 'categories/:id/edit', component: CategoryFormComponent, canActivate: [authGuard] },
  { path: 'audit-logs', component: AuditLogListComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: 'products' }
];

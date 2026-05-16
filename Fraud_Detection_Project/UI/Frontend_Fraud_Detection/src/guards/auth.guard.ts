import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const user = authService.getCurrentUserValue();

  if (!user) {
    router.navigate(['/login']);
    return false;
  }

  // Check if the route requires admin role
  const requiresAdmin = route.data?.['role'] === 'Admin';
  if (requiresAdmin && user.role !== 'Admin') {
    router.navigate(['/user/dashboard']);
    return false;
  }

  return true;
};

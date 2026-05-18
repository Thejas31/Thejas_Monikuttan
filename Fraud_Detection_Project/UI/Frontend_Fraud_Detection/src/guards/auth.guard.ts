import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const platformId = inject(PLATFORM_ID);

  // If running on the server, allow navigation to pass through.
  // The client will re-evaluate this guard upon hydration.
  if (!isPlatformBrowser(platformId)) {
    return true;
  }

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

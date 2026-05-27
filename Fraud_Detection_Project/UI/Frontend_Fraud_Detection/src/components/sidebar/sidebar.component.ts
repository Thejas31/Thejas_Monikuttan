import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { UserDTO } from '../../models/user.dto';
import { FraudService } from '../../services/fraud.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './sidebar.component.html',
})
export class SidebarComponent implements OnInit, OnDestroy {
  @Input() activeRoute = '';
  currentUser: UserDTO | null = null;
  hasUnresolvedAlerts = false;
  private pollInterval: any;

  constructor(
    private authService: AuthService, 
    private router: Router,
    private fraudService: FraudService
  ) {}

  ngOnInit() {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
      if (this.isAdmin()) {
        this.fetchAlertsStatus();
        this.startPolling();
      } else {
        this.stopPolling();
      }
    });
  }

  ngOnDestroy() {
    this.stopPolling();
  }

  private startPolling() {
    if (this.pollInterval) return;
    this.pollInterval = setInterval(() => {
      this.fetchAlertsStatus();
    }, 5000);
  }

  private stopPolling() {
    if (this.pollInterval) {
      clearInterval(this.pollInterval);
      this.pollInterval = null;
    }
  }

  private fetchAlertsStatus() {
    this.fraudService.getRecentAlerts().subscribe({
      next: alerts => {
        this.hasUnresolvedAlerts = alerts ? alerts.some(a => a.status === 'Pending') : false;
      },
      error: () => {
        this.hasUnresolvedAlerts = false;
      }
    });
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  isAdmin(): boolean {
    return this.currentUser?.role === 'Admin';
  }
}

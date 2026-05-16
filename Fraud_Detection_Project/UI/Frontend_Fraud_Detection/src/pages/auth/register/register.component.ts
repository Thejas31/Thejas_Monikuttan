import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './register.component.html',
})
export class RegisterComponent {
  firstName = '';
  lastName = '';
  email = '';
  password = '';
  confirmPassword = '';
  error = '';
  isLoading = false;

  constructor(private authService: AuthService, private router: Router) {}

  onSubmit() {
    if (!this.email || !this.password || !this.firstName || !this.lastName) {
      this.error = 'Please fill in all fields';
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.error = 'Passwords do not match';
      return;
    }

    this.isLoading = true;
    this.error = '';

    this.authService.register({ 
      firstName: this.firstName, 
      lastName: this.lastName,
      email: this.email, 
      password: this.password 
    }).subscribe({
        next: (user) => {
          this.isLoading = false;
          // By default new users go to user dashboard
          this.router.navigate(['/user/dashboard']);
        },
        error: (err) => {
          this.isLoading = false;
          this.error = 'Registration failed. Please try again.';
        }
      });
  }
}

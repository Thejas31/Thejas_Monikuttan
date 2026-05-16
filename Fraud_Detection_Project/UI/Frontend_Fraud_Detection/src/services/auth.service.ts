import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { UserDTO, LoginDTO, RegisterDTO } from '../models/user.dto';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private currentUserSubject = new BehaviorSubject<UserDTO | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor() {}

  // MOCK LOGIN for now - later replace with HttpClient
  login(credentials: LoginDTO): Observable<UserDTO> {
    let mockUser: UserDTO;
    
    // Simulate role based on email
    if (credentials.email.includes('admin')) {
      mockUser = { id: '1', email: credentials.email, role: 'Admin', firstName: 'Admin', lastName: 'User', token: 'mock-jwt-token' };
    } else {
      mockUser = { id: '2', email: credentials.email, role: 'User', firstName: 'Standard', lastName: 'User', token: 'mock-jwt-token' };
    }

    this.currentUserSubject.next(mockUser);
    localStorage.setItem('currentUser', JSON.stringify(mockUser));
    return of(mockUser);
  }

  register(data: RegisterDTO): Observable<UserDTO> {
    const newUser: UserDTO = {
      id: Math.random().toString(36).substr(2, 9),
      email: data.email,
      firstName: data.firstName,
      lastName: data.lastName,
      role: 'User',
      token: 'mock-jwt-token'
    };
    this.currentUserSubject.next(newUser);
    localStorage.setItem('currentUser', JSON.stringify(newUser));
    return of(newUser);
  }

  logout() {
    localStorage.removeItem('currentUser');
    this.currentUserSubject.next(null);
  }

  getCurrentUserValue(): UserDTO | null {
    return this.currentUserSubject.value;
  }
}

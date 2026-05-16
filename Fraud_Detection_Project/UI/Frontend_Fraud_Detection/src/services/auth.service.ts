import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { HttpClient } from '@angular/common/http';
import { UserDTO, LoginDTO, RegisterDTO } from '../models/user.dto';
import { environment } from '../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private currentUserSubject = new BehaviorSubject<UserDTO | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {
    const savedUser = localStorage.getItem('currentUser');
    if (savedUser) {
      this.currentUserSubject.next(JSON.parse(savedUser));
    }
  }

  login(credentials: LoginDTO): Observable<UserDTO> {
    return this.http.post<{ token: string }>(`${environment.apiUrl}/auth/login`, {
      username: credentials.email, // backend expects Username
      password: credentials.password
    }).pipe(
      map(response => {
        const token = response.token;
        const decoded = this.decodeToken(token);
        const user: UserDTO = {
          id: decoded.nameid || '0',
          email: credentials.email,
          role: decoded.role || 'User',
          firstName: credentials.email.split('@')[0], // Extract from email for display
          lastName: '',
          token: token
        };
        this.currentUserSubject.next(user);
        localStorage.setItem('currentUser', JSON.stringify(user));
        return user;
      })
    );
  }

  register(data: RegisterDTO): Observable<UserDTO> {
    return this.http.post<{ token: string }>(`${environment.apiUrl}/auth/register`, {
      username: data.email, // Mapping email to username
      email: data.email,
      password: data.password,
      role: 'User'
    }).pipe(
      map(response => {
        const token = response.token;
        const decoded = this.decodeToken(token);
        const user: UserDTO = {
          id: decoded.nameid || '0',
          email: data.email,
          role: 'User',
          firstName: data.firstName,
          lastName: data.lastName,
          token: token
        };
        this.currentUserSubject.next(user);
        localStorage.setItem('currentUser', JSON.stringify(user));
        return user;
      })
    );
  }

  private decodeToken(token: string): any {
    try {
      return JSON.parse(atob(token.split('.')[1]));
    } catch (e) {
      return {};
    }
  }

  logout() {
    localStorage.removeItem('currentUser');
    this.currentUserSubject.next(null);
  }

  getCurrentUserValue(): UserDTO | null {
    return this.currentUserSubject.value;
  }
}

import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
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

  constructor(
    private http: HttpClient,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {
    if (isPlatformBrowser(this.platformId)) {
      const savedUser = localStorage.getItem('currentUser');
      if (savedUser) {
        this.currentUserSubject.next(JSON.parse(savedUser));
      }
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
        
        // .NET stores roles and names using long claim URIs by default
        const roleClaim = decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || decoded.role || 'User';
        const givenName = decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname'] || credentials.email.split('@')[0];
        const surname = decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname'] || '';

        const user: UserDTO = {
          id: decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || decoded.nameid || '0',
          email: credentials.email,
          role: roleClaim,
          firstName: givenName,
          lastName: surname,
          token: token
        };
        this.currentUserSubject.next(user);
        if (isPlatformBrowser(this.platformId)) {
          localStorage.setItem('currentUser', JSON.stringify(user));
        }
        return user;
      })
    );
  }

  register(data: RegisterDTO): Observable<UserDTO> {
    return this.http.post<{ token: string }>(`${environment.apiUrl}/auth/register`, {
      username: data.email, // Mapping email to username
      email: data.email,
      firstName: data.firstName,
      lastName: data.lastName,
      password: data.password,
      role: 'User'
    }).pipe(
      map(response => {
        const token = response.token;
        const decoded = this.decodeToken(token);
        
        const roleClaim = decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || decoded.role || 'User';
        const givenName = decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname'] || data.firstName;
        const surname = decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname'] || data.lastName;

        const user: UserDTO = {
          id: decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || decoded.nameid || '0',
          email: data.email,
          role: roleClaim,
          firstName: givenName,
          lastName: surname,
          token: token
        };
        this.currentUserSubject.next(user);
        if (isPlatformBrowser(this.platformId)) {
          localStorage.setItem('currentUser', JSON.stringify(user));
        }
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
    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem('currentUser');
    }
    this.currentUserSubject.next(null);
  }

  getCurrentUserValue(): UserDTO | null {
    return this.currentUserSubject.value;
  }
}

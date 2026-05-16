// src/models/user.dto.ts
export interface UserDTO {
  id: string;
  email: string;
  role: 'Admin' | 'User';
  firstName: string;
  lastName: string;
  token?: string;
}

export interface LoginDTO {
  email: string;
  password?: string; // Optional for response, required for request
}

export interface RegisterDTO {
  firstName: string;
  lastName: string;
  email: string;
  password?: string;
}

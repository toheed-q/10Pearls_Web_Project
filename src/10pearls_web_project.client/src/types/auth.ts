export interface LoginDTO {
  email: string;
  password: string;
}

export interface RegisterDTO {
  fullName: string;
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  id: string;
  email: string;
  fullName: string;
}

export interface AuthUser {
  id: string;
  email: string;
  fullName: string;
}

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
  role: string;
}

export interface AuthUser {
  id: string;
  email: string;
  fullName: string;
  role: string;
}

export interface UserSummary {
  id: string;
  fullName: string;
  email: string;
  role: string;
  taskCount: number;
}

export interface UserRoleChangedPayload {
  userId: string;
  oldRole: string;
  newRole: string;
}

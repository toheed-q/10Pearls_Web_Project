export interface LoginDTO {
  email: string;
  password: string;
}

export interface RegisterDTO {
  fullName: string;
  email: string;
  password: string;
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

export interface ProfileTaskStats {
  total: number;
  completed: number;
  pending: number;
  inProgress: number;
}

export interface UserProfileDto {
  id: string;
  fullName: string;
  email: string;
  role: string;
  taskStats: ProfileTaskStats;
}

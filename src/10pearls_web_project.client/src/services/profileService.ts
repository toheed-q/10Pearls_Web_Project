import type { UserProfileDto } from '../types/auth';
import { apiRequest } from './apiClient';

export interface UpdateProfileRequest {
  fullName?: string;
  currentPassword?: string;
  newPassword?: string;
}

export const profileService = {
  getProfile: () =>
    apiRequest<UserProfileDto>('/api/auth/me'),

  updateProfile: (req: UpdateProfileRequest) =>
    apiRequest<{ message: string }>('/api/auth/me', {
      method: 'PUT',
      body: JSON.stringify(req),
    }),
};

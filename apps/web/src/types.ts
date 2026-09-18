export interface UserProfileDto {
  displayName: string;
  timezone: string;
  locale: string;
  theme: string;
  version: number;
}

export interface UserPreferenceDto {
  emailNotificationsEnabled: boolean;
  quietHoursEnabled: boolean;
  quietStart: string | null;
  quietEnd: string | null;
  quietTimezone: string | null;
  recipientVersion: number;
}

export interface UserDto {
  id: string;
  email: string;
  emailConfirmed: boolean;
  profile: UserProfileDto;
  preferences: UserPreferenceDto;
}

export interface UserSessionDto {
  id: string;
  isCurrent: boolean;
  createdAt: string;
  expiresAt: string;
  ipAddress: string | null;
  deviceInfo: string | null;
}

export interface CsrfTokenResponse {
  headerName: string;
  token: string;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}

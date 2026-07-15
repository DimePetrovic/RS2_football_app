export interface RegisterRequest {
  email: string;
  username: string;
  password: string;
  confirmPassword: string;
}

export interface CompleteRegistrationRequest {
  userId: string;
  token: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  preferredPosition: number;
  canPlayGoalkeeper: boolean;
  youthSeasons: number;
  seniorSeasons: number;
  nationality?: string | null;
}

export interface ResendConfirmationRequest {
  email: string;
}

export interface ValidateEmailTokenResponse {
  isValid: boolean;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  userId: string;
  username: string;
  email: string;
  role: string;
}

export interface RegisterResponse {
  message: string;
}

export interface RefreshResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  userId: string;
  username: string;
  email: string;
  role: string;
}

export interface CurrentUser {
  userId: string;
  username: string;
  email: string;
  role: string;
}

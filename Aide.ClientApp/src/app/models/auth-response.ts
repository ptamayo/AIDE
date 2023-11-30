export interface AuthResponse {
    isLoginSuccessful: boolean;
    isUserLocked: boolean;
    message: string;
    token: string;
}

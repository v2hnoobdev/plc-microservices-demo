// User DTO from User Service
export interface User {
  id: number;
  username: string;
  email: string;
  fullName: string;
  department: string;
  isActive: boolean;
}

// API configuration - All requests go through Gateway
export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

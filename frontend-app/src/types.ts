// User DTO from User Service
export interface User {
  id: string; // Guid
  keycloakUserId: string; // Guid
  username: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  role: string | null;
  createdAt: string; // DateTime ISO string
  updatedAt: string | null; // DateTime ISO string
}

// API configuration - All requests go through Gateway
export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

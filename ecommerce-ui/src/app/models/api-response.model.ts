export interface ApiResponse<T = any> {
  isSuccessful: boolean;
  status: string;
  statusReason: string;
  data: T | null;
}

// Common status types
export type ApiStatus = 'Success' | 'Error' | 'Exception' | 'ValidationError' | 'NotFound' | 'Unauthorized';

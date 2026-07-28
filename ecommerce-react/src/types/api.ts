export interface ApiResponse<T> {
  isSuccessful: boolean;
  status: string;
  statusReason: string;
  data: T | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface ApiProblem {
  message?: string;
  statusReason?: string;
  title?: string;
  errors?: Record<string, string[]>;
}

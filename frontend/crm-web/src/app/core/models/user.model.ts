export interface User {
  employeeId: number;
  fullName: string;
  email: string;
  role: 'ADMIN' | 'SALES';
  token: string;
}
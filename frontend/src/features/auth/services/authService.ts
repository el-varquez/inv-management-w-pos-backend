import api from '../../../services/api';
import type { LoginResult } from '../../../types';

export const authService = {
  login: async (email: string, password: string): Promise<LoginResult> => {
    const { data } = await api.post<LoginResult>('/auth/login', { email, password });
    return data;
  },
};
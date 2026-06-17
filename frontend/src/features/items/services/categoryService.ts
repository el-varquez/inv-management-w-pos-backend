import api from '../../../services/api';
import type { Category } from '../../../types';

export const categoryService = {
  getAll: async (): Promise<Category[]> => {
    const { data } = await api.get<Category[]>('/categories');
    return data;
  },

  create: async (payload: {
    name: string;
    description?: string;
  }): Promise<{ id: string }> => {
    const { data } = await api.post<{ id: string }>('/categories', payload);
    return data;
  },
};

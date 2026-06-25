import api from '../../../services/api';
import type { CashierList } from '../../../types';

export const cashierService = {
  getAll: async (): Promise<CashierList> => {
    const { data } = await api.get<CashierList>('/cashiers');
    return data;
  },

  create: async (payload: {
    name: string;
    email: string;
    password: string;
  }): Promise<{ id: string }> => {
    const { data } = await api.post<{ id: string }>('/cashiers', payload);
    return data;
  },

  deactivate: async (id: string): Promise<void> => {
    await api.post(`/cashiers/${id}/deactivate`);
  },

  reactivate: async (id: string): Promise<void> => {
    await api.post(`/cashiers/${id}/reactivate`);
  },

  resetPassword: async (id: string, password: string): Promise<void> => {
    await api.post(`/cashiers/${id}/reset-password`, { password });
  },
};

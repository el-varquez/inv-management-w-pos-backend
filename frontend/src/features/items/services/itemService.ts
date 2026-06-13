import api from '../../../services/api';
import type { Item } from '../../../types';

export type CreateItemPayload = Omit<
  Item,
  'id' | 'stock' | 'isLowStock' | 'isActive' | 'categoryName' | 'createdAt'
>;

export type UpdateItemPayload = CreateItemPayload & { isActive: boolean };

export const itemService = {
  getAll: async (): Promise<Item[]> => {
    const { data } = await api.get<Item[]>('/items');
    return data;
  },

  create: async (payload: CreateItemPayload): Promise<{ id: string }> => {
    const { data } = await api.post<{ id: string }>('/items', payload);
    return data;
  },

  update: async (id: string, payload: UpdateItemPayload): Promise<void> => {
    await api.put(`/items/${id}`, payload);
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/items/${id}`);
  },
};
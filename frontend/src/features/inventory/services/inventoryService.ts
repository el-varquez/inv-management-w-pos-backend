import api from '../../../services/api';
import type {
  StockLevel,
  InventoryHistoryItem,
  InventoryValuation,
  ItemComponents,
  LowStockItem,
  Paged,
} from '../../../types';

export const inventoryService = {
  getStockLevels: async (params: {
    page: number;
    pageSize: number;
  }): Promise<Paged<StockLevel>> => {
    const { data } = await api.get<Paged<StockLevel>>(
      '/inventory/stock-levels',
      { params }
    );
    return data;
  },

  getLowStock: async (): Promise<LowStockItem[]> => {
    const { data } = await api.get<LowStockItem[]>('/inventory/low-stock');
    return data;
  },

  addStock: async (payload: {
    itemId: string;
    quantity: number;
    costPerUnit: number;
    supplierName?: string;
    notes?: string;
  }): Promise<{ id: string }> => {
    const { data } = await api.post<{ id: string }>(
      '/inventory/add-stock',
      payload
    );
    return data;
  },

  adjustStock: async (payload: {
    itemId: string;
    quantity: number;
    reason: string;
    notes?: string;
  }): Promise<{ id: string }> => {
    const { data } = await api.post<{ id: string }>(
      '/inventory/adjust-stock',
      payload
    );
    return data;
  },

  createCount: async (notes?: string): Promise<{ id: string }> => {
    const { data } = await api.post<{ id: string }>('/inventory/count', {
      notes,
    });
    return data;
  },

  completeCount: async (
    countId: string,
    lines: { itemId: string; actualQty: number }[]
  ): Promise<void> => {
    await api.post(`/inventory/count/${countId}/complete`, lines);
  },

  getComponents: async (itemId: string): Promise<ItemComponents> => {
    const { data } = await api.get<ItemComponents>(
      `/inventory/items/${itemId}/components`
    );
    return data;
  },

  setComponents: async (
    itemId: string,
    components: { componentItemId: string; quantity: number }[]
  ): Promise<void> => {
    await api.post(`/inventory/items/${itemId}/components`, components);
  },

  getHistory: async (params: {
    from?: string;
    to?: string;
    type?: string;
    page: number;
    pageSize: number;
  }): Promise<Paged<InventoryHistoryItem>> => {
    const { data } = await api.get<Paged<InventoryHistoryItem>>(
      '/inventory/history',
      { params }
    );
    return data;
  },

  getValuation: async (): Promise<InventoryValuation> => {
    const { data } = await api.get<InventoryValuation>('/inventory/valuation');
    return data;
  },
};

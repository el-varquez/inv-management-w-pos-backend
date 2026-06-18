import api from '../../../services/api';
import type {
  CreateTransactionPayload,
  TransactionResult,
  Transaction,
  TransactionDetail,
  SalesSummary,
  Paged,
} from '../../../types';

export interface SalesFilters {
  from?: string;
  to?: string;
}

export const salesService = {
  createTransaction: async (
    payload: CreateTransactionPayload
  ): Promise<TransactionResult> => {
    const { data } = await api.post<TransactionResult>('/sales', payload);
    return data;
  },

  refund: async (
    transactionId: string
  ): Promise<{ receiptNumber: string; refundedAmount: number }> => {
    const { data } = await api.post(`/sales/${transactionId}/refund`);
    return data;
  },

  getTransactions: async (
    params: SalesFilters & { page: number; pageSize: number }
  ): Promise<Paged<Transaction>> => {
    const { data } = await api.get<Paged<Transaction>>('/sales', { params });
    return data;
  },

  getTransactionById: async (id: string): Promise<TransactionDetail> => {
    const { data } = await api.get<TransactionDetail>(`/sales/${id}`);
    return data;
  },

  getSummary: async (params?: SalesFilters): Promise<SalesSummary> => {
    const { data } = await api.get<SalesSummary>('/sales/summary', { params });
    return data;
  },
};

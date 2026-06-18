import api from '../../../services/api';
import type { SalesReport, ExpenseReport, ProfitReport } from '../../../types';

export interface DateRange {
  from?: string;
  to?: string;
}

export const reportsService = {
  getSalesReport: async (range?: DateRange): Promise<SalesReport> => {
    const { data } = await api.get<SalesReport>('/reports/sales', {
      params: range,
    });
    return data;
  },

  getExpenseReport: async (range?: DateRange): Promise<ExpenseReport> => {
    const { data } = await api.get<ExpenseReport>('/reports/expenses', {
      params: range,
    });
    return data;
  },

  getProfitReport: async (
    params?: DateRange & { categoryId?: string; itemId?: string }
  ): Promise<ProfitReport> => {
    const { data } = await api.get<ProfitReport>('/reports/profit', { params });
    return data;
  },
};

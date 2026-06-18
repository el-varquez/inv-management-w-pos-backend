import api from '../../../services/api';
import type { SalesReport } from '../../../types';

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
};

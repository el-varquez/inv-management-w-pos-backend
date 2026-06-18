import { useEffect, useState } from 'react';
import { reportsService, type DateRange } from '../services/reportsService';
import { getApiErrorMessage } from '../../../services/apiError';
import type { ProfitReport } from '../../../types';

interface ProfitFilters {
  categoryId?: string;
  itemId?: string;
}

export const useProfitReport = (range?: DateRange, filters?: ProfitFilters) => {
  const [report, setReport] = useState<ProfitReport | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetch = async () => {
    setLoading(true);
    setError(null);
    try {
      const params: DateRange & ProfitFilters = { ...range };
      if (filters?.categoryId) params.categoryId = filters.categoryId;
      if (filters?.itemId) params.itemId = filters.itemId;
      const data = await reportsService.getProfitReport(params);
      setReport(data);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to load profit report.'));
    } finally {
      setLoading(false);
    }
  };

  /* eslint-disable react-hooks/set-state-in-effect, react-hooks/exhaustive-deps */
  useEffect(() => {
    fetch();
  }, [range?.from, range?.to, filters?.categoryId, filters?.itemId]);
  /* eslint-enable react-hooks/set-state-in-effect, react-hooks/exhaustive-deps */

  return { report, loading, error, refetch: fetch };
};

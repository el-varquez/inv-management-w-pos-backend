import { useEffect, useState } from 'react';
import { reportsService, type DateRange } from '../services/reportsService';
import { getApiErrorMessage } from '../../../services/apiError';
import type { SalesReport } from '../../../types';

export const useSalesReport = (range?: DateRange) => {
  const [report, setReport] = useState<SalesReport | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetch = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await reportsService.getSalesReport(range);
      setReport(data);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to load sales report.'));
    } finally {
      setLoading(false);
    }
  };

  /* eslint-disable react-hooks/set-state-in-effect, react-hooks/exhaustive-deps */
  useEffect(() => {
    fetch();
  }, [range?.from, range?.to]);
  /* eslint-enable react-hooks/set-state-in-effect, react-hooks/exhaustive-deps */

  return { report, loading, error, refetch: fetch };
};

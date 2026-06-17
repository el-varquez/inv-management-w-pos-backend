import { useEffect, useState } from 'react';
import { salesService, type SalesFilters } from '../services/salesService';
import { getApiErrorMessage } from '../../../services/apiError';
import type { Transaction, SalesSummary } from '../../../types';

/**
 * Loads the sales ledger plus its summary totals for the given date range, and
 * exposes a refund action that re-fetches both once a refund is posted.
 */
export const useSalesHistory = (filters: SalesFilters) => {
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [summary, setSummary] = useState<SalesSummary | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchHistory = async () => {
    setLoading(true);
    setError(null);
    try {
      const [txns, sum] = await Promise.all([
        salesService.getTransactions(filters),
        salesService.getSummary(filters),
      ]);
      setTransactions(txns);
      setSummary(sum);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to load sales history.'));
    } finally {
      setLoading(false);
    }
  };

  // Re-fetch whenever the date range changes.
  /* eslint-disable react-hooks/set-state-in-effect, react-hooks/exhaustive-deps */
  useEffect(() => {
    fetchHistory();
  }, [filters.from, filters.to]);
  /* eslint-enable react-hooks/set-state-in-effect, react-hooks/exhaustive-deps */

  const refund = async (id: string) => {
    await salesService.refund(id);
    await fetchHistory();
  };

  return { transactions, summary, loading, error, refetch: fetchHistory, refund };
};

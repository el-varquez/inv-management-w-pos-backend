import { useEffect, useState } from 'react';
import { salesService, type SalesFilters } from '../services/salesService';
import { getApiErrorMessage } from '../../../services/apiError';
import type { Transaction, SalesSummary } from '../../../types';
import { DEFAULT_PAGE_SIZE } from '../../../lib/pagination';

/**
 * Loads the sales ledger (paged) plus its summary totals for the given date
 * range, and exposes a refund action that re-fetches both once a refund is
 * posted. The summary covers the whole filtered range, not just the page.
 */
export const useSalesHistory = (filters: SalesFilters) => {
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [summary, setSummary] = useState<SalesSummary | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(DEFAULT_PAGE_SIZE);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchHistory = async () => {
    setLoading(true);
    setError(null);
    try {
      const [txns, sum] = await Promise.all([
        salesService.getTransactions({ ...filters, page, pageSize }),
        salesService.getSummary(filters),
      ]);
      setTransactions(txns.items);
      setTotalCount(txns.totalCount);
      setTotalPages(txns.totalPages);
      setSummary(sum);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to load sales history.'));
    } finally {
      setLoading(false);
    }
  };

  // Reset to the first page whenever the date range changes. Adjusting state
  // during render (with a previous-value guard) is React's recommended
  // alternative to a reset-in-effect — it avoids the extra commit.
  const filterKey = `${filters.from ?? ''}|${filters.to ?? ''}`;
  const [prevFilterKey, setPrevFilterKey] = useState(filterKey);
  if (filterKey !== prevFilterKey) {
    setPrevFilterKey(filterKey);
    setPage(1);
  }

  /* eslint-disable react-hooks/set-state-in-effect, react-hooks/exhaustive-deps */
  useEffect(() => {
    fetchHistory();
  }, [filters.from, filters.to, page, pageSize]);
  /* eslint-enable react-hooks/set-state-in-effect, react-hooks/exhaustive-deps */

  const refund = async (id: string) => {
    await salesService.refund(id);
    await fetchHistory();
  };

  return {
    transactions,
    summary,
    loading,
    error,
    refetch: fetchHistory,
    refund,
    page,
    setPage,
    pageSize,
    totalCount,
    totalPages,
  };
};

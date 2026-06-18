import { useEffect, useState } from 'react';
import { inventoryService } from '../services/inventoryService';
import type { InventoryHistoryItem } from '../../../types';
import { getApiErrorMessage } from '../../../services/apiError';
import { DEFAULT_PAGE_SIZE } from '../../../lib/pagination';

export interface InventoryHistoryFilters {
  from?: string;
  to?: string;
  type?: string;
}

export const useInventoryHistory = (filters?: InventoryHistoryFilters) => {
  const [history, setHistory] = useState<InventoryHistoryItem[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(DEFAULT_PAGE_SIZE);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetch = async () => {
    setLoading(true);
    setError(null);
    try {
      // Strip empty strings so we don't send blank query params.
      const params: InventoryHistoryFilters & { page: number; pageSize: number } = {
        page,
        pageSize,
      };
      if (filters?.from) params.from = filters.from;
      if (filters?.to) params.to = filters.to;
      if (filters?.type) params.type = filters.type;
      const data = await inventoryService.getHistory(params);
      setHistory(data.items);
      setTotalCount(data.totalCount);
      setTotalPages(data.totalPages);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to load history.'));
    } finally {
      setLoading(false);
    }
  };

  // Reset to the first page whenever a filter changes. Adjusting state during
  // render (with a previous-value guard) is React's recommended alternative to
  // a reset-in-effect — it avoids the extra commit + cascading render.
  const filterKey = `${filters?.from ?? ''}|${filters?.to ?? ''}|${filters?.type ?? ''}`;
  const [prevFilterKey, setPrevFilterKey] = useState(filterKey);
  if (filterKey !== prevFilterKey) {
    setPrevFilterKey(filterKey);
    setPage(1);
  }

  /* eslint-disable react-hooks/set-state-in-effect, react-hooks/exhaustive-deps */
  useEffect(() => {
    fetch();
  }, [filters?.from, filters?.to, filters?.type, page, pageSize]);
  /* eslint-enable react-hooks/set-state-in-effect, react-hooks/exhaustive-deps */

  return {
    history,
    loading,
    error,
    refetch: fetch,
    page,
    setPage,
    pageSize,
    totalCount,
    totalPages,
  };
};

import { useEffect, useState } from 'react';
import { itemService } from '../services/itemService';
import type { Item } from '../../../types';
import { getApiErrorMessage } from '../../../services/apiError';
import { DEFAULT_PAGE_SIZE } from '../../../lib/pagination';

export interface ItemFilters {
  isComposite?: boolean;
}

export const useItems = (filters?: ItemFilters) => {
  const [items, setItems] = useState<Item[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(DEFAULT_PAGE_SIZE);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchItems = async () => {
    setLoading(true);
    setError(null);
    try {
      const params: { page: number; pageSize: number; isComposite?: boolean } = {
        page,
        pageSize,
      };
      if (filters?.isComposite !== undefined) params.isComposite = filters.isComposite;
      const data = await itemService.getPaged(params);
      setItems(data.items);
      setTotalCount(data.totalCount);
      setTotalPages(data.totalPages);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to load items.'));
    } finally {
      setLoading(false);
    }
  };

  const filterKey = `${filters?.isComposite ?? ''}`;
  const [prevFilterKey, setPrevFilterKey] = useState(filterKey);
  if (filterKey !== prevFilterKey) {
    setPrevFilterKey(filterKey);
    setPage(1);
  }

  /* eslint-disable react-hooks/set-state-in-effect, react-hooks/exhaustive-deps */
  useEffect(() => {
    fetchItems();
  }, [filters?.isComposite, page, pageSize]);
  /* eslint-enable react-hooks/set-state-in-effect, react-hooks/exhaustive-deps */

  return {
    items,
    loading,
    error,
    refetch: fetchItems,
    page,
    setPage,
    pageSize,
    totalCount,
    totalPages,
  };
};

import { useEffect, useState } from 'react';
import { itemService } from '../services/itemService';
import type { Item } from '../../../types';
import { getApiErrorMessage } from '../../../services/apiError';
import { DEFAULT_PAGE_SIZE } from '../../../lib/pagination';

export const useItems = () => {
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
      const data = await itemService.getPaged({ page, pageSize });
      setItems(data.items);
      setTotalCount(data.totalCount);
      setTotalPages(data.totalPages);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to load items.'));
    } finally {
      setLoading(false);
    }
  };

  // Re-fetch whenever the page changes. The lint rules flag the loading-state
  // update inside the effect and the omitted fetchItems dep — both expected.
  /* eslint-disable react-hooks/set-state-in-effect, react-hooks/exhaustive-deps */
  useEffect(() => {
    fetchItems();
  }, [page, pageSize]);
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

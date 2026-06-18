import { useEffect, useState } from 'react';
import { itemService } from '../services/itemService';
import type { Item } from '../../../types';
import { getApiErrorMessage } from '../../../services/apiError';

/**
 * Loads the FULL active catalog for the POS register, which searches/filters
 * items client-side. Unpaged on purpose — distinct from the paged {@link useItems}
 * that backs the admin Items browse-list.
 */
export const useSellableItems = () => {
  const [items, setItems] = useState<Item[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchItems = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await itemService.getSellable();
      setItems(data);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to load items.'));
    } finally {
      setLoading(false);
    }
  };

  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(() => { fetchItems(); }, []);

  return { items, loading, error, refetch: fetchItems };
};

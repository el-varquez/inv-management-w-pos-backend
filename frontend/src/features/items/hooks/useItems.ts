import { useEffect, useState } from 'react';
import { itemService } from '../services/itemService';
import type { Item } from '../../../types';
import { getApiErrorMessage } from '../../../services/apiError';

export const useItems = () => {
  const [items, setItems] = useState<Item[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchItems = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await itemService.getAll();
      setItems(data);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to load items.'));
    } finally {
      setLoading(false);
    }
  };

  // Standard fetch-on-mount. The eslint rule flags the loading-state update
  // inside the effect, which is expected here (we sync with a remote source).
  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(() => { fetchItems(); }, []);

  return { items, loading, error, refetch: fetchItems };
};
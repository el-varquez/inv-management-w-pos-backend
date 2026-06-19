import { useState } from 'react';
import { inventoryService } from '../services/inventoryService';
import { getApiErrorMessage } from '../../../services/apiError';
import type { ItemComponents } from '../../../types';

export const useItemComponents = () => {
  const [components, setComponents] = useState<ItemComponents | null>(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = async (itemId: string): Promise<void> => {
    setLoading(true);
    setError(null);
    try {
      setComponents(await inventoryService.getComponents(itemId));
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to load components.'));
    } finally {
      setLoading(false);
    }
  };

  const save = async (
    itemId: string,
    rows: { componentItemId: string; quantity: number }[]
  ): Promise<boolean> => {
    setSaving(true);
    setError(null);
    try {
      await inventoryService.setComponents(itemId, rows);
      return true;
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to save components.'));
      return false;
    } finally {
      setSaving(false);
    }
  };

  return { components, load, save, loading, saving, error };
};

import { useState } from 'react';
import { inventoryService } from '../services/inventoryService';
import { getApiErrorMessage } from '../../../services/apiError';

export const useInventoryCount = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const createCount = async (notes?: string): Promise<string | null> => {
    setLoading(true);
    setError(null);
    try {
      const result = await inventoryService.createCount(notes);
      return result.id;
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to create count.'));
      return null;
    } finally {
      setLoading(false);
    }
  };

  const completeCount = async (
    countId: string,
    lines: { itemId: string; actualQty: number }[]
  ): Promise<boolean> => {
    setLoading(true);
    setError(null);
    try {
      await inventoryService.completeCount(countId, lines);
      return true;
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to complete count.'));
      return false;
    } finally {
      setLoading(false);
    }
  };

  const submitCount = async (
    lines: { itemId: string; actualQty: number }[],
    notes?: string
  ): Promise<boolean> => {
    const id = await createCount(notes);
    if (!id) return false;
    return completeCount(id, lines);
  };

  return { createCount, completeCount, submitCount, loading, error };
};

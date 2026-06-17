import { useState } from 'react';
import {
  itemService,
  type CreateItemPayload,
  type UpdateItemPayload,
} from '../services/itemService';
import { getApiErrorMessage } from '../../../services/apiError';

/** Create / update / delete actions for items, with shared loading + error. */
export const useItemMutations = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const run = async (action: () => Promise<void>): Promise<boolean> => {
    setLoading(true);
    setError(null);
    try {
      await action();
      return true;
    } catch (err) {
      setError(getApiErrorMessage(err, 'Something went wrong. Please try again.'));
      return false;
    } finally {
      setLoading(false);
    }
  };

  const createItem = (payload: CreateItemPayload) =>
    run(async () => {
      await itemService.create(payload);
    });

  const updateItem = (id: string, payload: UpdateItemPayload) =>
    run(() => itemService.update(id, payload));

  const deleteItem = (id: string) => run(() => itemService.delete(id));

  return { createItem, updateItem, deleteItem, loading, error };
};

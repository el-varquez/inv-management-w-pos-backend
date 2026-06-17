import { useState } from 'react';
import { categoryService } from '../services/categoryService';
import { getApiErrorMessage } from '../../../services/apiError';

/** Create / update / delete actions for categories, with shared loading + error. */
export const useCategoryMutations = () => {
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

  const createCategory = (payload: { name: string; description?: string }) =>
    run(async () => {
      await categoryService.create(payload);
    });

  const updateCategory = (
    id: string,
    payload: { name: string; description?: string }
  ) => run(() => categoryService.update(id, payload));

  const deleteCategory = (id: string) => run(() => categoryService.remove(id));

  return { createCategory, updateCategory, deleteCategory, loading, error };
};

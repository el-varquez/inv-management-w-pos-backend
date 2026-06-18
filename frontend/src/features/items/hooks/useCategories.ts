import { useEffect, useState } from 'react';
import { categoryService } from '../services/categoryService';
import type { Category } from '../../../types';
import { getApiErrorMessage } from '../../../services/apiError';

export const useCategories = () => {
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchCategories = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await categoryService.getAll();
      setCategories(data);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to load categories.'));
    } finally {
      setLoading(false);
    }
  };

  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(() => { fetchCategories(); }, []);

  const createCategory = async (
    name: string,
    description?: string
  ): Promise<string> => {
    const { id } = await categoryService.create({ name, description });
    await fetchCategories();
    return id;
  };

  return { categories, loading, error, refetch: fetchCategories, createCategory };
};

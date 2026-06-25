import { useState } from 'react';
import { cashierService } from '../services/cashierService';
import { getApiErrorMessage } from '../../../services/apiError';

export const useCashierMutations = () => {
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

  const createCashier = (payload: {
    name: string;
    email: string;
    password: string;
  }) =>
    run(async () => {
      await cashierService.create(payload);
    });

  const deactivateCashier = (id: string) =>
    run(() => cashierService.deactivate(id));

  const reactivateCashier = (id: string) =>
    run(() => cashierService.reactivate(id));

  const resetPassword = (id: string, password: string) =>
    run(() => cashierService.resetPassword(id, password));

  return {
    createCashier,
    deactivateCashier,
    reactivateCashier,
    resetPassword,
    loading,
    error,
  };
};

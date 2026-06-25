import { useEffect, useState } from 'react';
import { cashierService } from '../services/cashierService';
import type { Cashier } from '../../../types';
import { getApiErrorMessage } from '../../../services/apiError';

export const useCashiers = () => {
  const [cashiers, setCashiers] = useState<Cashier[]>([]);
  const [activeCount, setActiveCount] = useState(0);
  const [cashierCap, setCashierCap] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchCashiers = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await cashierService.getAll();
      setCashiers(data.cashiers);
      setActiveCount(data.activeCount);
      setCashierCap(data.cashierCap);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Failed to load cashiers.'));
    } finally {
      setLoading(false);
    }
  };

  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(() => { fetchCashiers(); }, []);

  return {
    cashiers,
    activeCount,
    cashierCap,
    loading,
    error,
    refetch: fetchCashiers,
  };
};

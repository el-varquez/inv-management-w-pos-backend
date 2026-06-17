import { useState } from 'react';
import { salesService } from '../services/salesService';
import { getApiErrorMessage } from '../../../services/apiError';
import type { CartItem, PaymentType, TransactionResult } from '../../../types';

export const useCheckout = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<TransactionResult | null>(null);

  const checkout = async (
    cart: CartItem[],
    transactionDiscount: number,
    paymentType: PaymentType,
    amountTendered: number
  ): Promise<TransactionResult | null> => {
    setLoading(true);
    setError(null);
    try {
      const res = await salesService.createTransaction({
        items: cart.map((c) => ({
          itemId: c.itemId,
          quantity: c.quantity,
          discount: c.discount,
        })),
        transactionDiscount,
        paymentType,
        amountTendered,
      });
      setResult(res);
      return res;
    } catch (err) {
      setError(getApiErrorMessage(err, 'Checkout failed.'));
      return null;
    } finally {
      setLoading(false);
    }
  };

  return {
    checkout,
    loading,
    error,
    result,
    reset: () => {
      setResult(null);
      setError(null);
    },
  };
};

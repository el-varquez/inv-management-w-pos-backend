import { useMemo, useState } from 'react';
import type { CartItem, Item } from '../../../types';

export const useCart = () => {
  const [cart, setCart] = useState<CartItem[]>([]);
  const [transactionDiscount, setTransactionDiscount] = useState(0);

  const addItem = (item: Item) => {
    setCart((prev) => {
      const existing = prev.find((c) => c.itemId === item.id);
      if (existing) {
        return prev.map((c) =>
          c.itemId === item.id ? { ...c, quantity: c.quantity + 1 } : c
        );
      }
      return [
        ...prev,
        {
          itemId: item.id,
          name: item.name,
          unitPrice: item.sellingPrice,
          quantity: 1,
          discount: 0,
          stock: item.stock,
        },
      ];
    });
  };

  const removeItem = (itemId: string) => {
    setCart((prev) => prev.filter((c) => c.itemId !== itemId));
  };

  const updateQuantity = (itemId: string, quantity: number) => {
    if (quantity <= 0) {
      removeItem(itemId);
      return;
    }
    setCart((prev) =>
      prev.map((c) => (c.itemId === itemId ? { ...c, quantity } : c))
    );
  };

  const updateDiscount = (itemId: string, discount: number) => {
    setCart((prev) =>
      prev.map((c) =>
        c.itemId === itemId ? { ...c, discount: Math.max(0, discount) } : c
      )
    );
  };

  const clearCart = () => {
    setCart([]);
    setTransactionDiscount(0);
  };

  const subtotal = useMemo(
    () => cart.reduce((sum, c) => sum + c.unitPrice * c.quantity, 0),
    [cart]
  );

  const lineDiscounts = useMemo(
    () => cart.reduce((sum, c) => sum + c.discount, 0),
    [cart]
  );

  const total = useMemo(
    () => Math.max(0, subtotal - lineDiscounts - transactionDiscount),
    [subtotal, lineDiscounts, transactionDiscount]
  );

  return {
    cart,
    transactionDiscount,
    setTransactionDiscount,
    addItem,
    removeItem,
    updateQuantity,
    updateDiscount,
    clearCart,
    subtotal,
    lineDiscounts,
    total,
  };
};

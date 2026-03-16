import { useState, useCallback, useEffect } from 'react';

/** Generates a new basket ID like B4821 */
export function newBasketId() {
  return 'B' + Math.floor(Math.random() * 9000 + 1000);
}

const EMPTY_BASKET = { basketId: newBasketId(), cashierId: '', items: [], total: 0 };

/**
 * Manages basket state — updated via SignalR BasketUpdated events.
 *
 * @param {{ invoke: Function, on: Function }} signalR
 */
export function useBasket({ invoke, on }) {
  const [basket, setBasket] = useState(EMPTY_BASKET);

  // Listen for BasketUpdated events from Basket.Service (via POS.WebHost → SignalR)
  useEffect(() => {
    return on('BasketUpdated', data => {
      setBasket({
        basketId:  data.basketId,
        cashierId: data.cashierId ?? '',
        items:     data.items ?? [],
        total:     data.total ?? 0
      });
    });
  }, [on]);

  const addItem = useCallback((barcode) =>
    invoke('AddItem', basket.basketId, barcode),
  [invoke, basket.basketId]);

  const removeItem = useCallback((itemId) =>
    invoke('RemoveItem', basket.basketId, itemId),
  [invoke, basket.basketId]);

  const abortBasket = useCallback(() =>
    invoke('AbortBasket', basket.basketId),
  [invoke, basket.basketId]);

  const resetBasket = useCallback(() => {
    setBasket({ ...EMPTY_BASKET, basketId: newBasketId() });
  }, []);

  return { basket, addItem, removeItem, abortBasket, resetBasket };
}

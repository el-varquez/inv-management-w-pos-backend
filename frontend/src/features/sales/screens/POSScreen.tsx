import { useMemo, useState } from 'react';
import { useSellableItems } from '../../items/hooks/useSellableItems';
import { useCart } from '../hooks/useCart';
import { useCheckout } from '../hooks/useCheckout';
import { SalesTabs } from '../components/SalesTabs';
import { ReceiptModal } from '../components/ReceiptModal';
import { peso } from '../../../lib/format';
import type { PaymentType } from '../../../types';

const PAYMENT_TYPES: PaymentType[] = ['Cash', 'GCash', 'Maya'];

export const POSScreen = () => {
  const { items, loading, error } = useSellableItems();
  const cart = useCart();
  const { checkout, loading: checkingOut, error: checkoutError, result, reset } =
    useCheckout();

  const [paymentType, setPaymentType] = useState<PaymentType>('Cash');
  const [tendered, setTendered] = useState('');
  const [search, setSearch] = useState('');

  const sellable = useMemo(
    () =>
      items
        .filter((i) => i.isActive)
        .filter((i) => i.name.toLowerCase().includes(search.toLowerCase())),
    [items, search]
  );

  const tenderedNum = Number(tendered) || 0;
  const change = tenderedNum - cart.total;
  const canCheckout =
    cart.cart.length > 0 && tenderedNum >= cart.total && !checkingOut;

  const handleCheckout = async () => {
    const res = await checkout(
      cart.cart,
      cart.transactionDiscount,
      paymentType,
      tenderedNum
    );
    if (res) {
      cart.clearCart();
      setTendered('');
    }
  };

  return (
    <>
      <div className="page-head">
        <div>
          <p className="eyebrow">Sales · Register</p>
          <h1 className="page-title">Point of sale</h1>
          <p className="page-lead">
            {loading
              ? 'Loading catalog…'
              : error
                ? 'Could not load items.'
                : 'Tap an item to add it to the cart.'}
          </p>
        </div>
      </div>

      <SalesTabs />

      <div className="pos-layout">
        {/* ── Products ──────────────────────────────────── */}
        <section className="pos-products card">
          <div className="pos-search">
            <input
              className="input"
              type="search"
              placeholder="Search items…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>

          {error ? (
            <div className="state state-error">
              <div className="state-emoji">⚠️</div>
              <div className="state-title">Something went wrong</div>
              <p className="state-msg">{error}</p>
            </div>
          ) : loading ? (
            <div className="product-grid">
              {Array.from({ length: 8 }).map((_, i) => (
                <div key={i} className="product-card is-skeleton">
                  <span className="skeleton" style={{ width: '70%' }} />
                  <span className="skeleton" style={{ width: 50 }} />
                </div>
              ))}
            </div>
          ) : sellable.length === 0 ? (
            <div className="state">
              <div className="state-emoji">🔍</div>
              <div className="state-title">No items found</div>
              <p className="state-msg">
                {items.length === 0
                  ? 'Add items to your catalog first.'
                  : 'No active items match your search.'}
              </p>
            </div>
          ) : (
            <div className="product-grid">
              {sellable.map((item) => (
                <button
                  key={item.id}
                  type="button"
                  className="product-card"
                  onClick={() => cart.addItem(item)}
                >
                  <span className="product-name">{item.name}</span>
                  <span className="product-price tnum">
                    {peso.format(item.sellingPrice)}
                  </span>
                  <span
                    className={`badge ${item.stock <= 0 ? 'badge-low' : item.isLowStock ? 'badge-low' : 'badge-muted'}`}
                  >
                    {item.stock <= 0 ? 'Out' : `${item.stock} left`}
                  </span>
                </button>
              ))}
            </div>
          )}
        </section>

        {/* ── Cart ──────────────────────────────────────── */}
        <aside className="pos-cart card">
          <div className="pos-cart-head">
            <h2 className="modal-title">Cart</h2>
            {cart.cart.length > 0 && (
              <button
                type="button"
                className="btn btn-quiet btn-sm"
                onClick={cart.clearCart}
              >
                Clear
              </button>
            )}
          </div>

          {cart.cart.length === 0 ? (
            <div className="cart-empty">
              <div className="state-emoji">🛒</div>
              <p className="state-msg">Cart is empty.</p>
            </div>
          ) : (
            <div className="cart-lines">
              {cart.cart.map((c) => (
                <div key={c.itemId} className="cart-line">
                  <div className="cart-line-main">
                    <span className="item-name">{c.name}</span>
                    <button
                      type="button"
                      className="cart-remove"
                      aria-label={`Remove ${c.name}`}
                      onClick={() => cart.removeItem(c.itemId)}
                    >
                      ✕
                    </button>
                  </div>
                  <div className="cart-line-controls">
                    <div className="qty-stepper">
                      <button
                        type="button"
                        onClick={() =>
                          cart.updateQuantity(c.itemId, c.quantity - 1)
                        }
                        aria-label="Decrease quantity"
                      >
                        −
                      </button>
                      <input
                        className="input"
                        type="number"
                        min="0"
                        value={c.quantity}
                        onChange={(e) =>
                          cart.updateQuantity(
                            c.itemId,
                            parseInt(e.target.value, 10) || 0
                          )
                        }
                      />
                      <button
                        type="button"
                        onClick={() =>
                          cart.updateQuantity(c.itemId, c.quantity + 1)
                        }
                        aria-label="Increase quantity"
                      >
                        +
                      </button>
                    </div>
                    <span className="cart-line-total tnum">
                      {peso.format(c.unitPrice * c.quantity - c.discount)}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          )}

          <div className="cart-totals">
            <div className="total-line">
              <span>Subtotal</span>
              <span className="tnum">{peso.format(cart.subtotal)}</span>
            </div>
            <div className="total-line">
              <span>Line discounts</span>
              <span className="tnum">−{peso.format(cart.lineDiscounts)}</span>
            </div>
            <div className="total-line total-line-input">
              <label htmlFor="extra-discount">Extra discount</label>
              <input
                id="extra-discount"
                className="input input-inline"
                type="number"
                min="0"
                step="0.01"
                placeholder="0.00"
                value={cart.transactionDiscount || ''}
                onChange={(e) =>
                  cart.setTransactionDiscount(parseFloat(e.target.value) || 0)
                }
              />
            </div>
            <div className="total-line total-grand">
              <span>Total</span>
              <span className="tnum">{peso.format(cart.total)}</span>
            </div>
          </div>

          <div className="pos-pay">
            <div className="field">
              <label>Payment method</label>
              <div className="segmented">
                {PAYMENT_TYPES.map((pt) => (
                  <button
                    key={pt}
                    type="button"
                    className={pt === paymentType ? 'seg is-active' : 'seg'}
                    onClick={() => setPaymentType(pt)}
                  >
                    {pt}
                  </button>
                ))}
              </div>
            </div>

            <div className="field">
              <label htmlFor="tendered">Amount tendered</label>
              <input
                id="tendered"
                className="input"
                type="number"
                min="0"
                step="0.01"
                placeholder="0.00"
                value={tendered}
                onChange={(e) => setTendered(e.target.value)}
              />
            </div>

            {tendered !== '' && cart.cart.length > 0 && (
              <div className={`change-line ${change < 0 ? 'is-short' : ''}`}>
                <span>{change < 0 ? 'Short by' : 'Change'}</span>
                <span className="tnum">{peso.format(Math.abs(change))}</span>
              </div>
            )}

            {checkoutError && (
              <div className="login-error" role="alert">
                <span aria-hidden="true">⚠</span>
                {checkoutError}
              </div>
            )}

            <button
              type="button"
              className="btn btn-primary btn-block"
              onClick={handleCheckout}
              disabled={!canCheckout}
            >
              {checkingOut ? <span className="spinner" aria-hidden="true" /> : null}
              {checkingOut
                ? 'Processing…'
                : `Charge ${peso.format(cart.total)}`}
            </button>
          </div>
        </aside>
      </div>

      {result && <ReceiptModal result={result} onClose={reset} />}
    </>
  );
};

import { useEffect, useState } from 'react';
import { Modal } from '../../../components/Modal';
import { salesService } from '../services/salesService';
import { getApiErrorMessage } from '../../../services/apiError';
import { peso, formatDateTime } from '../../../lib/format';
import type { Transaction, TransactionDetail } from '../../../types';

interface Props {
  transaction: Transaction;
  onClose: () => void;
  onRefund: (id: string) => Promise<void>;
}

export const TransactionDetailModal = ({
  transaction,
  onClose,
  onRefund,
}: Props) => {
  const [detail, setDetail] = useState<TransactionDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [confirming, setConfirming] = useState(false);
  const [refunding, setRefunding] = useState(false);

  const isRefund = (transaction.refundedFromId ?? null) !== null;
  const canRefund = !isRefund && !transaction.isRefunded;

  /* eslint-disable react-hooks/set-state-in-effect */
  useEffect(() => {
    let active = true;
    setLoading(true);
    salesService
      .getTransactionById(transaction.id)
      .then((d) => active && setDetail(d))
      .catch(
        (err) =>
          active && setError(getApiErrorMessage(err, 'Failed to load receipt.'))
      )
      .finally(() => active && setLoading(false));
    return () => {
      active = false;
    };
  }, [transaction.id]);
  /* eslint-enable react-hooks/set-state-in-effect */

  const handleRefund = async () => {
    setRefunding(true);
    setError(null);
    try {
      await onRefund(transaction.id);
      onClose();
    } catch (err) {
      setError(getApiErrorMessage(err, 'Refund failed.'));
      setRefunding(false);
    }
  };

  return (
    <Modal
      title={transaction.receiptNumber}
      subtitle={`${formatDateTime(transaction.createdAt)} · ${transaction.paymentType}`}
      onClose={onClose}
    >
      {loading ? (
        <div className="state">
          <span className="skeleton" style={{ width: '100%', height: 80 }} />
        </div>
      ) : error && !detail ? (
        <div className="login-error" role="alert">
          <span aria-hidden="true">⚠</span>
          {error}
        </div>
      ) : detail ? (
        <>
          <table className="ledger receipt-table">
            <thead>
              <tr>
                <th>Item</th>
                <th className="num">Qty</th>
                <th className="num">Price</th>
                <th className="num">Total</th>
              </tr>
            </thead>
            <tbody>
              {detail.lines.map((l, i) => (
                <tr key={i}>
                  <td>{l.itemName}</td>
                  <td className="num tnum">{l.quantity}</td>
                  <td className="num tnum cost">{peso.format(l.unitPrice)}</td>
                  <td className="num tnum">{peso.format(l.total)}</td>
                </tr>
              ))}
            </tbody>
          </table>

          <div className="receipt">
            <div className="receipt-line">
              <span>Subtotal</span>
              <span className="tnum">{peso.format(detail.subtotal)}</span>
            </div>
            <div className="receipt-line">
              <span>Discount</span>
              <span className="tnum">−{peso.format(detail.discountAmount)}</span>
            </div>
            <div className="receipt-line receipt-grand">
              <span>Total</span>
              <span className="tnum">{peso.format(detail.total)}</span>
            </div>
          </div>

          {transaction.isRefunded && (
            <div className="badge badge-low refund-flag">Refunded</div>
          )}
          {isRefund && (
            <div className="badge badge-muted refund-flag">Refund receipt</div>
          )}

          {error && (
            <div className="login-error" role="alert">
              <span aria-hidden="true">⚠</span>
              {error}
            </div>
          )}

          {canRefund && (
            <div className="modal-actions">
              {confirming ? (
                <>
                  <span className="refund-confirm-msg">Refund this sale?</span>
                  <button
                    type="button"
                    className="btn btn-ghost"
                    onClick={() => setConfirming(false)}
                    disabled={refunding}
                  >
                    Cancel
                  </button>
                  <button
                    type="button"
                    className="btn btn-danger"
                    onClick={handleRefund}
                    disabled={refunding}
                  >
                    {refunding ? (
                      <span className="spinner spinner-dark" aria-hidden="true" />
                    ) : null}
                    {refunding ? 'Refunding…' : 'Confirm refund'}
                  </button>
                </>
              ) : (
                <button
                  type="button"
                  className="btn btn-ghost"
                  onClick={() => setConfirming(true)}
                >
                  Refund sale
                </button>
              )}
            </div>
          )}
        </>
      ) : null}
    </Modal>
  );
};

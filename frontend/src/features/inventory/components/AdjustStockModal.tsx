import { useState } from 'react';
import { Modal } from '../../../components/Modal';
import { SearchSelect } from '../../../components/SearchSelect';
import { useAdjustStock } from '../hooks/useAdjustStock';
import type { StockLevel, AdjustmentReason } from '../../../types';

interface Props {
  item: StockLevel;
  onClose: () => void;
  onDone: () => void;
}

const REASONS: AdjustmentReason[] = [
  'Damage',
  'Loss',
  'Spoilage',
  'Correction',
  'Other',
];

type Direction = 'remove' | 'add';

export const AdjustStockModal = ({ item, onClose, onDone }: Props) => {
  const { adjustStock, loading, error } = useAdjustStock();
  const [direction, setDirection] = useState<Direction>('remove');
  const [amount, setAmount] = useState('');
  const [reason, setReason] = useState<AdjustmentReason>('Damage');
  const [notes, setNotes] = useState('');

  const magnitude = Number(amount);
  const delta = direction === 'remove' ? -magnitude : magnitude;
  const newStock = item.stock + delta;
  const canSubmit = magnitude > 0 && newStock >= 0;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!canSubmit) return;
    const ok = await adjustStock({
      itemId: item.itemId,
      quantity: delta,
      reason,
      notes: notes.trim() || undefined,
    });
    if (ok) onDone();
  };

  return (
    <Modal
      title="Adjust stock"
      subtitle={`${item.itemName} · ${item.stock} on hand`}
      onClose={onClose}
    >
      <form onSubmit={handleSubmit}>
        {error && (
          <div className="login-error" role="alert">
            <span aria-hidden="true">⚠</span>
            {error}
          </div>
        )}

        <div className="field">
          <label>Direction</label>
          <div className="segmented">
            <button
              type="button"
              className={direction === 'remove' ? 'seg is-active' : 'seg'}
              onClick={() => setDirection('remove')}
            >
              Remove
            </button>
            <button
              type="button"
              className={direction === 'add' ? 'seg is-active' : 'seg'}
              onClick={() => setDirection('add')}
            >
              Add
            </button>
          </div>
        </div>

        <div className="form-grid">
          <div className="field">
            <label htmlFor="amount">Quantity</label>
            <input
              id="amount"
              className="input"
              type="number"
              min="1"
              step="1"
              placeholder="0"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              autoFocus
              required
            />
          </div>
          <div className="field">
            <label htmlFor="reason">Reason</label>
            <SearchSelect
              id="reason"
              value={reason}
              onChange={(v) => setReason(v as AdjustmentReason)}
              options={REASONS.map((r) => ({ value: r, label: r }))}
            />
          </div>
        </div>

        <div className="field">
          <label htmlFor="adj-notes">Notes (optional)</label>
          <input
            id="adj-notes"
            className="input"
            type="text"
            placeholder="What happened?"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
          />
        </div>

        <div className="modal-summary">
          <span>New stock level</span>
          <strong className={`tnum ${newStock < 0 ? 'text-red' : ''}`}>
            {newStock}
          </strong>
        </div>
        {magnitude > 0 && newStock < 0 && (
          <p className="field-hint text-red">
            Adjustment would drop stock below zero.
          </p>
        )}

        <div className="modal-actions">
          <button
            type="button"
            className="btn btn-ghost"
            onClick={onClose}
            disabled={loading}
          >
            Cancel
          </button>
          <button
            type="submit"
            className="btn btn-primary"
            disabled={loading || !canSubmit}
          >
            {loading ? <span className="spinner" aria-hidden="true" /> : null}
            {loading ? 'Saving…' : 'Apply adjustment'}
          </button>
        </div>
      </form>
    </Modal>
  );
};

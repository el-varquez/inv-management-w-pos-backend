import { useEffect, useMemo, useState } from 'react';
import { Modal } from '../../../components/Modal';
import { SearchSelect } from '../../../components/SearchSelect';
import { useItemComponents } from '../../inventory/hooks/useItemComponents';
import { useSellableItems } from '../hooks/useSellableItems';
import { peso } from '../../../lib/format';
import type { Item } from '../../../types';

interface Props {
  item: Item;
  onClose: () => void;
  onSaved: () => void;
}

interface Row {
  componentItemId: string;
  quantity: string;
}

export const ComponentsModal = ({ item, onClose, onSaved }: Props) => {
  const { components, load, save, loading, saving, error } = useItemComponents();
  const { items: allItems, loading: itemsLoading } = useSellableItems();
  const [rows, setRows] = useState<Row[]>([]);

  useEffect(() => {
    load(item.id);
  }, [item.id]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    if (!components) return;
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setRows(
      components.components.map((c) => ({
        componentItemId: c.componentItemId,
        quantity: String(c.quantity),
      }))
    );
  }, [components]);

  const costById = useMemo(() => {
    const map = new Map<string, number>();
    allItems.forEach((i) => map.set(i.id, i.costPrice));
    return map;
  }, [allItems]);

  const totalCost = rows.reduce((sum, r) => {
    const cost = costById.get(r.componentItemId) ?? 0;
    const qty = Number(r.quantity);
    return sum + (qty > 0 ? cost * qty : 0);
  }, 0);

  const chosenIds = rows.map((r) => r.componentItemId).filter(Boolean);
  const hasDuplicate = new Set(chosenIds).size !== chosenIds.length;
  const hasInvalidRow = rows.some(
    (r) => r.componentItemId === '' || Number(r.quantity) <= 0
  );
  const canSave = !hasInvalidRow && !hasDuplicate && !saving;

  const optionsFor = (current: string) =>
    allItems
      .filter(
        (i) =>
          i.id !== item.id &&
          (i.id === current || !chosenIds.includes(i.id))
      )
      .map((i) => ({ value: i.id, label: i.name }));

  const addRow = () =>
    setRows((prev) => [...prev, { componentItemId: '', quantity: '1' }]);

  const removeRow = (index: number) =>
    setRows((prev) => prev.filter((_, i) => i !== index));

  const updateRow = (index: number, patch: Partial<Row>) =>
    setRows((prev) =>
      prev.map((r, i) => (i === index ? { ...r, ...patch } : r))
    );

  const handleSave = async () => {
    if (!canSave) return;
    const ok = await save(
      item.id,
      rows.map((r) => ({
        componentItemId: r.componentItemId,
        quantity: Number(r.quantity),
      }))
    );
    if (ok) onSaved();
  };

  const isEmpty = rows.length === 0;

  return (
    <Modal
      title="Recipe"
      subtitle={`${item.name} — components consumed on each sale`}
      onClose={onClose}
    >
      {error && (
        <div className="login-error" role="alert">
          <span aria-hidden="true">⚠</span>
          {error}
        </div>
      )}

      {loading || itemsLoading ? (
        <div className="state">
          <span className="spinner" aria-hidden="true" />
          <p className="state-msg">Loading recipe…</p>
        </div>
      ) : (
        <>
          {isEmpty ? (
            <p className="field-hint">
              This item has no components. Add one to turn it into a composite —
              selling it will then draw down the components below instead of its
              own stock.
            </p>
          ) : (
            <div className="component-rows">
              {rows.map((row, index) => (
                <div className="component-row" key={index}>
                  <SearchSelect
                    value={row.componentItemId}
                    onChange={(v) => updateRow(index, { componentItemId: v })}
                    options={optionsFor(row.componentItemId)}
                    placeholder="Pick a component…"
                  />
                  <input
                    className="input"
                    type="number"
                    min="0"
                    step="0.01"
                    placeholder="Qty"
                    value={row.quantity}
                    onChange={(e) =>
                      updateRow(index, { quantity: e.target.value })
                    }
                  />
                  <button
                    type="button"
                    className="btn btn-quiet btn-sm"
                    onClick={() => removeRow(index)}
                    aria-label="Remove component"
                  >
                    ✕
                  </button>
                </div>
              ))}
            </div>
          )}

          {hasDuplicate && (
            <p className="field-hint text-red">
              Each component can only be listed once.
            </p>
          )}

          <button
            type="button"
            className="btn btn-ghost btn-sm"
            onClick={addRow}
            style={{ marginTop: 8 }}
          >
            + Add component
          </button>

          <div className="modal-summary">
            <span>Total component cost</span>
            <strong className="tnum">{peso.format(totalCost)}</strong>
          </div>

          <div className="modal-actions">
            <button
              type="button"
              className="btn btn-ghost"
              onClick={onClose}
              disabled={saving}
            >
              Cancel
            </button>
            <button
              type="button"
              className="btn btn-primary"
              onClick={handleSave}
              disabled={!canSave}
            >
              {saving ? <span className="spinner" aria-hidden="true" /> : null}
              {saving
                ? 'Saving…'
                : isEmpty
                  ? 'Remove recipe'
                  : 'Save recipe'}
            </button>
          </div>
        </>
      )}
    </Modal>
  );
};

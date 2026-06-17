import { useState } from 'react';
import { Modal } from '../../../components/Modal';
import { useItemMutations } from '../hooks/useItemMutations';
import { getApiErrorMessage } from '../../../services/apiError';
import { peso } from '../../../lib/format';
import type { Category, Item } from '../../../types';

interface Props {
  /** The item to edit, or null/undefined to create a new one. */
  item?: Item | null;
  categories: Category[];
  createCategory: (name: string) => Promise<string>;
  onClose: () => void;
  onSaved: () => void;
}

export const ItemFormModal = ({
  item,
  categories,
  createCategory,
  onClose,
  onSaved,
}: Props) => {
  const isEdit = !!item;
  const { createItem, updateItem, loading, error } = useItemMutations();

  const [name, setName] = useState(item?.name ?? '');
  const [sku, setSku] = useState(item?.sku ?? '');
  const [description, setDescription] = useState(item?.description ?? '');
  const [costPrice, setCostPrice] = useState(
    item ? String(item.costPrice) : ''
  );
  const [sellingPrice, setSellingPrice] = useState(
    item ? String(item.sellingPrice) : ''
  );
  const [lowStockThreshold, setLowStockThreshold] = useState(
    item ? String(item.lowStockThreshold) : '0'
  );
  const [categoryId, setCategoryId] = useState(item?.categoryId ?? '');
  const [isActive, setIsActive] = useState(item?.isActive ?? true);

  // Inline category creation
  const [addingCategory, setAddingCategory] = useState(categories.length === 0);
  const [newCategory, setNewCategory] = useState('');
  const [catSaving, setCatSaving] = useState(false);
  const [catError, setCatError] = useState<string | null>(null);

  const cost = Number(costPrice);
  const price = Number(sellingPrice);
  const margin = price > 0 && cost >= 0 ? price - cost : 0;

  const canSubmit =
    name.trim().length > 0 && price > 0 && cost >= 0 && categoryId !== '';

  const handleAddCategory = async () => {
    const trimmed = newCategory.trim();
    if (!trimmed) return;
    setCatSaving(true);
    setCatError(null);
    try {
      const id = await createCategory(trimmed);
      setCategoryId(id);
      setNewCategory('');
      setAddingCategory(false);
    } catch (err) {
      setCatError(getApiErrorMessage(err, 'Could not create category.'));
    } finally {
      setCatSaving(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!canSubmit) return;

    const payload = {
      name: name.trim(),
      description: description.trim() || undefined,
      sku: sku.trim() || undefined,
      costPrice: cost,
      sellingPrice: price,
      lowStockThreshold: parseInt(lowStockThreshold, 10) || 0,
      categoryId,
    };

    const ok = item
      ? await updateItem(item.id, { ...payload, isActive })
      : await createItem(payload);

    if (ok) onSaved();
  };

  return (
    <Modal
      title={isEdit ? 'Edit item' : 'New item'}
      subtitle={isEdit ? item!.name : 'Add a product to your catalog'}
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
          <label htmlFor="name">Item name</label>
          <input
            id="name"
            className="input"
            type="text"
            placeholder="e.g. Kopiko Black 3-in-1"
            value={name}
            onChange={(e) => setName(e.target.value)}
            autoFocus
            required
          />
        </div>

        <div className="field">
          <label htmlFor="sku">SKU / barcode (optional)</label>
          <input
            id="sku"
            className="input"
            type="text"
            placeholder="e.g. 4800016641234"
            value={sku}
            onChange={(e) => setSku(e.target.value)}
          />
        </div>

        <div className="field">
          <label htmlFor="description">Description (optional)</label>
          <input
            id="description"
            className="input"
            type="text"
            placeholder="Short note about this item"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
        </div>

        <div className="form-grid">
          <div className="field">
            <label htmlFor="cost">Cost price</label>
            <input
              id="cost"
              className="input"
              type="number"
              min="0"
              step="0.01"
              placeholder="0.00"
              value={costPrice}
              onChange={(e) => setCostPrice(e.target.value)}
              required
            />
          </div>
          <div className="field">
            <label htmlFor="price">Selling price</label>
            <input
              id="price"
              className="input"
              type="number"
              min="0"
              step="0.01"
              placeholder="0.00"
              value={sellingPrice}
              onChange={(e) => setSellingPrice(e.target.value)}
              required
            />
          </div>
        </div>

        <div className="field">
          <label htmlFor="threshold">Low-stock threshold</label>
          <input
            id="threshold"
            className="input"
            type="number"
            min="0"
            step="1"
            placeholder="0"
            value={lowStockThreshold}
            onChange={(e) => setLowStockThreshold(e.target.value)}
          />
        </div>

        <div className="field">
          <label htmlFor="category">Category</label>
          {!addingCategory && (
            <select
              id="category"
              className="input"
              value={categoryId}
              onChange={(e) => {
                if (e.target.value === '__add__') {
                  setAddingCategory(true);
                  return;
                }
                setCategoryId(e.target.value);
              }}
              required
            >
              <option value="" disabled>
                Select a category…
              </option>
              {categories.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
              <option value="__add__">+ New category…</option>
            </select>
          )}

          {addingCategory && (
            <>
              <div className="cat-add-row">
                <input
                  className="input"
                  type="text"
                  placeholder="New category name"
                  value={newCategory}
                  onChange={(e) => setNewCategory(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') {
                      e.preventDefault();
                      handleAddCategory();
                    }
                  }}
                />
                <button
                  type="button"
                  className="btn btn-ghost btn-sm"
                  onClick={handleAddCategory}
                  disabled={catSaving || !newCategory.trim()}
                >
                  {catSaving ? 'Adding…' : 'Add'}
                </button>
                {categories.length > 0 && (
                  <button
                    type="button"
                    className="btn btn-quiet btn-sm"
                    onClick={() => {
                      setAddingCategory(false);
                      setCatError(null);
                    }}
                    disabled={catSaving}
                  >
                    Cancel
                  </button>
                )}
              </div>
              {catError && (
                <p className="field-hint text-red">{catError}</p>
              )}
            </>
          )}
        </div>

        {isEdit && (
          <div className="field">
            <label>Status</label>
            <div className="segmented">
              <button
                type="button"
                className={isActive ? 'seg is-active' : 'seg'}
                onClick={() => setIsActive(true)}
              >
                Active
              </button>
              <button
                type="button"
                className={!isActive ? 'seg is-active' : 'seg'}
                onClick={() => setIsActive(false)}
              >
                Inactive
              </button>
            </div>
          </div>
        )}

        <div className="modal-summary">
          <span>Margin per unit</span>
          <strong className="tnum">{peso.format(margin)}</strong>
        </div>

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
            {loading ? 'Saving…' : isEdit ? 'Save changes' : 'Create item'}
          </button>
        </div>
      </form>
    </Modal>
  );
};

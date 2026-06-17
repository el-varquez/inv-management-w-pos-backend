import { useState } from 'react';
import { Modal } from '../../../components/Modal';
import { useCategoryMutations } from '../hooks/useCategoryMutations';
import type { Category } from '../../../types';

interface Props {
  /** The category to edit, or null/undefined to create a new one. */
  category?: Category | null;
  onClose: () => void;
  onSaved: () => void;
}

export const CategoryFormModal = ({ category, onClose, onSaved }: Props) => {
  const isEdit = !!category;
  const { createCategory, updateCategory, loading, error } =
    useCategoryMutations();

  const [name, setName] = useState(category?.name ?? '');
  const [description, setDescription] = useState(category?.description ?? '');

  const canSubmit = name.trim().length > 0;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!canSubmit) return;

    const payload = {
      name: name.trim(),
      description: description.trim() || undefined,
    };

    const ok = category
      ? await updateCategory(category.id, payload)
      : await createCategory(payload);

    if (ok) onSaved();
  };

  return (
    <Modal
      title={isEdit ? 'Edit category' : 'New category'}
      subtitle={isEdit ? category!.name : 'Group items in your catalog'}
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
          <label htmlFor="cat-name">Category name</label>
          <input
            id="cat-name"
            className="input"
            type="text"
            placeholder="e.g. Beverages"
            value={name}
            onChange={(e) => setName(e.target.value)}
            autoFocus
            required
          />
        </div>

        <div className="field">
          <label htmlFor="cat-desc">Description (optional)</label>
          <input
            id="cat-desc"
            className="input"
            type="text"
            placeholder="Short note about this category"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
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
            {loading ? 'Saving…' : isEdit ? 'Save changes' : 'Create category'}
          </button>
        </div>
      </form>
    </Modal>
  );
};

import { Modal } from '../../../components/Modal';
import { useCategoryMutations } from '../hooks/useCategoryMutations';
import type { Category } from '../../../types';

interface Props {
  category: Category;
  onClose: () => void;
  onDeleted: () => void;
}

export const DeleteCategoryModal = ({ category, onClose, onDeleted }: Props) => {
  const { deleteCategory, loading, error } = useCategoryMutations();

  const blocked = category.itemCount > 0;

  const handleDelete = async () => {
    const ok = await deleteCategory(category.id);
    if (ok) onDeleted();
  };

  return (
    <Modal title="Delete category" subtitle={category.name} onClose={onClose}>
      {error && (
        <div className="login-error" role="alert">
          <span aria-hidden="true">⚠</span>
          {error}
        </div>
      )}

      {blocked ? (
        <>
          <p className="state-msg" style={{ margin: '0 0 8px' }}>
            <strong>{category.name}</strong> still has{' '}
            <strong>
              {category.itemCount} item{category.itemCount === 1 ? '' : 's'}
            </strong>
            . Move those items to another category (or delete them) first, then
            you can remove this category.
          </p>
          <div className="modal-actions">
            <button type="button" className="btn btn-primary" onClick={onClose}>
              Close
            </button>
          </div>
        </>
      ) : (
        <>
          <p className="state-msg" style={{ margin: '0 0 8px' }}>
            This permanently removes the <strong>{category.name}</strong>{' '}
            category. This can’t be undone.
          </p>
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
              type="button"
              className="btn btn-danger"
              onClick={handleDelete}
              disabled={loading}
            >
              {loading ? (
                <span className="spinner spinner-dark" aria-hidden="true" />
              ) : null}
              {loading ? 'Deleting…' : 'Delete category'}
            </button>
          </div>
        </>
      )}
    </Modal>
  );
};

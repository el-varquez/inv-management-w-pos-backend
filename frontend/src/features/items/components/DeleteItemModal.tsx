import { Modal } from '../../../components/Modal';
import { useItemMutations } from '../hooks/useItemMutations';
import type { Item } from '../../../types';

interface Props {
  item: Item;
  onClose: () => void;
  onDeleted: () => void;
}

export const DeleteItemModal = ({ item, onClose, onDeleted }: Props) => {
  const { deleteItem, loading, error } = useItemMutations();

  const handleDelete = async () => {
    const ok = await deleteItem(item.id);
    if (ok) onDeleted();
  };

  return (
    <Modal
      title="Delete item"
      subtitle={item.name}
      onClose={onClose}
    >
      {error && (
        <div className="login-error" role="alert">
          <span aria-hidden="true">⚠</span>
          {error}
        </div>
      )}

      <p className="state-msg" style={{ margin: '0 0 8px' }}>
        This removes <strong>{item.name}</strong> from your catalog. Past sales
        that reference it are unaffected. This can’t be undone.
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
          {loading ? <span className="spinner spinner-dark" aria-hidden="true" /> : null}
          {loading ? 'Deleting…' : 'Delete item'}
        </button>
      </div>
    </Modal>
  );
};

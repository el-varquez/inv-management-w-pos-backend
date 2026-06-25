import { Modal } from '../../../components/Modal';
import { useCashierMutations } from '../hooks/useCashierMutations';
import type { Cashier } from '../../../types';

interface Props {
  cashier: Cashier;
  mode: 'deactivate' | 'reactivate';
  onClose: () => void;
  onDone: () => void;
}

export const CashierStatusModal = ({ cashier, mode, onClose, onDone }: Props) => {
  const { deactivateCashier, reactivateCashier, loading, error } =
    useCashierMutations();

  const isDeactivate = mode === 'deactivate';

  const handleConfirm = async () => {
    const ok = isDeactivate
      ? await deactivateCashier(cashier.id)
      : await reactivateCashier(cashier.id);
    if (ok) onDone();
  };

  return (
    <Modal
      title={isDeactivate ? 'Deactivate cashier' : 'Reactivate cashier'}
      subtitle={cashier.name}
      onClose={onClose}
    >
      {error && (
        <div className="login-error" role="alert">
          <span aria-hidden="true">⚠</span>
          {error}
        </div>
      )}

      <p className="state-msg" style={{ margin: '0 0 8px' }}>
        {isDeactivate ? (
          <>
            <strong>{cashier.name}</strong> won’t be able to sign in to the
            mobile app, and a cashier slot will be freed up. Their past sales
            stay on record. You can reactivate them later.
          </>
        ) : (
          <>
            <strong>{cashier.name}</strong> will be able to sign in again and
            will use one of your cashier slots.
          </>
        )}
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
          className={isDeactivate ? 'btn btn-danger' : 'btn btn-primary'}
          onClick={handleConfirm}
          disabled={loading}
        >
          {loading ? (
            <span
              className={isDeactivate ? 'spinner spinner-dark' : 'spinner'}
              aria-hidden="true"
            />
          ) : null}
          {loading
            ? isDeactivate
              ? 'Deactivating…'
              : 'Reactivating…'
            : isDeactivate
              ? 'Deactivate'
              : 'Reactivate'}
        </button>
      </div>
    </Modal>
  );
};

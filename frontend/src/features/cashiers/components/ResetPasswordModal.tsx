import { useState } from 'react';
import { Modal } from '../../../components/Modal';
import { useCashierMutations } from '../hooks/useCashierMutations';
import type { Cashier } from '../../../types';

interface Props {
  cashier: Cashier;
  onClose: () => void;
  onDone: () => void;
}

export const ResetPasswordModal = ({ cashier, onClose, onDone }: Props) => {
  const { resetPassword, loading, error } = useCashierMutations();
  const [password, setPassword] = useState('');

  const canSubmit = password.length >= 8;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!canSubmit) return;
    const ok = await resetPassword(cashier.id, password);
    if (ok) onDone();
  };

  return (
    <Modal
      title="Reset password"
      subtitle={cashier.name}
      onClose={onClose}
    >
      <form onSubmit={handleSubmit}>
        {error && (
          <div className="login-error" role="alert">
            <span aria-hidden="true">⚠</span>
            {error}
          </div>
        )}

        <p className="state-msg" style={{ margin: '0 0 12px' }}>
          Set a new password for <strong>{cashier.email}</strong>. Share it with
          them directly — they’ll use it to sign in on the mobile app.
        </p>

        <div className="field">
          <label htmlFor="reset-password">New password</label>
          <input
            id="reset-password"
            className="input"
            type="password"
            autoComplete="new-password"
            placeholder="At least 8 characters"
            minLength={8}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoFocus
            required
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
            {loading ? 'Saving…' : 'Set new password'}
          </button>
        </div>
      </form>
    </Modal>
  );
};

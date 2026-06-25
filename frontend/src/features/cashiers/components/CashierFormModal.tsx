import { useState } from 'react';
import { Modal } from '../../../components/Modal';
import { useCashierMutations } from '../hooks/useCashierMutations';

interface Props {
  onClose: () => void;
  onSaved: () => void;
}

export const CashierFormModal = ({ onClose, onSaved }: Props) => {
  const { createCashier, loading, error } = useCashierMutations();

  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  const canSubmit =
    name.trim().length > 0 &&
    email.trim().length > 0 &&
    password.length >= 8;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!canSubmit) return;

    const ok = await createCashier({
      name: name.trim(),
      email: email.trim(),
      password,
    });

    if (ok) onSaved();
  };

  return (
    <Modal
      title="New cashier"
      subtitle="Set up a cashier account for your store"
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
          <label htmlFor="cashier-name">Name</label>
          <input
            id="cashier-name"
            className="input"
            type="text"
            placeholder="e.g. Maria Santos"
            value={name}
            onChange={(e) => setName(e.target.value)}
            autoFocus
            required
          />
        </div>

        <div className="field">
          <label htmlFor="cashier-email">Email</label>
          <input
            id="cashier-email"
            className="input"
            type="email"
            autoComplete="off"
            placeholder="cashier@store.ph"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
        </div>

        <div className="field">
          <label htmlFor="cashier-password">Password</label>
          <input
            id="cashier-password"
            className="input"
            type="password"
            autoComplete="new-password"
            placeholder="At least 8 characters"
            minLength={8}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
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
            {loading ? 'Creating…' : 'Create cashier'}
          </button>
        </div>
      </form>
    </Modal>
  );
};

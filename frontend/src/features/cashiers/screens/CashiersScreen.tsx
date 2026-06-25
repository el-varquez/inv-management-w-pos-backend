import { useState } from 'react';
import { useCashiers } from '../hooks/useCashiers';
import { CashierFormModal } from '../components/CashierFormModal';
import { CashierStatusModal } from '../components/CashierStatusModal';
import { ResetPasswordModal } from '../components/ResetPasswordModal';
import type { Cashier } from '../../../types';

const SKELETON_ROWS = Array.from({ length: 5 });

type ModalState =
  | { kind: 'create' }
  | { kind: 'deactivate'; cashier: Cashier }
  | { kind: 'reactivate'; cashier: Cashier }
  | { kind: 'reset'; cashier: Cashier }
  | null;

export const CashiersScreen = () => {
  const { cashiers, activeCount, cashierCap, loading, error, refetch } =
    useCashiers();
  const [modal, setModal] = useState<ModalState>(null);

  const atCap = cashierCap > 0 && activeCount >= cashierCap;

  const closeAndRefresh = () => {
    setModal(null);
    refetch();
  };

  return (
    <>
      <div className="page-head">
        <div>
          <p className="eyebrow">Team</p>
          <h1 className="page-title">Cashiers</h1>
          <p className="page-lead">
            {loading
              ? 'Loading cashiers…'
              : error
                ? 'Could not load cashiers.'
                : `${activeCount} of ${cashierCap} active cashier${cashierCap === 1 ? '' : 's'}`}
          </p>
        </div>
        <div className="page-actions">
          <button className="btn btn-ghost" onClick={refetch} disabled={loading}>
            Refresh
          </button>
          <button
            className="btn btn-primary"
            onClick={() => setModal({ kind: 'create' })}
            disabled={atCap}
            title={atCap ? 'Deactivate a cashier to free up a slot' : undefined}
          >
            New cashier
          </button>
        </div>
      </div>

      {atCap && !loading && !error && (
        <div className="login-error" role="status" style={{ marginBottom: 16 }}>
          <span aria-hidden="true">⚠</span>
          You’ve reached your cashier limit of {cashierCap}. Deactivate a cashier
          to add a new one.
        </div>
      )}

      <div className="card table-wrap">
        {error ? (
          <div className="state state-error">
            <div className="state-emoji">⚠️</div>
            <div className="state-title">Something went wrong</div>
            <p className="state-msg">{error}</p>
            <button className="btn btn-ghost" onClick={refetch}>
              Try again
            </button>
          </div>
        ) : loading ? (
          <CashiersTable>
            {SKELETON_ROWS.map((_, i) => (
              <tr key={i}>
                <td>
                  <span className="skeleton" style={{ width: '50%' }} />
                </td>
                <td>
                  <span className="skeleton" style={{ width: '70%' }} />
                </td>
                <td>
                  <span className="skeleton" style={{ width: 60 }} />
                </td>
                <td className="num">
                  <span className="skeleton" style={{ width: 120, marginLeft: 'auto' }} />
                </td>
              </tr>
            ))}
          </CashiersTable>
        ) : cashiers.length === 0 ? (
          <div className="state">
            <div className="state-emoji">🧑‍💼</div>
            <div className="state-title">No cashiers yet</div>
            <p className="state-msg">
              Add cashier accounts so your team can ring up sales on the mobile
              app.
            </p>
            <button
              className="btn btn-primary"
              onClick={() => setModal({ kind: 'create' })}
            >
              New cashier
            </button>
          </div>
        ) : (
          <CashiersTable>
            {cashiers.map((c) => (
              <tr key={c.id}>
                <td>
                  <div className="item-name">{c.name}</div>
                </td>
                <td className="item-sub">{c.email}</td>
                <td>
                  <span className={`badge ${c.isActive ? 'badge-ok' : 'badge-muted'}`}>
                    {c.isActive ? 'Active' : 'Inactive'}
                  </span>
                </td>
                <td className="num">
                  <div className="row-actions">
                    <button
                      className="btn btn-ghost btn-sm"
                      onClick={() => setModal({ kind: 'reset', cashier: c })}
                    >
                      Reset password
                    </button>
                    {c.isActive ? (
                      <button
                        className="btn btn-quiet btn-sm"
                        onClick={() => setModal({ kind: 'deactivate', cashier: c })}
                      >
                        Deactivate
                      </button>
                    ) : (
                      <button
                        className="btn btn-ghost btn-sm"
                        onClick={() => setModal({ kind: 'reactivate', cashier: c })}
                      >
                        Reactivate
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </CashiersTable>
        )}
      </div>

      {modal?.kind === 'create' && (
        <CashierFormModal
          onClose={() => setModal(null)}
          onSaved={closeAndRefresh}
        />
      )}
      {modal?.kind === 'deactivate' && (
        <CashierStatusModal
          cashier={modal.cashier}
          mode="deactivate"
          onClose={() => setModal(null)}
          onDone={closeAndRefresh}
        />
      )}
      {modal?.kind === 'reactivate' && (
        <CashierStatusModal
          cashier={modal.cashier}
          mode="reactivate"
          onClose={() => setModal(null)}
          onDone={closeAndRefresh}
        />
      )}
      {modal?.kind === 'reset' && (
        <ResetPasswordModal
          cashier={modal.cashier}
          onClose={() => setModal(null)}
          onDone={() => setModal(null)}
        />
      )}
    </>
  );
};

const CashiersTable = ({ children }: { children: React.ReactNode }) => (
  <table className="ledger">
    <thead>
      <tr>
        <th>Name</th>
        <th>Email</th>
        <th>Status</th>
        <th className="num">Actions</th>
      </tr>
    </thead>
    <tbody>{children}</tbody>
  </table>
);

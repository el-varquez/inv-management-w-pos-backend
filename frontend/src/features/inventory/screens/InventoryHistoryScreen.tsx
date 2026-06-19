import { useState } from 'react';
import {
  useInventoryHistory,
  type InventoryHistoryFilters,
} from '../hooks/useInventoryHistory';
import { InventoryTabs } from '../components/InventoryTabs';
import { Pagination } from '../../../components/Pagination';
import { SearchSelect } from '../../../components/SearchSelect';
import { peso, formatDateTime, signed } from '../../../lib/format';
import type { StockMovementType } from '../../../types';

const SKELETON_ROWS = Array.from({ length: 6 });

const MOVEMENT_TYPES: { value: StockMovementType | ''; label: string }[] = [
  { value: '', label: 'All movements' },
  { value: 'AddStock', label: 'Add stock' },
  { value: 'Sale', label: 'Sale' },
  { value: 'Adjustment', label: 'Adjustment' },
  { value: 'InventoryCount', label: 'Inventory count' },
  { value: 'Return', label: 'Return' },
];

export const InventoryHistoryScreen = () => {
  const [filters, setFilters] = useState<InventoryHistoryFilters>({});
  const {
    history,
    loading,
    error,
    refetch,
    page,
    setPage,
    pageSize,
    totalCount,
    totalPages,
  } = useInventoryHistory(filters);

  const update = (patch: Partial<InventoryHistoryFilters>) =>
    setFilters((f) => ({ ...f, ...patch }));

  return (
    <>
      <div className="page-head">
        <div>
          <p className="eyebrow">Inventory · Ledger</p>
          <h1 className="page-title">History</h1>
          <p className="page-lead">
            {loading
              ? 'Loading movements…'
              : error
                ? 'Could not load history.'
                : `${totalCount} movement${totalCount === 1 ? '' : 's'}`}
          </p>
        </div>
        <div className="page-actions">
          <button className="btn btn-ghost" onClick={refetch} disabled={loading}>
            Refresh
          </button>
        </div>
      </div>

      <InventoryTabs />

      <div className="filter-bar card">
        <div className="field">
          <label htmlFor="from">From</label>
          <input
            id="from"
            className="input"
            type="date"
            value={filters.from ?? ''}
            onChange={(e) => update({ from: e.target.value || undefined })}
          />
        </div>
        <div className="field">
          <label htmlFor="to">To</label>
          <input
            id="to"
            className="input"
            type="date"
            value={filters.to ?? ''}
            onChange={(e) => update({ to: e.target.value || undefined })}
          />
        </div>
        <div className="field">
          <label htmlFor="type">Type</label>
          <SearchSelect
            id="type"
            value={filters.type ?? ''}
            onChange={(v) => update({ type: v || undefined })}
            allLabel="All movements"
            options={MOVEMENT_TYPES.filter((t) => t.value !== '').map((t) => ({
              value: t.value,
              label: t.label,
            }))}
          />
        </div>
        {(filters.from || filters.to || filters.type) && (
          <button
            type="button"
            className="btn btn-quiet btn-sm filter-clear"
            onClick={() => setFilters({})}
          >
            Clear filters
          </button>
        )}
      </div>

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
          <HistoryTable>
            {SKELETON_ROWS.map((_, i) => (
              <tr key={i}>
                <td>
                  <span className="skeleton" style={{ width: 120 }} />
                </td>
                <td>
                  <span className="skeleton" style={{ width: '60%' }} />
                </td>
                <td>
                  <span className="skeleton" style={{ width: 90 }} />
                </td>
                <td className="num">
                  <span className="skeleton" style={{ width: 40, marginLeft: 'auto' }} />
                </td>
                <td className="num">
                  <span className="skeleton" style={{ width: 80, marginLeft: 'auto' }} />
                </td>
              </tr>
            ))}
          </HistoryTable>
        ) : history.length === 0 ? (
          <div className="state">
            <div className="state-emoji">🧾</div>
            <div className="state-title">No movements</div>
            <p className="state-msg">
              No stock movements match the current filters.
            </p>
          </div>
        ) : (
          <HistoryTable>
            {history.map((m) => (
              <tr key={m.id}>
                <td className="item-sub">{formatDateTime(m.createdAt)}</td>
                <td>
                  <div className="item-name">{m.itemName}</div>
                  <div className="item-sub">
                    {m.reason ?? m.supplierName ?? m.notes ?? m.categoryName}
                  </div>
                </td>
                <td>
                  <MovementBadge type={m.movementType} />
                </td>
                <td className="num">
                  <span
                    className={`tnum qty-delta ${m.quantity < 0 ? 'text-red' : 'text-green'}`}
                  >
                    {signed(m.quantity)}
                  </span>
                </td>
                <td className="num tnum cost">
                  {m.totalCost != null ? peso.format(m.totalCost) : '—'}
                </td>
              </tr>
            ))}
          </HistoryTable>
        )}
      </div>

      {!loading && !error && (
        <Pagination
          page={page}
          pageSize={pageSize}
          totalCount={totalCount}
          totalPages={totalPages}
          onPageChange={setPage}
        />
      )}
    </>
  );
};

const MovementBadge = ({ type }: { type: string }) => {
  const cls =
    type === 'AddStock' || type === 'Return'
      ? 'badge badge-ok'
      : type === 'Sale'
        ? 'badge badge-muted'
        : 'badge badge-low';
  const label = type.replace(/([a-z])([A-Z])/g, '$1 $2');
  return <span className={cls}>{label}</span>;
};

const HistoryTable = ({ children }: { children: React.ReactNode }) => (
  <table className="ledger">
    <thead>
      <tr>
        <th>Date</th>
        <th>Item</th>
        <th>Type</th>
        <th className="num">Qty</th>
        <th className="num">Cost</th>
      </tr>
    </thead>
    <tbody>{children}</tbody>
  </table>
);

import { useState } from 'react';
import { useSalesHistory } from '../hooks/useSalesHistory';
import { SalesTabs } from '../components/SalesTabs';
import { TransactionDetailModal } from '../components/TransactionDetailModal';
import { Pagination } from '../../../components/Pagination';
import type { SalesFilters } from '../services/salesService';
import { peso, formatDateTime } from '../../../lib/format';
import type { Transaction } from '../../../types';

const SKELETON_ROWS = Array.from({ length: 6 });

export const SalesHistoryScreen = () => {
  const [filters, setFilters] = useState<SalesFilters>({});
  const {
    transactions,
    summary,
    loading,
    error,
    refetch,
    refund,
    page,
    setPage,
    pageSize,
    totalCount,
    totalPages,
  } = useSalesHistory(filters);
  const [selected, setSelected] = useState<Transaction | null>(null);

  const update = (patch: Partial<SalesFilters>) =>
    setFilters((f) => ({ ...f, ...patch }));

  return (
    <>
      <div className="page-head">
        <div>
          <p className="eyebrow">Sales · Ledger</p>
          <h1 className="page-title">Sales history</h1>
          <p className="page-lead">
            {loading
              ? 'Loading sales…'
              : error
                ? 'Could not load sales.'
                : `${totalCount} transaction${totalCount === 1 ? '' : 's'}`}
          </p>
        </div>
        <div className="page-actions">
          <button className="btn btn-ghost" onClick={refetch} disabled={loading}>
            Refresh
          </button>
        </div>
      </div>

      <SalesTabs />

      {summary && (
        <div className="stat-row">
          <div className="card stat-card">
            <div className="stat-label">Gross sales</div>
            <div className="stat-value tnum">{peso.format(summary.grossSales)}</div>
          </div>
          <div className="card stat-card">
            <div className="stat-label">Discounts</div>
            <div className="stat-value tnum">
              {peso.format(summary.totalDiscounts)}
            </div>
          </div>
          <div className="card stat-card">
            <div className="stat-label">Refunds</div>
            <div className="stat-value tnum">{peso.format(summary.refunds)}</div>
          </div>
          <div className="card stat-card">
            <div className="stat-label">Net sales</div>
            <div className="stat-value tnum">{peso.format(summary.netSales)}</div>
          </div>
        </div>
      )}

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
        {(filters.from || filters.to) && (
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
                  <span className="skeleton" style={{ width: 110 }} />
                </td>
                <td>
                  <span className="skeleton" style={{ width: 60 }} />
                </td>
                <td className="num">
                  <span className="skeleton" style={{ width: 40, marginLeft: 'auto' }} />
                </td>
                <td className="num">
                  <span className="skeleton" style={{ width: 80, marginLeft: 'auto' }} />
                </td>
                <td className="num">
                  <span className="skeleton" style={{ width: 70, marginLeft: 'auto' }} />
                </td>
              </tr>
            ))}
          </HistoryTable>
        ) : transactions.length === 0 ? (
          <div className="state">
            <div className="state-emoji">🧾</div>
            <div className="state-title">No sales yet</div>
            <p className="state-msg">
              Sales you ring up on the register will appear here.
            </p>
          </div>
        ) : (
          <HistoryTable>
            {transactions.map((t) => {
              const isRefund = (t.refundedFromId ?? null) !== null;
              return (
                <tr key={t.id}>
                  <td className="item-sub">{formatDateTime(t.createdAt)}</td>
                  <td>
                    <div className="item-name">{t.receiptNumber}</div>
                    <div className="item-sub">
                      {t.itemCount} item{t.itemCount === 1 ? '' : 's'}
                    </div>
                  </td>
                  <td>
                    <span className="cat-pill">{t.paymentType}</span>
                  </td>
                  <td className="num tnum">{t.itemCount}</td>
                  <td
                    className={`num tnum ${isRefund ? 'text-red' : ''}`}
                    style={{ fontWeight: 600 }}
                  >
                    {peso.format(t.total)}
                  </td>
                  <td className="num">
                    <div className="row-actions">
                      {isRefund ? (
                        <span className="badge badge-muted">Refund</span>
                      ) : t.isRefunded ? (
                        <span className="badge badge-low">Refunded</span>
                      ) : (
                        <span className="badge badge-ok">Paid</span>
                      )}
                      <button
                        className="btn btn-quiet btn-sm"
                        onClick={() => setSelected(t)}
                      >
                        View
                      </button>
                    </div>
                  </td>
                </tr>
              );
            })}
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

      {selected && (
        <TransactionDetailModal
          transaction={selected}
          onClose={() => setSelected(null)}
          onRefund={refund}
        />
      )}
    </>
  );
};

const HistoryTable = ({ children }: { children: React.ReactNode }) => (
  <table className="ledger">
    <thead>
      <tr>
        <th>Date</th>
        <th>Receipt</th>
        <th>Payment</th>
        <th className="num">Items</th>
        <th className="num">Total</th>
        <th className="num">Status</th>
      </tr>
    </thead>
    <tbody>{children}</tbody>
  </table>
);

import { useState } from 'react';
import { useStockLevels } from '../hooks/useStockLevels';
import { InventoryTabs } from '../components/InventoryTabs';
import { AddStockModal } from '../components/AddStockModal';
import { AdjustStockModal } from '../components/AdjustStockModal';
import { Pagination } from '../../../components/Pagination';
import { peso } from '../../../lib/format';
import type { StockLevel } from '../../../types';

const SKELETON_ROWS = Array.from({ length: 5 });

type ModalState =
  | { kind: 'add'; item: StockLevel }
  | { kind: 'adjust'; item: StockLevel }
  | null;

export const StockLevelsScreen = () => {
  const {
    stockLevels,
    loading,
    error,
    refetch,
    page,
    setPage,
    pageSize,
    totalCount,
    totalPages,
  } = useStockLevels();
  const [modal, setModal] = useState<ModalState>(null);

  const closeAndRefresh = () => {
    setModal(null);
    refetch();
  };

  return (
    <>
      <div className="page-head">
        <div>
          <p className="eyebrow">Inventory · Stock</p>
          <h1 className="page-title">Stock levels</h1>
          <p className="page-lead">
            {loading
              ? 'Loading stock…'
              : error
                ? 'Could not load stock levels.'
                : `${totalCount} item${totalCount === 1 ? '' : 's'}`}
          </p>
        </div>
        <div className="page-actions">
          <button className="btn btn-ghost" onClick={refetch} disabled={loading}>
            Refresh
          </button>
        </div>
      </div>

      <InventoryTabs />

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
          <StockTable>
            {SKELETON_ROWS.map((_, i) => (
              <tr key={i}>
                <td>
                  <span className="skeleton" style={{ width: '60%' }} />
                </td>
                <td className="item-sub-cat">
                  <span className="skeleton" style={{ width: 70 }} />
                </td>
                <td className="num">
                  <span className="skeleton" style={{ width: 50, marginLeft: 'auto' }} />
                </td>
                <td className="num">
                  <span className="skeleton" style={{ width: 80, marginLeft: 'auto' }} />
                </td>
                <td className="num">
                  <span className="skeleton" style={{ width: 120, marginLeft: 'auto' }} />
                </td>
              </tr>
            ))}
          </StockTable>
        ) : stockLevels.length === 0 ? (
          <div className="state">
            <div className="state-emoji">📦</div>
            <div className="state-title">No items to track</div>
            <p className="state-msg">
              Add items to your catalog first, then come back to manage their
              stock.
            </p>
          </div>
        ) : (
          <StockTable>
            {stockLevels.map((s) => (
              <tr key={s.itemId}>
                <td>
                  <div className="item-name">{s.itemName}</div>
                  <div className="item-sub">{peso.format(s.costPrice)} cost</div>
                </td>
                <td className="item-sub-cat">
                  <span className="cat-pill">{s.categoryName}</span>
                </td>
                <td className="num">
                  <span className="stock-cell">
                    <span className="stock-num tnum">{s.stock}</span>
                    {s.isLowStock ? (
                      <span className="badge badge-low">Low</span>
                    ) : (
                      <span className="badge badge-ok">OK</span>
                    )}
                  </span>
                </td>
                <td className="num tnum cost">{peso.format(s.stockValue)}</td>
                <td className="num">
                  <div className="row-actions">
                    <button
                      className="btn btn-ghost btn-sm"
                      onClick={() => setModal({ kind: 'add', item: s })}
                    >
                      Add stock
                    </button>
                    <button
                      className="btn btn-quiet btn-sm"
                      onClick={() => setModal({ kind: 'adjust', item: s })}
                    >
                      Adjust
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </StockTable>
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

      {modal?.kind === 'add' && (
        <AddStockModal
          item={modal.item}
          onClose={() => setModal(null)}
          onDone={closeAndRefresh}
        />
      )}
      {modal?.kind === 'adjust' && (
        <AdjustStockModal
          item={modal.item}
          onClose={() => setModal(null)}
          onDone={closeAndRefresh}
        />
      )}
    </>
  );
};

const StockTable = ({ children }: { children: React.ReactNode }) => (
  <table className="ledger">
    <thead>
      <tr>
        <th>Item</th>
        <th className="item-sub-cat">Category</th>
        <th className="num">On hand</th>
        <th className="num">Stock value</th>
        <th className="num">Actions</th>
      </tr>
    </thead>
    <tbody>{children}</tbody>
  </table>
);

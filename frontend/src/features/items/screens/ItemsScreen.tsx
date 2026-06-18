import { useState } from 'react';
import { useItems } from '../hooks/useItems';
import { useCategories } from '../hooks/useCategories';
import { useAuth } from '../../auth/hooks/useAuth';
import { ItemsTabs } from '../components/ItemsTabs';
import { ItemFormModal } from '../components/ItemFormModal';
import { DeleteItemModal } from '../components/DeleteItemModal';
import { Pagination } from '../../../components/Pagination';
import { peso } from '../../../lib/format';
import type { Item } from '../../../types';

const SKELETON_ROWS = Array.from({ length: 5 });

type ModalState =
  | { kind: 'create' }
  | { kind: 'edit'; item: Item }
  | { kind: 'delete'; item: Item }
  | null;

export const ItemsScreen = () => {
  const {
    items,
    loading,
    error,
    refetch,
    page,
    setPage,
    pageSize,
    totalCount,
    totalPages,
  } = useItems();
  const { categories, createCategory } = useCategories();
  const { user } = useAuth();
  const [modal, setModal] = useState<ModalState>(null);

  const isAdmin = user?.role === 'Admin';

  const closeAndRefresh = () => {
    setModal(null);
    refetch();
  };

  return (
    <>
      <div className="page-head">
        <div>
          <p className="eyebrow">Inventory · Catalog</p>
          <h1 className="page-title">Items</h1>
          <p className="page-lead">
            {loading
              ? 'Loading your catalog…'
              : error
                ? 'Could not load items.'
                : `${totalCount} item${totalCount === 1 ? '' : 's'}`}
          </p>
        </div>
        <div className="page-actions">
          <button className="btn btn-ghost" onClick={refetch} disabled={loading}>
            Refresh
          </button>
          {isAdmin && (
            <button
              className="btn btn-primary"
              onClick={() => setModal({ kind: 'create' })}
            >
              New item
            </button>
          )}
        </div>
      </div>

      <ItemsTabs />

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
          <ItemsTable isAdmin={isAdmin}>
            {SKELETON_ROWS.map((_, i) => (
              <tr key={i}>
                <td>
                  <span className="skeleton" style={{ width: '60%' }} />
                </td>
                <td className="item-sub-cat">
                  <span className="skeleton" style={{ width: 70 }} />
                </td>
                <td className="num">
                  <span className="skeleton" style={{ width: 56, marginLeft: 'auto' }} />
                </td>
                <td className="num">
                  <span className="skeleton" style={{ width: 56, marginLeft: 'auto' }} />
                </td>
                <td className="num">
                  <span className="skeleton" style={{ width: 80, marginLeft: 'auto' }} />
                </td>
                {isAdmin && (
                  <td className="num">
                    <span className="skeleton" style={{ width: 90, marginLeft: 'auto' }} />
                  </td>
                )}
              </tr>
            ))}
          </ItemsTable>
        ) : items.length === 0 ? (
          <div className="state">
            <div className="state-emoji">🗂️</div>
            <div className="state-title">No items yet</div>
            <p className="state-msg">
              Your catalog is empty. Add your first product to start tracking
              stock and sales.
            </p>
            {isAdmin && (
              <button
                className="btn btn-primary"
                onClick={() => setModal({ kind: 'create' })}
              >
                New item
              </button>
            )}
          </div>
        ) : (
          <ItemsTable isAdmin={isAdmin}>
            {items.map((item) => (
              <ItemRow
                key={item.id}
                item={item}
                isAdmin={isAdmin}
                onEdit={() => setModal({ kind: 'edit', item })}
                onDelete={() => setModal({ kind: 'delete', item })}
              />
            ))}
          </ItemsTable>
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

      {modal?.kind === 'create' && (
        <ItemFormModal
          categories={categories}
          createCategory={createCategory}
          onClose={() => setModal(null)}
          onSaved={closeAndRefresh}
        />
      )}
      {modal?.kind === 'edit' && (
        <ItemFormModal
          item={modal.item}
          categories={categories}
          createCategory={createCategory}
          onClose={() => setModal(null)}
          onSaved={closeAndRefresh}
        />
      )}
      {modal?.kind === 'delete' && (
        <DeleteItemModal
          item={modal.item}
          onClose={() => setModal(null)}
          onDeleted={closeAndRefresh}
        />
      )}
    </>
  );
};

const ItemsTable = ({
  isAdmin,
  children,
}: {
  isAdmin: boolean;
  children: React.ReactNode;
}) => (
  <table className="ledger">
    <thead>
      <tr>
        <th>Item</th>
        <th className="item-sub-cat">Category</th>
        <th className="num">Cost</th>
        <th className="num">Price</th>
        <th className="num">Stock</th>
        {isAdmin && <th className="num">Actions</th>}
      </tr>
    </thead>
    <tbody>{children}</tbody>
  </table>
);

const ItemRow = ({
  item,
  isAdmin,
  onEdit,
  onDelete,
}: {
  item: Item;
  isAdmin: boolean;
  onEdit: () => void;
  onDelete: () => void;
}) => (
  <tr>
    <td>
      <div className="item-name">
        {item.name}
        {!item.isActive && (
          <span className="badge badge-muted" style={{ marginLeft: 8 }}>
            Inactive
          </span>
        )}
      </div>
      <div className="item-sub">{item.sku ?? item.description ?? '—'}</div>
    </td>
    <td className="item-sub-cat">
      <span className="cat-pill">{item.categoryName}</span>
    </td>
    <td className="num tnum cost">{peso.format(item.costPrice)}</td>
    <td className="num tnum price">{peso.format(item.sellingPrice)}</td>
    <td className="num">
      <span className="stock-cell">
        <span className="stock-num tnum">{item.stock}</span>
        {item.isLowStock ? (
          <span className="badge badge-low">Low</span>
        ) : (
          <span className="badge badge-ok">OK</span>
        )}
      </span>
    </td>
    {isAdmin && (
      <td className="num">
        <div className="row-actions">
          <button className="btn btn-ghost btn-sm" onClick={onEdit}>
            Edit
          </button>
          <button className="btn btn-quiet btn-sm" onClick={onDelete}>
            Delete
          </button>
        </div>
      </td>
    )}
  </tr>
);

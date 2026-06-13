import { useItems } from '../hooks/useItems';
import type { Item } from '../../../types';

const peso = new Intl.NumberFormat('en-PH', {
  style: 'currency',
  currency: 'PHP',
});

const SKELETON_ROWS = Array.from({ length: 5 });

export const ItemsScreen = () => {
  const { items, loading, error, refetch } = useItems();

  const lowCount = items.filter((i) => i.isLowStock).length;

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
                : `${items.length} item${items.length === 1 ? '' : 's'}` +
                  (lowCount > 0 ? ` · ${lowCount} low on stock` : '')}
          </p>
        </div>
        <div className="page-actions">
          <button
            className="btn btn-ghost"
            onClick={refetch}
            disabled={loading}
          >
            Refresh
          </button>
          <button className="btn btn-primary" disabled title="Coming next">
            New item
          </button>
        </div>
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
          <ItemsTable>
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
          </div>
        ) : (
          <ItemsTable>
            {items.map((item) => (
              <ItemRow key={item.id} item={item} />
            ))}
          </ItemsTable>
        )}
      </div>
    </>
  );
};

const ItemsTable = ({ children }: { children: React.ReactNode }) => (
  <table className="ledger">
    <thead>
      <tr>
        <th>Item</th>
        <th className="item-sub-cat">Category</th>
        <th className="num">Cost</th>
        <th className="num">Price</th>
        <th className="num">Stock</th>
      </tr>
    </thead>
    <tbody>{children}</tbody>
  </table>
);

const ItemRow = ({ item }: { item: Item }) => (
  <tr>
    <td>
      <div className="item-name">{item.name}</div>
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
  </tr>
);

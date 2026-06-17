import { useState } from 'react';
import { useCategories } from '../hooks/useCategories';
import { useAuth } from '../../auth/hooks/useAuth';
import { ItemsTabs } from '../components/ItemsTabs';
import { CategoryFormModal } from '../components/CategoryFormModal';
import { DeleteCategoryModal } from '../components/DeleteCategoryModal';
import type { Category } from '../../../types';

const SKELETON_ROWS = Array.from({ length: 5 });

type ModalState =
  | { kind: 'create' }
  | { kind: 'edit'; category: Category }
  | { kind: 'delete'; category: Category }
  | null;

export const CategoriesScreen = () => {
  const { categories, loading, error, refetch } = useCategories();
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
          <h1 className="page-title">Categories</h1>
          <p className="page-lead">
            {loading
              ? 'Loading categories…'
              : error
                ? 'Could not load categories.'
                : `${categories.length} categor${categories.length === 1 ? 'y' : 'ies'}`}
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
              New category
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
          <CategoriesTable isAdmin={isAdmin}>
            {SKELETON_ROWS.map((_, i) => (
              <tr key={i}>
                <td>
                  <span className="skeleton" style={{ width: '50%' }} />
                </td>
                <td>
                  <span className="skeleton" style={{ width: '70%' }} />
                </td>
                <td className="num">
                  <span className="skeleton" style={{ width: 40, marginLeft: 'auto' }} />
                </td>
                {isAdmin && (
                  <td className="num">
                    <span className="skeleton" style={{ width: 90, marginLeft: 'auto' }} />
                  </td>
                )}
              </tr>
            ))}
          </CategoriesTable>
        ) : categories.length === 0 ? (
          <div className="state">
            <div className="state-emoji">🏷️</div>
            <div className="state-title">No categories yet</div>
            <p className="state-msg">
              Categories help you group products. Create your first one to get
              started.
            </p>
            {isAdmin && (
              <button
                className="btn btn-primary"
                onClick={() => setModal({ kind: 'create' })}
              >
                New category
              </button>
            )}
          </div>
        ) : (
          <CategoriesTable isAdmin={isAdmin}>
            {categories.map((c) => (
              <tr key={c.id}>
                <td>
                  <div className="item-name">{c.name}</div>
                </td>
                <td className="item-sub">{c.description ?? '—'}</td>
                <td className="num tnum">
                  <span className="cat-pill">{c.itemCount}</span>
                </td>
                {isAdmin && (
                  <td className="num">
                    <div className="row-actions">
                      <button
                        className="btn btn-ghost btn-sm"
                        onClick={() => setModal({ kind: 'edit', category: c })}
                      >
                        Edit
                      </button>
                      <button
                        className="btn btn-quiet btn-sm"
                        onClick={() => setModal({ kind: 'delete', category: c })}
                      >
                        Delete
                      </button>
                    </div>
                  </td>
                )}
              </tr>
            ))}
          </CategoriesTable>
        )}
      </div>

      {modal?.kind === 'create' && (
        <CategoryFormModal
          onClose={() => setModal(null)}
          onSaved={closeAndRefresh}
        />
      )}
      {modal?.kind === 'edit' && (
        <CategoryFormModal
          category={modal.category}
          onClose={() => setModal(null)}
          onSaved={closeAndRefresh}
        />
      )}
      {modal?.kind === 'delete' && (
        <DeleteCategoryModal
          category={modal.category}
          onClose={() => setModal(null)}
          onDeleted={closeAndRefresh}
        />
      )}
    </>
  );
};

const CategoriesTable = ({
  isAdmin,
  children,
}: {
  isAdmin: boolean;
  children: React.ReactNode;
}) => (
  <table className="ledger">
    <thead>
      <tr>
        <th>Name</th>
        <th>Description</th>
        <th className="num">Items</th>
        {isAdmin && <th className="num">Actions</th>}
      </tr>
    </thead>
    <tbody>{children}</tbody>
  </table>
);

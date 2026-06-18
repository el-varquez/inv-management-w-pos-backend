import { useMemo, useState } from 'react';
import { useStockLevels } from '../hooks/useStockLevels';
import { useInventoryCount } from '../hooks/useInventoryCount';
import { InventoryTabs } from '../components/InventoryTabs';
import { signed } from '../../../lib/format';

export const InventoryCountScreen = () => {
  const { stockLevels, loading, error, refetch } = useStockLevels();
  const { submitCount, loading: submitting, error: submitError } =
    useInventoryCount();

  const [actuals, setActuals] = useState<Record<string, string>>({});
  const [notes, setNotes] = useState('');
  const [done, setDone] = useState(false);

  const lines = useMemo(
    () =>
      stockLevels.map((s) => {
        const raw = actuals[s.itemId];
        const counted = raw === undefined || raw === '' ? null : Number(raw);
        const actual = counted ?? s.stock;
        return {
          itemId: s.itemId,
          itemName: s.itemName,
          categoryName: s.categoryName,
          expected: s.stock,
          counted,
          actual,
          variance: actual - s.stock,
        };
      }),
    [stockLevels, actuals]
  );

  const countedLines = lines.filter((l) => l.counted !== null);
  const varianceLines = lines.filter((l) => l.variance !== 0);

  const handleSubmit = async () => {
    const payload = lines.map((l) => ({
      itemId: l.itemId,
      actualQty: l.actual,
    }));
    const ok = await submitCount(payload, notes.trim() || undefined);
    if (ok) {
      setDone(true);
      setActuals({});
      setNotes('');
      refetch();
    }
  };

  if (done) {
    return (
      <>
        <div className="page-head">
          <div>
            <p className="eyebrow">Inventory · Stocktake</p>
            <h1 className="page-title">Stocktake</h1>
          </div>
        </div>
        <InventoryTabs />
        <div className="card">
          <div className="state">
            <div className="state-emoji">✅</div>
            <div className="state-title">Count submitted</div>
            <p className="state-msg">
              Stock levels have been adjusted to match your counted quantities.
              Variances were recorded in the inventory history.
            </p>
            <button className="btn btn-primary" onClick={() => setDone(false)}>
              Start another count
            </button>
          </div>
        </div>
      </>
    );
  }

  return (
    <>
      <div className="page-head">
        <div>
          <p className="eyebrow">Inventory · Stocktake</p>
          <h1 className="page-title">Stocktake</h1>
          <p className="page-lead">
            Enter the quantity you physically counted. Blank rows keep their
            system quantity.
          </p>
        </div>
        <div className="page-actions">
          <button className="btn btn-ghost" onClick={refetch} disabled={loading}>
            Refresh
          </button>
        </div>
      </div>

      <InventoryTabs />

      {(error || submitError) && (
        <div className="login-error" role="alert">
          <span aria-hidden="true">⚠</span>
          {submitError ?? error}
        </div>
      )}

      <div className="card table-wrap">
        {loading ? (
          <div className="state">
            <span className="spinner spinner-dark" aria-hidden="true" />
          </div>
        ) : stockLevels.length === 0 ? (
          <div className="state">
            <div className="state-emoji">📋</div>
            <div className="state-title">Nothing to count</div>
            <p className="state-msg">Add items to your catalog first.</p>
          </div>
        ) : (
          <table className="ledger">
            <thead>
              <tr>
                <th>Item</th>
                <th className="item-sub-cat">Category</th>
                <th className="num">System</th>
                <th className="num">Counted</th>
                <th className="num">Variance</th>
              </tr>
            </thead>
            <tbody>
              {lines.map((l) => (
                <tr key={l.itemId}>
                  <td>
                    <div className="item-name">{l.itemName}</div>
                  </td>
                  <td className="item-sub-cat">
                    <span className="cat-pill">{l.categoryName}</span>
                  </td>
                  <td className="num tnum cost">{l.expected}</td>
                  <td className="num">
                    <input
                      className="input input-inline tnum"
                      type="number"
                      min="0"
                      step="1"
                      placeholder={String(l.expected)}
                      value={actuals[l.itemId] ?? ''}
                      onChange={(e) =>
                        setActuals((a) => ({
                          ...a,
                          [l.itemId]: e.target.value,
                        }))
                      }
                    />
                  </td>
                  <td className="num">
                    {l.counted === null || l.variance === 0 ? (
                      <span className="tnum text-muted">
                        {l.counted === null ? '—' : '0'}
                      </span>
                    ) : (
                      <span
                        className={`tnum ${l.variance < 0 ? 'text-red' : 'text-green'}`}
                      >
                        {signed(l.variance)}
                      </span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {stockLevels.length > 0 && !loading && (
        <div className="card count-footer">
          <div className="field count-notes">
            <label htmlFor="count-notes">Notes (optional)</label>
            <input
              id="count-notes"
              className="input"
              type="text"
              placeholder="e.g. End-of-month full count"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
            />
          </div>
          <div className="count-summary">
            <span>
              {countedLines.length} counted · {varianceLines.length} with
              variance
            </span>
            <button
              className="btn btn-primary"
              onClick={handleSubmit}
              disabled={submitting || countedLines.length === 0}
            >
              {submitting ? (
                <span className="spinner" aria-hidden="true" />
              ) : null}
              {submitting ? 'Submitting…' : 'Submit count'}
            </button>
          </div>
        </div>
      )}
    </>
  );
};

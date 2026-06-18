import { useState } from 'react';
import { useSalesReport } from '../hooks/useSalesReport';
import { useDateRange } from '../hooks/useDateRange';
import { ReportsTabs } from '../components/ReportsTabs';
import { DateRangeControls } from '../components/DateRangeControls';
import { Pagination } from '../../../components/Pagination';
import { DEFAULT_PAGE_SIZE } from '../../../lib/pagination';
import { peso, formatDate } from '../../../lib/format';

const SKELETON_ROWS = Array.from({ length: 5 });

export const SalesReportScreen = () => {
  const {
    preset,
    setPreset,
    customFrom,
    setCustomFrom,
    customTo,
    setCustomTo,
    range,
  } = useDateRange('month');
  const { report, loading, error, refetch } = useSalesReport(range);

  const daily = report?.dailyBreakdown ?? [];
  const pageSize = DEFAULT_PAGE_SIZE;
  const [page, setPage] = useState(1);

  const rangeKey = `${range.from ?? ''}|${range.to ?? ''}`;
  const [prevRangeKey, setPrevRangeKey] = useState(rangeKey);
  if (rangeKey !== prevRangeKey) {
    setPrevRangeKey(rangeKey);
    setPage(1);
  }

  const totalPages = Math.max(1, Math.ceil(daily.length / pageSize));
  const currentPage = Math.min(page, totalPages);
  const pageRows = daily.slice(
    (currentPage - 1) * pageSize,
    currentPage * pageSize
  );

  return (
    <>
      <div className="page-head">
        <div>
          <p className="eyebrow">Reports · Sales</p>
          <h1 className="page-title">Sales report</h1>
          <p className="page-lead">
            {loading
              ? 'Crunching the numbers…'
              : error
                ? 'Could not load the sales report.'
                : `${report?.transactionCount ?? 0} sale${
                    report?.transactionCount === 1 ? '' : 's'
                  } in the selected period`}
          </p>
        </div>
        <div className="page-actions">
          <button className="btn btn-ghost" onClick={refetch} disabled={loading}>
            Refresh
          </button>
        </div>
      </div>

      <ReportsTabs />

      <DateRangeControls
        preset={preset}
        setPreset={setPreset}
        customFrom={customFrom}
        setCustomFrom={setCustomFrom}
        customTo={customTo}
        setCustomTo={setCustomTo}
      />

      {error ? (
        <div className="card table-wrap">
          <div className="state state-error">
            <div className="state-emoji">⚠️</div>
            <div className="state-title">Something went wrong</div>
            <p className="state-msg">{error}</p>
            <button className="btn btn-ghost" onClick={refetch}>
              Try again
            </button>
          </div>
        </div>
      ) : (
        <>
          <div className="stat-row">
            <div className="card stat-card">
              <div className="stat-label">Gross sales</div>
              <div className="stat-value tnum">
                {peso.format(report?.grossSales ?? 0)}
              </div>
            </div>
            <div className="card stat-card">
              <div className="stat-label">Discounts</div>
              <div className="stat-value tnum">
                {peso.format(report?.totalDiscounts ?? 0)}
              </div>
            </div>
            <div className="card stat-card">
              <div className="stat-label">Refunds</div>
              <div className="stat-value tnum">
                {peso.format(report?.totalRefunds ?? 0)}
              </div>
            </div>
            <div className="card stat-card">
              <div className="stat-label">Net sales</div>
              <div className="stat-value tnum">
                {peso.format(report?.netSales ?? 0)}
              </div>
            </div>
          </div>

          <div className="card table-wrap">
            {loading ? (
              <DailyTable>
                {SKELETON_ROWS.map((_, i) => (
                  <tr key={i}>
                    <td>
                      <span className="skeleton" style={{ width: 110 }} />
                    </td>
                    <td className="num">
                      <span className="skeleton" style={{ width: 30, marginLeft: 'auto' }} />
                    </td>
                    <td className="num">
                      <span className="skeleton" style={{ width: 80, marginLeft: 'auto' }} />
                    </td>
                    <td className="num">
                      <span className="skeleton" style={{ width: 70, marginLeft: 'auto' }} />
                    </td>
                    <td className="num">
                      <span className="skeleton" style={{ width: 70, marginLeft: 'auto' }} />
                    </td>
                    <td className="num">
                      <span className="skeleton" style={{ width: 80, marginLeft: 'auto' }} />
                    </td>
                  </tr>
                ))}
              </DailyTable>
            ) : daily.length === 0 ? (
              <div className="state">
                <div className="state-emoji">📊</div>
                <div className="state-title">No sales in this period</div>
                <p className="state-msg">
                  Try a wider date range, or ring up a sale on the register.
                </p>
              </div>
            ) : (
              <DailyTable>
                {pageRows.map((d) => (
                  <tr key={d.date}>
                    <td className="item-sub">{formatDate(d.date)}</td>
                    <td className="num tnum">{d.transactionCount}</td>
                    <td className="num tnum">{peso.format(d.grossSales)}</td>
                    <td className="num tnum">{peso.format(d.discounts)}</td>
                    <td className="num tnum cost">
                      {d.refunds > 0 ? `−${peso.format(d.refunds)}` : '—'}
                    </td>
                    <td className="num tnum price" style={{ fontWeight: 600 }}>
                      {peso.format(d.netSales)}
                    </td>
                  </tr>
                ))}
              </DailyTable>
            )}
          </div>

          {!loading && (
            <Pagination
              page={currentPage}
              pageSize={pageSize}
              totalCount={daily.length}
              totalPages={totalPages}
              onPageChange={setPage}
            />
          )}
        </>
      )}
    </>
  );
};

const DailyTable = ({ children }: { children: React.ReactNode }) => (
  <table className="ledger">
    <thead>
      <tr>
        <th>Date</th>
        <th className="num">Sales</th>
        <th className="num">Gross</th>
        <th className="num">Discounts</th>
        <th className="num">Refunds</th>
        <th className="num">Net</th>
      </tr>
    </thead>
    <tbody>{children}</tbody>
  </table>
);

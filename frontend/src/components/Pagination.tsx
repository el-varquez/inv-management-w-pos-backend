interface PaginationProps {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  onPageChange: (page: number) => void;
}

export const Pagination = ({
  page,
  pageSize,
  totalCount,
  totalPages,
  onPageChange,
}: PaginationProps) => {
  if (totalCount === 0) return null;

  const first = (page - 1) * pageSize + 1;
  const last = Math.min(page * pageSize, totalCount);

  return (
    <div className="pagination">
      <span className="pagination-range">
        {first}–{last} of {totalCount}
      </span>
      <div className="pagination-controls">
        <button
          type="button"
          className="btn btn-ghost btn-sm"
          onClick={() => onPageChange(page - 1)}
          disabled={page <= 1}
        >
          ‹ Prev
        </button>
        <span className="pagination-status">
          Page {page} of {totalPages}
        </span>
        <button
          type="button"
          className="btn btn-ghost btn-sm"
          onClick={() => onPageChange(page + 1)}
          disabled={page >= totalPages}
        >
          Next ›
        </button>
      </div>
    </div>
  );
};

import { useEffect, useRef, useState } from 'react';

export interface SearchSelectOption {
  value: string;
  label: string;
}

interface SearchSelectProps {
  value: string;
  onChange: (value: string) => void;
  options: SearchSelectOption[];
  allLabel?: string;
  placeholder?: string;
  id?: string;
  actionLabel?: string;
  onAction?: () => void;
}

export const SearchSelect = ({
  value,
  onChange,
  options,
  allLabel,
  placeholder,
  id,
  actionLabel,
  onAction,
}: SearchSelectProps) => {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState('');
  const ref = useRef<HTMLDivElement>(null);

  const selectedLabel =
    value === ''
      ? (allLabel ?? '')
      : (options.find((o) => o.value === value)?.label ?? allLabel ?? '');

  useEffect(() => {
    if (!open) return;
    const onDocMouseDown = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) {
        setOpen(false);
        setQuery('');
      }
    };
    document.addEventListener('mousedown', onDocMouseDown);
    return () => document.removeEventListener('mousedown', onDocMouseDown);
  }, [open]);

  const q = query.trim().toLowerCase();
  const filtered = q
    ? options.filter((o) => o.label.toLowerCase().includes(q))
    : options;

  const select = (v: string) => {
    onChange(v);
    setOpen(false);
    setQuery('');
  };

  const runAction = () => {
    setOpen(false);
    setQuery('');
    onAction?.();
  };

  return (
    <div className="search-select" ref={ref}>
      <input
        id={id}
        className="input"
        type="text"
        role="combobox"
        aria-expanded={open}
        autoComplete="off"
        value={open ? query : selectedLabel}
        placeholder={open ? selectedLabel || placeholder : placeholder}
        onFocus={() => {
          setOpen(true);
          setQuery('');
        }}
        onClick={() => setOpen(true)}
        onChange={(e) => {
          setQuery(e.target.value);
          setOpen(true);
        }}
        onKeyDown={(e) => {
          if (e.key === 'Escape') {
            setOpen(false);
            setQuery('');
            e.currentTarget.blur();
          } else if (e.key === 'Enter' && open) {
            e.preventDefault();
            if (q && filtered.length > 0) select(filtered[0].value);
          }
        }}
      />
      {open && (
        <ul className="search-select-menu" role="listbox">
          {allLabel !== undefined && (
            <li
              role="option"
              aria-selected={value === ''}
              className={
                value === ''
                  ? 'search-select-option is-active'
                  : 'search-select-option'
              }
              onMouseDown={(e) => {
                e.preventDefault();
                select('');
              }}
            >
              {allLabel}
            </li>
          )}
          {filtered.map((o) => (
            <li
              key={o.value}
              role="option"
              aria-selected={o.value === value}
              className={
                o.value === value
                  ? 'search-select-option is-active'
                  : 'search-select-option'
              }
              onMouseDown={(e) => {
                e.preventDefault();
                select(o.value);
              }}
            >
              {o.label}
            </li>
          ))}
          {filtered.length === 0 && (
            <li className="search-select-empty">No matches</li>
          )}
          {actionLabel && onAction && (
            <li
              className="search-select-option search-select-action"
              onMouseDown={(e) => {
                e.preventDefault();
                runAction();
              }}
            >
              {actionLabel}
            </li>
          )}
        </ul>
      )}
    </div>
  );
};

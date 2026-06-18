import type { RangePreset } from '../hooks/useDateRange';

interface DateRangeControlsProps {
  preset: RangePreset;
  setPreset: (p: RangePreset) => void;
  customFrom: string;
  setCustomFrom: (v: string) => void;
  customTo: string;
  setCustomTo: (v: string) => void;
  leading?: React.ReactNode;
}

const PRESETS: { value: RangePreset; label: string }[] = [
  { value: 'today', label: 'Today' },
  { value: 'week', label: 'Last 7 days' },
  { value: 'month', label: 'This month' },
  { value: 'custom', label: 'Custom' },
];

export const DateRangeControls = ({
  preset,
  setPreset,
  customFrom,
  setCustomFrom,
  customTo,
  setCustomTo,
  leading,
}: DateRangeControlsProps) => (
  <div className="filter-bar card">
    {leading}
    <div className="field">
      <label>Period</label>
      <div className="segmented">
        {PRESETS.map((p) => (
          <button
            key={p.value}
            type="button"
            className={p.value === preset ? 'seg is-active' : 'seg'}
            onClick={() => setPreset(p.value)}
          >
            {p.label}
          </button>
        ))}
      </div>
    </div>

    {preset === 'custom' && (
      <>
        <div className="field">
          <label htmlFor="report-from">From</label>
          <input
            id="report-from"
            className="input"
            type="date"
            value={customFrom}
            onChange={(e) => setCustomFrom(e.target.value)}
          />
        </div>
        <div className="field">
          <label htmlFor="report-to">To</label>
          <input
            id="report-to"
            className="input"
            type="date"
            value={customTo}
            onChange={(e) => setCustomTo(e.target.value)}
          />
        </div>
      </>
    )}
  </div>
);

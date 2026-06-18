import { useMemo, useState } from 'react';

export type RangePreset = 'today' | 'week' | 'month' | 'custom';

export interface DateRangeValue {
  from?: string;
  to?: string;
}

export const useDateRange = (initial: RangePreset = 'month') => {
  const [preset, setPreset] = useState<RangePreset>(initial);
  const [customFrom, setCustomFrom] = useState('');
  const [customTo, setCustomTo] = useState('');

  const range = useMemo<DateRangeValue>(() => {
    const now = new Date();
    const toIso = (d: Date) => d.toISOString();

    switch (preset) {
      case 'today': {
        const start = new Date(now.getFullYear(), now.getMonth(), now.getDate());
        return { from: toIso(start), to: toIso(now) };
      }
      case 'week': {
        const start = new Date(now);
        start.setDate(now.getDate() - 7);
        return { from: toIso(start), to: toIso(now) };
      }
      case 'month': {
        const start = new Date(now.getFullYear(), now.getMonth(), 1);
        return { from: toIso(start), to: toIso(now) };
      }
      case 'custom':
        return {
          from: customFrom ? new Date(customFrom).toISOString() : undefined,
          to: customTo ? new Date(customTo).toISOString() : undefined,
        };
    }
  }, [preset, customFrom, customTo]);

  return {
    preset,
    setPreset,
    customFrom,
    setCustomFrom,
    customTo,
    setCustomTo,
    range,
  };
};

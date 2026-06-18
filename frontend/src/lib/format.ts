export const peso = new Intl.NumberFormat('en-PH', {
  style: 'currency',
  currency: 'PHP',
});

const dateTimeFmt = new Intl.DateTimeFormat('en-PH', {
  year: 'numeric',
  month: 'short',
  day: 'numeric',
  hour: 'numeric',
  minute: '2-digit',
});

const dateFmt = new Intl.DateTimeFormat('en-PH', {
  year: 'numeric',
  month: 'short',
  day: 'numeric',
});

export const formatDateTime = (iso: string): string =>
  dateTimeFmt.format(new Date(iso));

export const formatDate = (iso: string): string =>
  dateFmt.format(new Date(iso));

export const signed = (n: number): string =>
  n > 0 ? `+${n}` : `${n}`;

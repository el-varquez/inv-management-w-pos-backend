import axios from 'axios';

/**
 * Pulls a human-readable message out of an unknown caught error. The backend
 * returns errors as `{ error: string }`; anything else falls back to `fallback`.
 */
export const getApiErrorMessage = (err: unknown, fallback: string): string => {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data as { error?: string } | undefined;
    return data?.error ?? err.message ?? fallback;
  }
  return fallback;
};

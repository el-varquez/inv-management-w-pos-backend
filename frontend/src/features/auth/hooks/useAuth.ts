import { useState } from 'react';
import { authService } from '../services/authService';
import { useAuthStore } from '../../../store/authStore';
import { getApiErrorMessage } from '../../../services/apiError';
import type { RegisterPayload } from '../../../types';

export const useAuth = () => {
  const { setAuth, logout, user, token } = useAuthStore();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const login = async (email: string, password: string) => {
    setLoading(true);
    setError(null);
    try {
      const result = await authService.login(email, password);
      if (result.role === 'Cashier') {
        setError('Cashiers sign in on the mobile app.');
        return;
      }
      setAuth(result.token, {
        name: result.name,
        email: result.email,
        role: result.role,
      });
    } catch (err) {
      setError(getApiErrorMessage(err, 'Login failed.'));
    } finally {
      setLoading(false);
    }
  };

  const register = async (payload: RegisterPayload) => {
    setLoading(true);
    setError(null);
    try {
      const result = await authService.register(payload);
      setAuth(result.token, {
        name: result.name,
        email: result.email,
        role: result.role,
      });
    } catch (err) {
      setError(getApiErrorMessage(err, 'Registration failed.'));
    } finally {
      setLoading(false);
    }
  };

  return { login, register, logout, user, token, loading, error };
};

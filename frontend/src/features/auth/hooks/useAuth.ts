import { useState } from 'react';
import { authService } from '../services/authService';
import { useAuthStore } from '../../../store/authStore';
import { getApiErrorMessage } from '../../../services/apiError';

export const useAuth = () => {
  const { setAuth, logout, user, token } = useAuthStore();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const login = async (email: string, password: string) => {
    setLoading(true);
    setError(null);
    try {
      const result = await authService.login(email, password);
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

  return { login, logout, user, token, loading, error };
};
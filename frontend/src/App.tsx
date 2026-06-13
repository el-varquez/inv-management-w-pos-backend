import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { ProtectedRoute } from './components/ProtectedRoute';
import { Layout } from './components/Layout';
import { LoginScreen } from './features/auth/screens/LoginScreen';
import { ItemsScreen } from './features/items/screens/ItemsScreen';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginScreen />} />
        <Route element={<ProtectedRoute />}>
          <Route element={<Layout />}>
            <Route path="/" element={<Navigate to="/items" replace />} />
            <Route path="/items" element={<ItemsScreen />} />
          </Route>
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
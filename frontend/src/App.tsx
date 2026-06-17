import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { ProtectedRoute } from './components/ProtectedRoute';
import { Layout } from './components/Layout';
import { LoginScreen } from './features/auth/screens/LoginScreen';
import { ItemsScreen } from './features/items/screens/ItemsScreen';
import { StockLevelsScreen } from './features/inventory/screens/StockLevelsScreen';
import { LowStockScreen } from './features/inventory/screens/LowStockScreen';
import { InventoryCountScreen } from './features/inventory/screens/InventoryCountScreen';
import { InventoryHistoryScreen } from './features/inventory/screens/InventoryHistoryScreen';
import { InventoryValuationScreen } from './features/inventory/screens/InventoryValuationScreen';
import { POSScreen } from './features/sales/screens/POSScreen';
import { SalesHistoryScreen } from './features/sales/screens/SalesHistoryScreen';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginScreen />} />
        <Route element={<ProtectedRoute />}>
          <Route element={<Layout />}>
            <Route path="/" element={<Navigate to="/items" replace />} />
            <Route path="/items" element={<ItemsScreen />} />
            <Route
              path="/inventory"
              element={<Navigate to="/inventory/stock-levels" replace />}
            />
            <Route
              path="/inventory/stock-levels"
              element={<StockLevelsScreen />}
            />
            <Route path="/inventory/low-stock" element={<LowStockScreen />} />
            <Route path="/inventory/count" element={<InventoryCountScreen />} />
            <Route
              path="/inventory/history"
              element={<InventoryHistoryScreen />}
            />
            <Route
              path="/inventory/valuation"
              element={<InventoryValuationScreen />}
            />
            <Route
              path="/sales"
              element={<Navigate to="/sales/pos" replace />}
            />
            <Route path="/sales/pos" element={<POSScreen />} />
            <Route path="/sales/history" element={<SalesHistoryScreen />} />
          </Route>
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

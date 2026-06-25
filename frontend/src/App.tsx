import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { ProtectedRoute } from './components/ProtectedRoute';
import { RoleRoute } from './components/RoleRoute';
import { Layout } from './components/Layout';
import { LoginScreen } from './features/auth/screens/LoginScreen';
import { RegisterScreen } from './features/auth/screens/RegisterScreen';
import { PlatformScreen } from './features/platform/screens/PlatformScreen';
import { ItemsScreen } from './features/items/screens/ItemsScreen';
import { CategoriesScreen } from './features/items/screens/CategoriesScreen';
import { StockLevelsScreen } from './features/inventory/screens/StockLevelsScreen';
import { LowStockScreen } from './features/inventory/screens/LowStockScreen';
import { InventoryCountScreen } from './features/inventory/screens/InventoryCountScreen';
import { InventoryHistoryScreen } from './features/inventory/screens/InventoryHistoryScreen';
import { InventoryValuationScreen } from './features/inventory/screens/InventoryValuationScreen';
import { CashiersScreen } from './features/cashiers/screens/CashiersScreen';
import { POSScreen } from './features/sales/screens/POSScreen';
import { SalesHistoryScreen } from './features/sales/screens/SalesHistoryScreen';
import { SalesReportScreen } from './features/reports/screens/SalesReportScreen';
import { ExpenseReportScreen } from './features/reports/screens/ExpenseReportScreen';
import { ProfitReportScreen } from './features/reports/screens/ProfitReportScreen';
import { BestSellersScreen } from './features/reports/screens/BestSellersScreen';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginScreen />} />
        <Route path="/register" element={<RegisterScreen />} />
        <Route element={<ProtectedRoute />}>
          <Route element={<Layout />}>
            <Route
              element={
                <RoleRoute allow={(r) => r === 'SuperAdmin'} redirectTo="/items" />
              }
            >
              <Route path="/platform" element={<PlatformScreen />} />
            </Route>

            <Route
              element={
                <RoleRoute allow={(r) => r !== 'SuperAdmin'} redirectTo="/platform" />
              }
            >
            <Route path="/" element={<Navigate to="/items" replace />} />
            <Route path="/items" element={<ItemsScreen />} />
            <Route path="/items/categories" element={<CategoriesScreen />} />
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
            <Route
              path="/reports"
              element={<Navigate to="/reports/sales" replace />}
            />
            <Route path="/reports/sales" element={<SalesReportScreen />} />
            <Route path="/reports/expenses" element={<ExpenseReportScreen />} />
            <Route path="/reports/profit" element={<ProfitReportScreen />} />
            <Route
              path="/reports/best-sellers"
              element={<BestSellersScreen />}
            />
            <Route path="/cashiers" element={<CashiersScreen />} />
            </Route>
          </Route>
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

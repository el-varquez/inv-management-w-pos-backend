export interface Item {
  id: string;
  name: string;
  description?: string;
  sku?: string;
  costPrice: number;
  sellingPrice: number;
  stock: number;
  lowStockThreshold: number;
  isLowStock: boolean;
  isActive: boolean;
  isComposite: boolean;
  categoryId: string;
  categoryName: string;
  createdAt: string;
}

export interface ItemComponent {
  componentItemId: string;
  componentItemName: string;
  quantity: number;
  componentCostPrice: number;
  lineCost: number;
}

export interface ItemComponents {
  parentItemId: string;
  parentItemName: string;
  isComposite: boolean;
  totalComponentCost: number;
  components: ItemComponent[];
}

export interface Category {
  id: string;
  name: string;
  description?: string;
  itemCount: number;
}

export interface LoginResult {
  token: string;
  name: string;
  email: string;
  role: string;
}

export interface RegisterPayload {
  businessName: string;
  adminName: string;
  email: string;
  password: string;
}

export interface Cashier {
  id: string;
  name: string;
  email: string;
  isActive: boolean;
  createdAt: string;
}

export interface CashierList {
  cashiers: Cashier[];
  activeCount: number;
  cashierCap: number;
}

export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface StockLevel {
  itemId: string;
  itemName: string;
  categoryName: string;
  stock: number;
  lowStockThreshold: number;
  isLowStock: boolean;
  costPrice: number;
  sellingPrice: number;
  stockValue: number;
}

export interface InventoryHistoryItem {
  id: string;
  itemId: string;
  itemName: string;
  categoryName: string;
  movementType: string;
  quantity: number;
  costPerUnit?: number;
  totalCost?: number;
  supplierName?: string;
  reason?: string;
  notes?: string;
  createdAt: string;
}

export interface InventoryValuationItem {
  itemId: string;
  itemName: string;
  categoryName: string;
  stock: number;
  costPrice: number;
  stockValue: number;
}

export interface InventoryValuation {
  items: InventoryValuationItem[];
  totalValue: number;
  totalItems: number;
  generatedAt: string;
}

export interface LowStockItem {
  itemId: string;
  itemName: string;
  categoryName: string;
  stock: number;
  lowStockThreshold: number;
  deficit: number;
}

export interface InventoryCountLine {
  itemId: string;
  itemName: string;
  categoryName: string;
  expectedQty: number;
  actualQty: number;
  variance: number;
}

export interface InventoryCount {
  id: string;
  reference: string;
  notes?: string;
  status: 'Draft' | 'Completed';
  completedAt?: string;
  createdAt: string;
  lines: InventoryCountLine[];
}

export type AdjustmentReason =
  | 'Damage'
  | 'Loss'
  | 'Spoilage'
  | 'Correction'
  | 'Other';

export type StockMovementType =
  | 'AddStock'
  | 'Sale'
  | 'Adjustment'
  | 'InventoryCount'
  | 'Return';

export type PaymentType = 'Cash' | 'GCash' | 'Maya';

export interface CartItem {
  itemId: string;
  name: string;
  unitPrice: number;
  quantity: number;
  discount: number;
  stock: number;
}

export interface CreateTransactionPayload {
  items: { itemId: string; quantity: number; discount: number }[];
  transactionDiscount: number;
  paymentType: PaymentType;
  amountTendered: number;
}

export interface TransactionResult {
  transactionId: string;
  receiptNumber: string;
  subtotal: number;
  discountAmount: number;
  total: number;
  amountTendered: number;
  change: number;
}

export interface Transaction {
  id: string;
  receiptNumber: string;
  subtotal: number;
  discountAmount: number;
  total: number;
  paymentType: string;
  amountTendered: number;
  change: number;
  isRefunded: boolean;
  refundedFromId?: string | null;
  itemCount: number;
  createdAt: string;
}

export interface TransactionLine {
  itemName: string;
  unitPrice: number;
  quantity: number;
  discount: number;
  total: number;
}

export interface TransactionDetail {
  id: string;
  receiptNumber: string;
  subtotal: number;
  discountAmount: number;
  total: number;
  paymentType: string;
  amountTendered: number;
  change: number;
  isRefunded: boolean;
  lines: TransactionLine[];
  createdAt: string;
}

export interface SalesSummary {
  grossSales: number;
  totalDiscounts: number;
  refunds: number;
  netSales: number;
  transactionCount: number;
  from?: string | null;
  to?: string | null;
}

export interface SalesReportDaily {
  date: string;
  grossSales: number;
  discounts: number;
  refunds: number;
  netSales: number;
  transactionCount: number;
}

export interface SalesReport {
  grossSales: number;
  totalDiscounts: number;
  totalRefunds: number;
  netSales: number;
  transactionCount: number;
  dailyBreakdown: SalesReportDaily[];
  from?: string | null;
  to?: string | null;
}

export interface ExpensePurchase {
  date: string;
  itemName: string;
  quantity: number;
  costPerUnit: number;
  totalCost: number;
  supplierName?: string;
}

export interface ExpenseReport {
  costOfPurchases: number;
  inventoryLoss: number;
  totalExpenses: number;
  breakdown: { category: string; amount: number }[];
  purchases: ExpensePurchase[];
  from?: string | null;
  to?: string | null;
}

export interface ProfitDetail {
  itemId: string;
  itemName: string;
  categoryName: string;
  quantitySold: number;
  revenue: number;
  cost: number;
  profit: number;
  marginPercent: number;
}

export interface ProfitReport {
  netSales: number;
  costOfGoodsSold: number;
  grossProfit: number;
  inventoryLoss: number;
  netProfit: number;
  grossMarginPercent: number;
  details: ProfitDetail[];
  from?: string | null;
  to?: string | null;
}

export interface BestSeller {
  itemId: string;
  itemName: string;
  quantitySold: number;
  revenue: number;
  profit: number;
  marginPercent: number;
}
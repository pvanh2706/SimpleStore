export interface Session {
  isAuthenticated: boolean
  email: string | null
  storeId: string | null
  roles: string[]
  hasStore: boolean
}

export interface StoreInfo {
  id: string
  name: string
  mainWarehouseId: string
  mainWarehouseName: string
  timeZoneId: string
}

export interface Product {
  id: string
  sku: string
  barcode: string | null
  name: string
  unit: string
  salePrice: number
  referencePurchaseCost: number | null
  isActive: boolean
  quantityOnHand: number
  inventoryValue: number
  averageCost: number
  hasAverageCost: boolean
  referencePurchaseCostRevision: number
  createdAt: string
  updatedAt: string
}

export interface ProductListItem {
  id: string
  sku: string
  barcode: string | null
  name: string
  unit: string
  salePrice: number
  isActive: boolean
  quantityOnHand: number
}

export interface ProductPage {
  items: ProductListItem[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface ProductInput {
  sku?: string | null
  barcode?: string | null
  name: string
  unit: string
  salePrice: number
  referencePurchaseCost?: number | null
  openingQuantity?: number
  openingCost?: number | null
}

export interface InventoryMovement {
  id: string
  type: string
  quantityDelta: number
  inventoryValueDelta: number
  unitCost: number
  sourceType: string
  sourceId: string
  occurredAt: string
}

export interface ImportError {
  rowNumber: number | null
  field: string
  code: string
  message: string
}

export interface ImportPreviewRow {
  rowNumber: number
  sku: string
  barcode: string | null
  name: string
  unit: string
  salePrice: number
  openingCost: number | null
  openingQuantity: number
}

export interface ImportValidationResult {
  importId: string | null
  isValid: boolean
  rows: ImportPreviewRow[]
  errors: ImportError[]
}

export interface ImportConfirmResult {
  importId: string
  importedProductCount: number
  wasAlreadyCompleted: boolean
}

export interface Supplier {
  id: string
  name: string
  phone: string | null
  note: string | null
  isActive: boolean
  outstandingAmount: number
  createdAt: string
  updatedAt: string
}

export interface SupplierPage {
  items: Supplier[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface PurchaseLine {
  id: string
  productId: string
  productName: string
  productUnit: string
  quantity: number
  unitPrice: number
  lineAmount: number
}

export interface PurchasePayment {
  id: string
  amount: number
  method: 'Cash' | 'Transfer'
  paidAt: string
}

export interface Purchase {
  id: string
  supplierId: string
  supplierName: string
  status: 'Draft' | 'Completed'
  lines: PurchaseLine[]
  payments: PurchasePayment[]
  totalAmount: number
  paidAmount: number
  outstandingAmount: number
  createdAt: string
  updatedAt: string
  completedAt: string | null
  wasAlreadyCompleted: boolean
  isVoided: boolean
  void: PurchaseVoidInfo | null
}

export interface PurchaseVoidInfo {
  id: string
  reason: string
  voidedByUserId: string
  voidedAt: string
}

export interface PurchaseListItem {
  id: string
  supplierId: string
  supplierName: string
  status: 'Draft' | 'Completed'
  totalAmount: number
  paidAmount: number
  outstandingAmount: number
  createdAt: string
  completedAt: string | null
  isVoided: boolean
}

export interface PurchasePage {
  items: PurchaseListItem[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface PurchaseWriteInput {
  supplierId: string
  lines: Array<{ productId: string; quantity: number; unitPrice: number }>
}

export interface OperationStatus {
  operationId: string
  status: 'Processing' | 'Completed'
  operationType: string
  resultReference: string | null
}

export interface Customer {
  id: string
  name: string
  phone: string | null
  createdAt: string
  updatedAt: string
}

export interface CustomerPage {
  items: Customer[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface SaleLine {
  id: string
  productId: string
  productName: string
  productSku: string
  productUnit: string
  quantity: number
  unitSalePrice: number
  lineAmount: number
  unitCostAtSale: number
  costReliability: 'Reliable' | 'Estimated' | 'Unavailable'
}

export interface SalePayment {
  id: string
  amount: number
  method: 'Cash' | 'Transfer'
  occurredAt: string
}

export interface Sale {
  id: string
  status: 'Completed'
  storeName: string
  warehouseId: string
  customer: Customer | null
  cashierDisplayName: string
  lines: SaleLine[]
  payments: SalePayment[]
  totalAmount: number
  paidAmount: number
  outstandingAmount: number
  createdAt: string
  completedAt: string
  wasAlreadyCompleted: boolean
  originalTotalAmount: number
  totalReturnedAmount: number
  netSaleAmount: number
  originalCollectedAmount: number
  totalRefundedAmount: number
  netCollectedAmount: number
  isVoided: boolean
  void: SaleVoidInfo | null
  returns: SaleReturnSummary[]
}

export interface SaleVoidInfo {
  id: string
  reason: string
  voidedByUserId: string
  voidedAt: string
}

export interface SaleReturnSummary {
  id: string
  totalReturnAmount: number
  refundAmount: number
  completedByUserId: string
  completedAt: string
}

export interface SaleListItem {
  id: string
  status: 'Completed'
  customerName: string | null
  cashierDisplayName: string
  totalAmount: number
  paidAmount: number
  outstandingAmount: number
  completedAt: string
  originalTotalAmount: number
  totalReturnedAmount: number
  netSaleAmount: number
  originalCollectedAmount: number
  totalRefundedAmount: number
  netCollectedAmount: number
  isVoided: boolean
}

export interface SalePage {
  items: SaleListItem[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface StoreOperationalSettings {
  allowNegativeStock: boolean
}

export interface DebtBalance {
  partyId: string
  partyName: string
  phone: string | null
  outstandingAmount: number
  asOf: string
}

export interface DebtBalancePage {
  items: DebtBalance[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  asOf: string
}

export interface DebtPaymentResult {
  id: string
  partyId: string
  direction: 'MoneyIn' | 'MoneyOut'
  purpose: 'CustomerDebtCollection' | 'SupplierDebtSettlement'
  amount: number
  method: 'Cash' | 'Transfer'
  note: string | null
  occurredAt: string
  performedByUserId: string
  outstandingBefore: number
  outstandingAfter: number
  wasAlreadyRecorded: boolean
}

export interface EndOfDayReport {
  businessDate: string
  timeZoneId: string
  startUtc: string
  endUtc: string
  salesRevenue: number
  collected: {
    salePayments: number
    customerDebtPayments: number
    customerRefunds: number
    netAmount: number
  }
  customerOutstandingDebtAtEnd: number
  supplierPayments: {
    purchasePayments: number
    supplierDebtPayments: number
    totalAmount: number
  }
  supplierOutstandingDebtAtEnd: number
  estimatedGrossProfit: {
    netSalesRevenue: number
    historicalCogs: number
    amount: number
    costReliability: 'Reliable' | 'Estimated' | 'Unavailable'
  }
}

export type CostReliability = 'Reliable' | 'Estimated' | 'Unavailable'

export type TodayMetricId =
  | 'revenue'
  | 'collected'
  | 'estimated-gross-profit'
  | 'sale-count'
  | 'customer-debt-created'
  | 'supplier-debt-created'

export type TodaySourceType =
  | 'Sale'
  | 'CustomerReturn'
  | 'SaleVoid'
  | 'SalePayment'
  | 'CustomerDebtPayment'
  | 'ActualCustomerRefund'
  | 'Purchase'
  | 'PurchasePayment'
  | 'HistoricalCogs'

export type TodaySourceNavigationType = 'Sale' | 'Return' | 'Purchase'

export interface TodaySourceNavigation {
  type: TodaySourceNavigationType
  id: string
}

export interface TodayEstimatedGrossProfit {
  netSalesRevenue: number
  historicalCogs: number
  amount: number
  costReliability: CostReliability
}

export interface TodaySummary {
  businessDate: string
  timeZoneId: string
  startUtc: string
  endUtc: string
  salesRevenue: number
  netCollected: number
  estimatedGrossProfit: TodayEstimatedGrossProfit
  saleCount: number
  customerDebtCreated: number
  supplierDebtCreated: number
}

export interface TodayDebtContribution {
  originalTotal: number
  directPayments: number
  baseDebt: number
  sameDayReturnObligationReduction: number
  sameDayVoided: boolean
  finalContribution: number
}

export interface TodayEvidenceSource {
  sourceType: TodaySourceType
  sourceId: string
  relatedSourceId: string | null
  occurredAt: string
  contributionAmount: number | null
  contributionCount: number | null
  title: string
  debtContribution: TodayDebtContribution | null
  navigation: TodaySourceNavigation | null
}

export interface TodayExplanation {
  metric: TodayMetricId
  headline: number
  historicalCogs: number | null
  costReliability: CostReliability | null
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  items: TodayEvidenceSource[]
}

export type C14AttentionKind = 'NegativeStock' | 'OutOfStock' | 'LowStockRisk'
export type C14HistoryCoverage = 'FullSevenCompletedDays' | 'PartialObservation'
export type C14RecentSalesEvidence = 'PositiveNetSold' | 'NoPositiveNetSold'
export type C14RiskEvaluation = 'Eligible' | 'InsufficientFullHistory' | 'NoPositiveSalesEvidence'
export type C14ExperimentEventType = 'TodayOpened' | 'SignalShown' | 'WhyOpened' | 'PurchaseDraftStarted'

export interface C14BusinessDate {
  businessDate: string
  startUtc: string
  endUtc: string
}

export interface C14AttentionItem {
  productId: string
  productName: string
  sku: string
  unit: string
  attentionKind: C14AttentionKind
  currentStock: number
  netSoldQuantity: number
  averageDailySales: number | null
  daysOfCover: number | null
  historyCoverage: C14HistoryCoverage
  recentSalesEvidence: C14RecentSalesEvidence
  riskEvaluation: C14RiskEvaluation
}

export interface C14AttentionList {
  businessDate: string
  timeZoneId: string
  velocityStartUtc: string
  velocityEndUtc: string
  completedBusinessDays: C14BusinessDate[]
  evaluationCoverage: C14HistoryCoverage
  totalAttentionCount: number
  page: number
  pageSize: number
  totalPages: number
  items: C14AttentionItem[]
}

export interface C14SourceEvidence {
  sourceType: 'Sale' | 'Return' | 'SaleVoid'
  sourceId: string
  relatedAggregateId: string | null
  occurredAt: string
  businessDate: string
  productId: string
  quantityContribution: number
  navigation: TodaySourceNavigation
}

export interface C14AttentionDetail extends C14AttentionItem {
  businessDate: string
  timeZoneId: string
  velocityStartUtc: string
  velocityEndUtc: string
  completedBusinessDays: C14BusinessDate[]
  formulaInputs: {
    saleQuantity: number
    returnQuantity: number
    saleVoidQuantity: number
    denominator: number
  }
  evidence: C14SourceEvidence[]
}

export interface C14ExperimentEventInput {
  eventId: string
  eventType: C14ExperimentEventType
  productId: string | null
  attentionKind: C14AttentionKind | null
}

export interface ReturnLine {
  id: string
  originalSaleLineId: string
  productId: string
  quantity: number
  restock: boolean
  unitSalePriceBasis: number
  returnLineAmount: number
  unitCostBasis: number
  restockedInventoryValue: number
}

export interface ReturnRefundPayment {
  id: string
  amount: number
  method: 'Cash' | 'Transfer'
  occurredAt: string
}

export interface ReturnResult {
  id: string
  originalSaleId: string
  status: 'Completed'
  lines: ReturnLine[]
  refundPayments: ReturnRefundPayment[]
  totalReturnAmount: number
  refundAmount: number
  completedByUserId: string
  createdAt: string
  completedAt: string
  wasAlreadyCompleted: boolean
}

export interface ReturnContextLine {
  saleLineId: string
  productId: string
  productName: string
  productSku: string
  productUnit: string
  soldQuantity: number
  previouslyReturnedQuantity: number
  returnableQuantity: number
  originalUnitSalePrice: number
  originalLineAmount: number
}

export interface ReturnContext {
  saleId: string
  isVoided: boolean
  hasReturns: boolean
  originalTotalAmount: number
  totalReturnedAmount: number
  netSaleAmount: number
  originalCollectedAmount: number
  totalRefundedAmount: number
  netCollectedAmount: number
  outstandingAmount: number
  lines: ReturnContextLine[]
}

export interface ReturnPreviewLine {
  originalSaleLineId: string
  productId: string
  requestedQuantity: number
  restock: boolean
  previouslyReturnedQuantity: number
  remainingQuantityBefore: number
  returnLineAmount: number
  restockedInventoryValue: number
}

export interface ReturnPreview {
  originalSaleId: string
  lines: ReturnPreviewLine[]
  currentReturnValue: number
  previousReturnedValue: number
  cumulativeReturnedValue: number
  netSaleObligation: number
  netCashHeld: number
  outstanding: number
  refundDueNow: number
  refundMethodRequired: boolean
  returnObligationReduction: number
  currentAggregateCustomerDebt: number | null
  debtReduction: number | null
  requiredActualRefund: number
}

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

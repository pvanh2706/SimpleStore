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

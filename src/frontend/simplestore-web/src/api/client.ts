export interface ProblemDetails {
  title?: string
  status?: number
  code?: string
  errors?: Array<{ message: string }>
}

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly problem: ProblemDetails,
  ) {
    super(problem.title ?? `Yêu cầu thất bại (${status}).`)
  }
}

let antiforgeryToken: string | null = null
let unauthorizedHandler: (() => void) | null = null

export function setUnauthorizedHandler(handler: () => void) {
  unauthorizedHandler = handler
}

export function resetAntiforgeryToken() {
  antiforgeryToken = null
}

async function getAntiforgeryToken(): Promise<string> {
  if (antiforgeryToken) return antiforgeryToken
  const response = await fetch('/api/security/antiforgery', { credentials: 'include' })
  if (!response.ok) throw await toApiError(response)
  const body = (await response.json()) as { requestToken: string }
  antiforgeryToken = body.requestToken
  return antiforgeryToken
}

async function toApiError(response: Response): Promise<ApiError> {
  let problem: ProblemDetails = {}
  try {
    problem = (await response.json()) as ProblemDetails
  } catch {
    problem.title = response.statusText
  }
  return new ApiError(response.status, problem)
}

export async function apiRequest<T>(path: string, init: RequestInit = {}): Promise<T> {
  const method = (init.method ?? 'GET').toUpperCase()
  const headers = new Headers(init.headers)
  if (init.body && !(init.body instanceof FormData)) headers.set('Content-Type', 'application/json')
  if (!['GET', 'HEAD', 'OPTIONS'].includes(method)) {
    headers.set('X-CSRF-TOKEN', await getAntiforgeryToken())
  }

  const response = await fetch(path, { ...init, method, headers, credentials: 'include' })
  if (!response.ok) {
    if (response.status === 401 && path !== '/api/auth/login') unauthorizedHandler?.()
    throw await toApiError(response)
  }
  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}

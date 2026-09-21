import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError, apiRequest, resetAntiforgeryToken, setUnauthorizedHandler } from './client'

afterEach(() => { vi.unstubAllGlobals(); resetAntiforgeryToken() })

describe('api client', () => {
  it('bootstraps antiforgery and sends the token on mutations', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(JSON.stringify({ requestToken: 'csrf-token' }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ id: '1' }), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    await apiRequest('/api/test', { method: 'POST', body: '{}' })

    expect(fetchMock).toHaveBeenCalledTimes(2)
    const mutation = fetchMock.mock.calls[1]?.[1] as RequestInit
    expect(new Headers(mutation.headers).get('X-CSRF-TOKEN')).toBe('csrf-token')
    expect(mutation.credentials).toBe('include')
  })

  it('surfaces ProblemDetails errors', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ title: 'Trùng SKU', code: 'duplicate-sku' }), { status: 409 })))
    await expect(apiRequest('/api/products')).rejects.toEqual(
      expect.objectContaining({ status: 409, message: 'Trùng SKU' }) as Partial<ApiError>,
    )
  })

  it('notifies the shell when an authenticated request expires', async () => {
    const onUnauthorized = vi.fn()
    setUnauthorizedHandler(onUnauthorized)
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 401 })))

    await expect(apiRequest('/api/products')).rejects.toBeInstanceOf(ApiError)
    expect(onUnauthorized).toHaveBeenCalledOnce()
  })
})

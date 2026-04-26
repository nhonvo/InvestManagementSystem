import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { fetchApi } from './api';

describe('fetchApi Refresh Logic', () => {
  const API_URL = "http://localhost:8080";

  beforeEach(() => {
    let storage: Record<string, string> = {};
    vi.stubGlobal('fetch', vi.fn());
    vi.stubGlobal('localStorage', {
      getItem: vi.fn((key) => storage[key] || null),
      setItem: vi.fn((key, value) => { storage[key] = value; }),
      removeItem: vi.fn((key) => { delete storage[key]; }),
    });
    vi.stubGlobal('window', {
      location: { href: '' },
    });
    vi.stubGlobal('process', {
      env: { NEXT_PUBLIC_API_URL: API_URL, NODE_ENV: 'development' }
    });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('should refresh token on 401 and retry', async () => {
    const oldToken = 'old-token';
    const newToken = 'new-token';
    localStorage.setItem('auth_token', oldToken);

    // First call: 401
    // Second call (refresh): 200 with new token
    // Third call (retry): 200
    vi.mocked(fetch)
      .mockResolvedValueOnce({
        status: 401,
        ok: false,
        json: async () => ({ message: 'Unauthorized' }),
      } as Response)
      .mockResolvedValueOnce({
        status: 200,
        ok: true,
        json: async () => ({ auth: { accessToken: newToken } }),
      } as Response)
      .mockResolvedValueOnce({
        status: 200,
        ok: true,
        json: async () => ({ data: 'success' }),
      } as Response);

    const result = await fetchApi('/test');

    expect(result).toEqual({ data: 'success' });
    expect(localStorage.setItem).toHaveBeenCalledWith('auth_token', newToken);
    expect(fetch).toHaveBeenCalledTimes(3);
    
    // Verify refresh was called
    expect(fetch).toHaveBeenNthCalledWith(2, `${API_URL}/api/v1/auth/refresh`, expect.anything());
    // Verify retry used new token
    expect(fetch).toHaveBeenNthCalledWith(3, `${API_URL}/test`, expect.objectContaining({
      headers: expect.objectContaining({
        Authorization: `Bearer ${newToken}`
      })
    }));
  });

  it('should handle concurrent 401s by waiting for a single refresh', async () => {
    const oldToken = 'old-token';
    const newToken = 'new-token';
    localStorage.setItem('auth_token', oldToken);

    // We need to be careful with the mock below because fetchApi calls itself recursively.
    let refreshCalledCount = 0;
    vi.mocked(fetch).mockImplementation(async (url, options) => {
        const urlStr = url.toString();
        if (urlStr.includes('/api/v1/auth/refresh')) {
            refreshCalledCount++;
            await new Promise(resolve => setTimeout(resolve, 50));
            return {
                status: 200,
                ok: true,
                json: async () => ({ auth: { accessToken: newToken } }),
            } as Response;
        }

        if (urlStr.includes('/test')) {
            const authHeader = (options?.headers as any)?.Authorization;
            if (authHeader === `Bearer ${oldToken}`) {
                return {
                    status: 401,
                    ok: false,
                    json: async () => ({ message: 'Unauthorized' }),
                } as Response;
            }
            if (authHeader === `Bearer ${newToken}`) {
                return {
                    status: 200,
                    ok: true,
                    json: async () => ({ data: 'success' }),
                } as Response;
            }
        }
        return { status: 404 } as Response;
    });

    // Trigger two concurrent requests
    const [res1, res2] = await Promise.all([
      fetchApi('/test'),
      fetchApi('/test')
    ]);

    expect(res1).toEqual({ data: 'success' });
    expect(res2).toEqual({ data: 'success' });
    
    // CRITICAL: Refresh should only be called ONCE despite two 401s
    expect(refreshCalledCount).toBe(1);
    expect(localStorage.setItem).toHaveBeenCalledWith('auth_token', newToken);
  });
});

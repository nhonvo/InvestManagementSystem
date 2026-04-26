import { describe, it, expect, vi, beforeEach } from 'vitest';
import { fetchApi } from './api';

describe('fetchApi', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn());
    vi.stubGlobal('localStorage', {
      getItem: vi.fn(),
      setItem: vi.fn(),
      removeItem: vi.fn(),
    });
    // Mock window.location
    vi.stubGlobal('window', {
      location: { href: '', pathname: '/' },
    });
  });

  it('should include Authorization header if token exists', async () => {
    const mockToken = 'test-token';
    vi.mocked(localStorage.getItem).mockReturnValue(mockToken);
    
    vi.mocked(fetch).mockResolvedValue({
      ok: true,
      status: 200,
      headers: { get: vi.fn().mockReturnValue('cid-123') },
      json: async () => ({ data: 'success' }),
    } as any);

    await fetchApi('/test');

    expect(fetch).toHaveBeenCalledWith(
      expect.stringContaining('/test'),
      expect.objectContaining({
        headers: expect.objectContaining({
          Authorization: `Bearer ${mockToken}`,
        }),
      })
    );
  });

  it('should throw error if response is not ok', async () => {
    vi.mocked(fetch).mockResolvedValue({
      ok: false,
      status: 500,
      headers: { get: vi.fn().mockReturnValue('cid-err') },
      json: async () => ({ message: 'Internal Server Error' }),
    } as any);

    await expect(fetchApi('/test')).rejects.toThrow('Internal Server Error');
  });
});

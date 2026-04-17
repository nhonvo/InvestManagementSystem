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
      location: { href: '' },
    });
  });

  it('should include Authorization header if token exists', async () => {
    const mockToken = 'test-token';
    vi.mocked(localStorage.getItem).mockReturnValue(mockToken);
    
    vi.mocked(fetch).mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ data: 'success' }),
    } as Response);

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
      json: async () => ({ message: 'Internal Server Error' }),
    } as Response);

    await expect(fetchApi('/test')).rejects.toThrow('Internal Server Error');
  });
});

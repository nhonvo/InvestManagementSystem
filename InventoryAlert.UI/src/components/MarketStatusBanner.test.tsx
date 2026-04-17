import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MarketStatusBanner } from './MarketStatusBanner';
import { fetchApi } from '@/lib/api';

// Mock the API module
vi.mock('@/lib/api', () => ({
  fetchApi: vi.fn(),
}));

describe('MarketStatusBanner', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should show loading state initially', () => {
    vi.mocked(fetchApi).mockReturnValue(new Promise(() => {})); // Never resolves
    const { container } = render(<MarketStatusBanner />);
    expect(container.firstChild).toHaveClass('animate-pulse');
  });

  it('should display OPEN when market is open', async () => {
    vi.mocked(fetchApi).mockResolvedValue([
      { exchange: 'US', isOpen: true }
    ]);

    render(<MarketStatusBanner />);

    await waitFor(() => {
      expect(screen.getByText(/US: OPEN/i)).toBeInTheDocument();
    });
    expect(screen.getByText(/US: OPEN/i)).toHaveClass('text-emerald-500');
  });

  it('should display CLOSED when market is closed', async () => {
    vi.mocked(fetchApi).mockResolvedValue([
      { exchange: 'NYSE', isOpen: false }
    ]);

    render(<MarketStatusBanner />);

    await waitFor(() => {
      expect(screen.getByText(/NYSE: CLOSED/i)).toBeInTheDocument();
    });
    expect(screen.getByText(/NYSE: CLOSED/i)).toHaveClass('text-rose-500');
  });
});

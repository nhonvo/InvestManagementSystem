import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import NotificationBell from './NotificationBell';
import { fetchApi } from '@/lib/api';

vi.mock('@/lib/api', () => ({
  fetchApi: vi.fn(),
}));

vi.mock('next/link', () => ({
  default: ({ href, className, children }: any) => (
    <a href={href} className={className}>
      {children}
    </a>
  ),
}));

describe('NotificationBell', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows badge when API returns a number', async () => {
    vi.mocked(fetchApi).mockResolvedValue(3);
    render(<NotificationBell />);

    await waitFor(() => {
      expect(screen.getByText('3')).toBeInTheDocument();
    });
  });

  it('shows badge when API returns {count}', async () => {
    vi.mocked(fetchApi).mockResolvedValue({ count: 2 });
    render(<NotificationBell />);

    await waitFor(() => {
      expect(screen.getByText('2')).toBeInTheDocument();
    });
  });

  it('does not show badge when count is 0', async () => {
    vi.mocked(fetchApi).mockResolvedValue(0);
    render(<NotificationBell />);

    await waitFor(() => {
      expect(screen.queryByText('0')).not.toBeInTheDocument();
    });
  });
});


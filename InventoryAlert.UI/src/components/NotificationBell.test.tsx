import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor, cleanup } from '@testing-library/react';
import NotificationBell from './NotificationBell';
import { useNotifications } from './NotificationProvider';

vi.mock('./NotificationProvider', () => ({
  useNotifications: vi.fn(),
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

  afterEach(() => {
    cleanup();
  });

  it('shows badge when unreadCount is positive', async () => {
    vi.mocked(useNotifications).mockReturnValue({
      unreadCount: 3,
      notifications: [],
      decrementCount: vi.fn(),
      markAllAsRead: vi.fn(),
    });

    render(<NotificationBell />);

    await waitFor(() => {
      expect(screen.getByText('3')).toBeInTheDocument();
    });
  });

  it('shows 9+ when unreadCount is high', async () => {
    vi.mocked(useNotifications).mockReturnValue({
      unreadCount: 12,
      notifications: [],
      decrementCount: vi.fn(),
      markAllAsRead: vi.fn(),
    });

    render(<NotificationBell />);

    await waitFor(() => {
      expect(screen.getByText('9+')).toBeInTheDocument();
    });
  });

  it('does not show badge when unreadCount is 0', async () => {
    vi.mocked(useNotifications).mockReturnValue({
      unreadCount: 0,
      notifications: [],
      decrementCount: vi.fn(),
      markAllAsRead: vi.fn(),
    });

    render(<NotificationBell />);

    await waitFor(() => {
      // Bell icon should be there
      expect(screen.getByText('🔔')).toBeInTheDocument();
      // But no badge number
      expect(screen.queryByText('0')).not.toBeInTheDocument();
    });
  });
});

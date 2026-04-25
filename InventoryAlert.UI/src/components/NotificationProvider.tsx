'use client'

import React, { createContext, useContext, useEffect, useState, ReactNode } from 'react';
import { useSignalR } from '@/hooks/useSignalR';
import { fetchApi } from '@/lib/api';

interface Notification {
    id: string;
    userId: string;
    message: string;
    tickerSymbol: string;
    isRead: boolean;
    createdAt: string;
    alertRuleId?: string;
}

interface NotificationContextType {
    unreadCount: number;
    notifications: Notification[];
    decrementCount: () => void;
    markAllAsRead: () => void;
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

export const NotificationProvider = ({ children }: { children: ReactNode }) => {
    const [unreadCount, setUnreadCount] = useState(0);
    const [notifications, setNotifications] = useState<Notification[]>([]);
    const connection = useSignalR('/hubs/notifications');

    useEffect(() => {
        // Initial fetch
        const loadInitialData = async () => {
            try {
                const count = await fetchApi('/api/v1/notifications/unread-count');
                setUnreadCount(typeof count === 'number' ? count : (count?.count || 0));
            } catch (err) {
                console.error("Failed to load initial notifications", err);
            }
        };

        loadInitialData();
    }, []);

    useEffect(() => {
        if (!connection) return;

        connection.on('ReceiveNotification', (notification: Notification) => {
            console.log('[SignalR] New notification received:', notification);
            setUnreadCount(prev => prev + 1);
            setNotifications(prev => [notification, ...prev]);
            
            // Note: In a full implementation, we could trigger a toast here
            // using a toast library or custom component.
        });

        return () => {
            connection.off('ReceiveNotification');
        };
    }, [connection]);

    const decrementCount = () => setUnreadCount(prev => Math.max(0, prev - 1));
    const markAllAsRead = () => setUnreadCount(0);

    return (
        <NotificationContext.Provider value={{ unreadCount, notifications, decrementCount, markAllAsRead }}>
            {children}
        </NotificationContext.Provider>
    );
};

export const useNotifications = () => {
    const context = useContext(NotificationContext);
    if (context === undefined) {
        throw new Error('useNotifications must be used within a NotificationProvider');
    }
    return context;
};

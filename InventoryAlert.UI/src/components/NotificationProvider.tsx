'use client'

import React, { createContext, useContext, useEffect, useState, ReactNode, useRef } from 'react';
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
    token: string | null;
    unreadCount: number;
    notifications: Notification[];
    decrementCount: () => void;
    markAllAsRead: () => void;
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

export const NotificationProvider = ({ children }: { children: ReactNode }) => {
    const [unreadCount, setUnreadCount] = useState(0);
    const [notifications, setNotifications] = useState<Notification[]>([]);
    
    // Use null explicitly as initial state
    const [token, setToken] = useState<string | null>(null);
    
    const lastTokenRef = useRef<string | null>(null);
    const hasLoadedRef = useRef<boolean>(false);

    // Sync token from localStorage
    useEffect(() => {
        const syncToken = () => {
            if (typeof window === "undefined") return;
            const currentToken = localStorage.getItem("auth_token");
            if (currentToken !== lastTokenRef.current) {
                console.log(`[NotificationProvider] Token changed: ${lastTokenRef.current?.slice(0, 8)} -> ${currentToken?.slice(0, 8)}`);
                lastTokenRef.current = currentToken;
                setToken(currentToken);
                hasLoadedRef.current = false;
            }
        };

        syncToken();

        const handleStorageChange = (e: StorageEvent) => {
            if (e.key === "auth_token") {
                console.log("[NotificationProvider] Storage change detected");
                syncToken();
            }
        };
        window.addEventListener('storage', handleStorageChange);
        
        const interval = setInterval(syncToken, 2000);

        return () => {
            window.removeEventListener('storage', handleStorageChange);
            clearInterval(interval);
        };
    }, []);

    // ONLY pass the path if we are 100% sure we have a valid token in the state
    const hubPath = token ? '/hubs/notifications' : null;
    const connection = useSignalR(hubPath);

    useEffect(() => {
        if (!token) {
            setUnreadCount(0);
            setNotifications([]);
            hasLoadedRef.current = false;
            return;
        }

        if (hasLoadedRef.current) return;

        const loadInitialData = async () => {
            try {
                console.log(`[NotificationProvider] Fetching initial data...`);
                const count = await fetchApi('/api/v1/notifications/unread-count');
                setUnreadCount(typeof count === 'number' ? count : (count?.count || 0));
                hasLoadedRef.current = true;
            } catch (err) {
                console.error("[NotificationProvider] Failed to load notifications:", err);
            }
        };

        loadInitialData();
    }, [token]);

    useEffect(() => {
        if (!connection) return;

        connection.on('ReceiveNotification', (notification: Notification) => {
            console.log('[SignalR] New notification received:', notification);
            setUnreadCount(prev => prev + 1);
            setNotifications(prev => [notification, ...prev]);
        });

        return () => {
            connection.off('ReceiveNotification');
        };
    }, [connection]);

    const decrementCount = () => setUnreadCount(prev => Math.max(0, prev - 1));
    const markAllAsRead = () => setUnreadCount(0);

    return (
        <NotificationContext.Provider value={{ token, unreadCount, notifications, decrementCount, markAllAsRead }}>
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

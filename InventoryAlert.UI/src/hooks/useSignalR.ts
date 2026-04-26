'use client'

import { useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:8080";

export const useSignalR = (hubPath: string | null) => {
    const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
    const connectionRef = useRef<signalR.HubConnection | null>(null);

    useEffect(() => {
        // 1. Strict Guard: Absolute refusal to proceed without a path
        if (!hubPath || hubPath === "" || typeof window === "undefined") {
            console.log("[SignalR] Hook idle: no hubPath provided.");
            return;
        }

        const token = localStorage.getItem("auth_token");
        if (!token) {
            console.log("[SignalR] Hook idle: no token in localStorage.");
            return;
        }

        let isMounted = true;

        const startConnection = async () => {
            // Clean up existing if needed
            if (connectionRef.current) {
                await connectionRef.current.stop();
                connectionRef.current = null;
            }

            const fullUrl = `${API_URL}${hubPath}`;
            console.log(`[SignalR] Attempting connection to: ${fullUrl}`);

            try {
                const newConnection = new signalR.HubConnectionBuilder()
                    .withUrl(fullUrl, {
                        accessTokenFactory: () => localStorage.getItem("auth_token") || "",
                        skipNegotiation: false,
                        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
                    })
                    .withAutomaticReconnect()
                    .configureLogging(signalR.LogLevel.Information)
                    .build();

                if (!isMounted) return;

                await newConnection.start();
                
                if (isMounted) {
                    console.log(`[SignalR] Connected successfully to ${hubPath}`);
                    connectionRef.current = newConnection;
                    setConnection(newConnection);
                } else {
                    await newConnection.stop();
                }
            } catch (err) {
                if (isMounted) {
                    console.error(`[SignalR] Connection Error on ${hubPath}:`, err);
                }
            }
        };

        startConnection();

        return () => {
            isMounted = false;
            if (connectionRef.current) {
                const conn = connectionRef.current;
                connectionRef.current = null;
                setConnection(null);
                conn.stop().catch(e => console.warn("[SignalR] Error during stop:", e));
            }
        };
    }, [hubPath]);

    return connection;
};

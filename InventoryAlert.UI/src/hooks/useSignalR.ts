'use client'

import { useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:8080";

export const useSignalR = (hubPath: string) => {
    const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
    const connectionRef = useRef<signalR.HubConnection | null>(null);

    useEffect(() => {
        const token = localStorage.getItem("auth_token");
        if (!token) return;

        const newConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${API_URL}${hubPath}`, {
                accessTokenFactory: () => token,
                skipNegotiation: true,
                transport: signalR.HttpTransportType.WebSockets
            })
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        newConnection.start()
            .then(() => {
                console.log(`[SignalR] Connected to ${hubPath}`);
                setConnection(newConnection);
                connectionRef.current = newConnection;
            })
            .catch(err => console.error(`[SignalR] Connection Error:`, err));

        return () => {
            if (connectionRef.current) {
                connectionRef.current.stop();
            }
        };
    }, [hubPath]);

    return connection;
};

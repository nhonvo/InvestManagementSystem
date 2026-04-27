const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:8080";

let isRefreshing = false;
let refreshPromise: Promise<boolean> | null = null;

export class ApiError extends Error {
  status: number;
  data: any;
  correlationId: string | null;

  constructor(message: string, status: number, data: any = null, correlationId: string | null = null) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.data = data;
    this.correlationId = correlationId;
  }
}

async function tryRefreshToken(): Promise<boolean> {
  try {
    const res = await fetch(`${API_URL}/api/v1/auth/refresh`, {
      method: "POST",
      credentials: "include", // sends the httpOnly refresh cookie
    });
    if (!res.ok) return false;
    const data = await res.json();
    
    // Backend returns AuthTokenPair which has Auth: { AccessToken: "..." }
    const newToken = data?.auth?.accessToken || data?.accessToken;
    
    if (newToken) {
      localStorage.setItem("auth_token", newToken);
      return true;
    }
    return false;
  } catch {
    return false;
  }
}

export async function fetchApi(endpoint: string, options: RequestInit = {}, _isRetry = false): Promise<any> {
  const token = typeof window !== "undefined" ? localStorage.getItem("auth_token") : null;

  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...(options.headers as Record<string, string>),
  };

  const url = endpoint.startsWith("http") ? endpoint : `${API_URL}${endpoint}`;

  const response = await fetch(url, {
    ...options,
    headers,
    credentials: "include", // always send cookies so refresh token flows work
  });

  const correlationId = response.headers.get("X-Correlation-Id");

  // Handle 401 Unauthorized
  if (response.status === 401) {
    const isAuthEndpoint = endpoint.includes("/auth/refresh") || endpoint.includes("/auth/login") || endpoint.includes("/auth/register");
    
    if (!isAuthEndpoint && !_isRetry) {
      if (!isRefreshing) {
        isRefreshing = true;
        refreshPromise = tryRefreshToken();
        const refreshed = await refreshPromise;
        isRefreshing = false;
        refreshPromise = null;
        
        if (refreshed) {
          return fetchApi(endpoint, options, true);
        }
      } else if (refreshPromise) {
        const refreshed = await refreshPromise;
        if (refreshed) {
          return fetchApi(endpoint, options, true);
        }
      }
      
      if (typeof window !== "undefined") {
        localStorage.removeItem("auth_token");
        const path = window.location.pathname.toLowerCase().split('?')[0].replace(/\/$/, "") || "/";
        const isAuthPage = path === "/login" || path === "/register";
        
        if (!isAuthPage) {
          window.location.href = "/login";
        }
      }
    }
    
    throw new ApiError("Unauthorized", 401, null, correlationId);
  }

  if (!response.ok) {
    let errorData;
    try {
      errorData = await response.json();
    } catch {
      errorData = { message: `API Error: ${response.status}` };
    }

    const message = errorData?.errors?.[0]?.message || errorData?.message || `Error ${response.status}`;
    throw new ApiError(message, response.status, errorData, correlationId);
  }

  if (response.status === 204) return null;
  return response.json();
}

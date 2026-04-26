const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:8080";

let isRefreshing = false;
let refreshPromise: Promise<boolean> | null = null;

async function tryRefreshToken(): Promise<boolean> {
  try {
    console.log(`[API] Attempting token refresh via /auth/refresh...`);
    const res = await fetch(`${API_URL}/api/v1/auth/refresh`, {
      method: "POST",
      credentials: "include", // sends the httpOnly refresh cookie
    });
    if (!res.ok) {
      console.error(`[API] Refresh failed with status: ${res.status}`);
      return false;
    }
    const data = await res.json();
    
    const newToken = data?.auth?.accessToken || data?.accessToken;
    
    if (newToken) {
      localStorage.setItem("auth_token", newToken);
      console.log(`[API] Refresh successful, new token acquired.`);
      return true;
    }
    return false;
  } catch (err) {
    console.error(`[API] Refresh exception:`, err);
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

  // Log all API calls in development OR production for now to debug
  console.log(`[API] ${options.method || "GET"} ${url} (Retry: ${_isRetry})`);

  const response = await fetch(url, {
    ...options,
    headers,
    credentials: "include", // always send cookies so refresh token flows work
  });

  // Handle 401 Unauthorized
  if (response.status === 401) {
    const isAuthEndpoint = endpoint.includes("/auth/refresh") || endpoint.includes("/auth/login") || endpoint.includes("/auth/register");
    
    if (isAuthEndpoint || _isRetry) {
      console.warn(`[API] 401 on auth endpoint or retry: ${endpoint}. Skipping refresh/redirect logic.`);
    } else {
      console.warn(`[API] 401 Unauthorized on ${endpoint}. Attempting refresh...`);
      
      if (!isRefreshing) {
        isRefreshing = true;
        refreshPromise = tryRefreshToken();
        const refreshed = await refreshPromise;
        isRefreshing = false;
        refreshPromise = null;
        
        if (refreshed) {
          console.log(`[API] Refresh successful. Retrying ${endpoint}...`);
          return fetchApi(endpoint, options, true);
        }
      } else if (refreshPromise) {
        console.log(`[API] Concurrent 401 on ${endpoint} - waiting for existing refresh...`);
        const refreshed = await refreshPromise;
        if (refreshed) {
          return fetchApi(endpoint, options, true);
        }
      }
      
      if (typeof window !== "undefined") {
        console.error(`[API] 401 Unauthorized on ${endpoint} - Refresh failed or unavailable. Evicting session.`);
        localStorage.removeItem("auth_token");
        
        const path = window.location.pathname.toLowerCase().split('?')[0].replace(/\/$/, "") || "/";
        const isAuthPage = path === "/login" || path === "/register";
        
        if (!isAuthPage) {
          console.warn(`[API] Redirecting from ${window.location.pathname} to /login`);
          window.location.href = "/login";
        } else {
          console.log(`[API] Already on auth page (${path}), suppressing redirect.`);
        }
      }
    }
    
    throw new Error("Unauthorized");
  }

  if (!response.ok) {
    let errorData;
    try {
      errorData = await response.json();
    } catch {
      errorData = { message: `API Error: ${response.status}` };
    }
    const message =
      errorData.userFriendlyMessage ||
      errorData.message ||
      errorData.title ||
      `Error ${response.status}`;
    throw new Error(message);
  }

  if (response.status === 204) return null;
  return response.json();
}

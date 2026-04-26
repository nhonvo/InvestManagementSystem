const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:8080";

let isRefreshing = false;

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

  if (process.env.NODE_ENV === "development") {
    console.log(`[API] ${options.method || "GET"} ${url}`);
  }

  const response = await fetch(url, {
    ...options,
    headers,
    credentials: "include", // always send cookies so refresh token flows work
  });

  // If unauthorized and NOT already trying to refresh and NOT the refresh endpoint itself
  if (response.status === 401 && !_isRetry && !endpoint.includes("/auth/refresh")) {
    // Attempt a single token refresh then retry the original request.
    if (!isRefreshing) {
      isRefreshing = true;
      const refreshed = await tryRefreshToken();
      isRefreshing = false;
      if (refreshed) {
        return fetchApi(endpoint, options, true);
      }
    }
    // Refresh failed — evict session.
    if (typeof window !== "undefined") {
      localStorage.removeItem("auth_token");
      window.location.href = "/login";
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

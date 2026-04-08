const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5294";

export async function fetchApi(endpoint: string, options: RequestInit = {}) {
  const token = typeof window !== "undefined" ? localStorage.getItem("auth_token") : null;
  
  const headers = {
    "Content-Type": "application/json",
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...options.headers,
  };

  const url = endpoint.startsWith("http") ? endpoint : `${API_URL}${endpoint}`;

  if (process.env.NODE_ENV === 'development') {
    console.log(`[API] ${options.method || 'GET'} ${url}`);
  }

  const response = await fetch(url, {
    ...options,
    headers,
  });

  if (response.status === 401) {
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
    
    const message = errorData.message || errorData.title || `Error ${response.status}`;
    throw new Error(message);
  }

  if (response.status === 204) return null;
  return response.json();
}

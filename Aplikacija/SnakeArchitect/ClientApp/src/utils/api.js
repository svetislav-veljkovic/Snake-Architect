export const API_BASE = import.meta.env.VITE_API_URL ?? "http://localhost:5232";
export const HUB_URL = `${API_BASE}/hubs/chat`;

// Recursive camelCase conversion for ASP.NET payloads (PascalCase -> camelCase).
// We only translate keys; primitive values stay as-is.
function camelKey(key) {
  if (key === "ID" || key === "Id") return "id";
  return key.charAt(0).toLowerCase() + key.slice(1);
}

export function normalize(value) {
  if (Array.isArray(value)) return value.map(normalize);

  if (value && typeof value === "object" && !(value instanceof Date)) {
    return Object.fromEntries(
      Object.entries(value).map(([key, item]) => [camelKey(key), normalize(item)])
    );
  }

  return value;
}

async function readResponse(response) {
  const text = await response.text();
  if (!text) return null;

  try {
    return normalize(JSON.parse(text));
  } catch {
    return text;
  }
}

export async function request(path, { method = "GET", body, token } = {}) {
  const headers = {
    Accept: "application/json"
  };

  if (body !== undefined) {
    headers["Content-Type"] = "application/json";
  }

  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  let response;
  try {
    response = await fetch(`${API_BASE}${path}`, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body)
    });
  } catch (networkError) {
    throw new Error("Server nije dostupan. Proveri konekciju i pokusaj opet.");
  }

  const data = await readResponse(response);

  if (!response.ok) {
    const message =
      data?.message ||
      data?.title ||
      (typeof data === "string" ? data : "Zahtev nije uspeo.");
    throw new Error(message);
  }

  return data;
}

export function createApi(token) {
  return {
    get: (path) => request(path, { token }),
    post: (path, body) => request(path, { method: "POST", body, token }),
    put: (path, body) => request(path, { method: "PUT", body, token }),
    delete: (path) => request(path, { method: "DELETE", token })
  };
}

// Pull the user id out of a JWT without depending on a crypto library.
export function readJwtUserId(token) {
  if (!token || typeof token !== "string") return null;
  const parts = token.split(".");
  if (parts.length < 2) return null;

  try {
    const payload = parts[1].replace(/-/g, "+").replace(/_/g, "/");
    const padded = payload + "=".repeat((4 - (payload.length % 4)) % 4);
    const json = atob(padded);
    const data = JSON.parse(json);
    return Number(data?.["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"]) || null;
  } catch {
    return null;
  }
}

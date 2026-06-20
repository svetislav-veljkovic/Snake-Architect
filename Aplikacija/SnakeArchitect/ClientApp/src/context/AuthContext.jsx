import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import React from "react";
import { createApi, readJwtUserId } from "../utils/api.js";

const TOKEN_KEY = "snakeArchitect.token";
const USER_KEY = "snakeArchitect.user";

const AuthContext = createContext(null);

function readStoredUser() {
  try {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }) {
  const [token, setToken] = useState(() => localStorage.getItem(TOKEN_KEY));
  const [user, setUser] = useState(() => {
    const stored = readStoredUser();
    if (stored) return stored;
    // Fallback: derive the user id straight from the JWT if a user payload
    // was not yet persisted (for example after a page refresh mid-session).
    const fallbackId = readJwtUserId(localStorage.getItem(TOKEN_KEY));
    return fallbackId ? { id: fallbackId, userId: fallbackId } : null;
  });

  const api = useMemo(() => createApi(token), [token]);

  const persistSession = useCallback((nextToken, nextUser) => {
    localStorage.setItem(TOKEN_KEY, nextToken);
    localStorage.setItem(USER_KEY, JSON.stringify(nextUser));
    setToken(nextToken);
    setUser(nextUser);
  }, []);

  const login = useCallback(async (credentials) => {
    const data = await createApi().post("/api/User/login", credentials);
    const nextUser = {
      id: data.userId,
      userId: data.userId,
      username: data.username
    };
    persistSession(data.token, nextUser);
    return data;
  }, [persistSession]);

  const register = useCallback(async (payload) => {
    return createApi().post("/api/User/register", payload);
  }, []);

  const refreshProfile = useCallback(async () => {
    if (!token || !user?.userId) return null;
    try {
      const profile = await createApi(token).get(`/api/User/${user.userId}`);
      const merged = {
        ...user,
        ...profile,
        id: user.userId,
        userId: user.userId,
        username: profile.username ?? user.username
      };
      localStorage.setItem(USER_KEY, JSON.stringify(merged));
      setUser(merged);
      return merged;
    } catch {
      return user;
    }
  }, [token, user]);

  const logout = useCallback(() => {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    setToken(null);
    setUser(null);
  }, []);

  // Keep the stored user payload in sync with whatever is currently in state.
  useEffect(() => {
    if (user) {
      localStorage.setItem(USER_KEY, JSON.stringify(user));
    }
  }, [user]);

  const value = useMemo(
    () => ({
      api,
      token,
      user,
      isAuthenticated: Boolean(token && user),
      login,
      logout,
      refreshProfile,
      register
    }),
    [api, token, user, login, logout, refreshProfile, register]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth mora biti pozvan unutar AuthProvider-a.");
  }
  return context;
}

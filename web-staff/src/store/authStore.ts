import { create } from "zustand";
import { persist } from "zustand/middleware";
import type { AuthUser } from "../auth/roles";

type AuthState = {
  token: string | null;
  user: AuthUser | null;
  login: (token: string, user: AuthUser) => void;
  logout: () => void;
};

/**
 * JWT is persisted in localStorage for this student project so the SPA can attach
 * Authorization on each request after a refresh.
 *
 * Production should not keep bearer tokens in localStorage (XSS can read them).
 * Move to an httpOnly Secure cookie issued by Hospital.Api, or equivalent
 * browser-inaccessible storage. See docs/adr/0010-staff-jwt-localstorage.md.
 */
export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      user: null,
      login: (token, user) => set({ token, user }),
      logout: () => set({ token: null, user: null })
    }),
    { name: "sah-staff-auth" }
  )
);

function decodeJwtPayload(token: string): { exp?: number } | null {
  const segment = token.split(".")[1];
  if (!segment) {
    return null;
  }

  try {
    const normalized = segment.replace(/-/g, "+").replace(/_/g, "/");
    const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, "=");
    return JSON.parse(atob(padded)) as { exp?: number };
  } catch {
    return null;
  }
}

export function hasValidJwt(token: string | null | undefined): boolean {
  if (!token) {
    return false;
  }

  const payload = decodeJwtPayload(token);
  if (!payload) {
    return false;
  }

  if (typeof payload.exp === "number") {
    return payload.exp * 1000 > Date.now();
  }

  return true;
}

import { hasValidJwt, useAuthStore } from "../store/authStore";
import type { AuthUser } from "../auth/roles";

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5080/api").replace(
  /\/$/,
  ""
);

export class ApiError extends Error {
  readonly status: number;
  readonly fields: Record<string, string[]>;

  constructor(message: string, status: number, fields: Record<string, string[]> = {}) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.fields = fields;
  }
}

export type AuthResponse = {
  token: string;
  expiresAt: string;
  user: AuthUser;
};

type ProblemBody = {
  title?: string;
  detail?: string;
  error?: string;
  errors?: Record<string, string[]>;
};

function joinUrl(path: string): string {
  return `${API_BASE_URL}/${path.replace(/^\//, "")}`;
}

function isLoginPath(path: string): boolean {
  return path.replace(/^\//, "").toLowerCase() === "auth/login";
}

async function readProblem(response: Response): Promise<{ message: string; fields: Record<string, string[]> }> {
  const body = (await response.json().catch(() => null)) as ProblemBody | null;
  const fields = body?.errors ?? {};
  const fieldMessages = Object.values(fields).flat();
  const message =
    fieldMessages[0] ??
    body?.detail ??
    body?.error ??
    body?.title ??
    response.statusText ??
    "Request failed";
  return { message, fields };
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers);
  if (!headers.has("Content-Type") && init.body) {
    headers.set("Content-Type", "application/json");
  }

  const token = useAuthStore.getState().token;
  if (token && hasValidJwt(token)) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  let response: Response;
  try {
    response = await fetch(joinUrl(path), { ...init, headers });
  } catch {
    throw new ApiError("Unable to reach the hospital API.", 0);
  }

  if (response.status === 401 && !isLoginPath(path)) {
    useAuthStore.getState().logout();
    if (window.location.pathname !== "/login") {
      window.location.assign("/login");
    }
    throw new ApiError("Please sign in again.", 401);
  }

  if (!response.ok) {
    const problem = await readProblem(response);
    throw new ApiError(problem.message, response.status, problem.fields);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export const api = {
  request,
  login: (email: string, password: string) =>
    request<AuthResponse>("/auth/login", {
      method: "POST",
      body: JSON.stringify({ email, password })
    })
};

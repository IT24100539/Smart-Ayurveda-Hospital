export type StaffRole = "Admin" | "Doctor" | "Therapist" | "Receptionist" | "Pharmacist" | "Nurse";

export type AuthResponse = {
  accessToken: string;
  expiresAt: string;
  userId: string;
  fullName: string;
  email: string;
  role: StaffRole;
};

export type Patient = {
  id: string;
  uhid: string;
  firstName: string;
  lastName: string;
  phone: string;
  prakriti: string | number;
  vikriti: string | number;
};

export type Appointment = {
  id: string;
  patientName: string;
  patientUhid: string;
  doctorName: string;
  scheduledAt: string;
  status: string | number;
  reason: string;
};

export type Paged<T> = {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
};

const TOKEN_KEY = "sah_staff_token";

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function setSession(auth: AuthResponse): void {
  localStorage.setItem(TOKEN_KEY, auth.accessToken);
  localStorage.setItem("sah_staff_name", auth.fullName);
  localStorage.setItem("sah_staff_role", auth.role);
}

export function clearSession(): void {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem("sah_staff_name");
  localStorage.removeItem("sah_staff_role");
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set("Content-Type", "application/json");
  const token = getToken();
  if (token) headers.set("Authorization", `Bearer ${token}`);

  const response = await fetch(path, { ...init, headers });
  if (response.status === 401) {
    clearSession();
    throw new Error("Please sign in again.");
  }
  if (!response.ok) {
    const body = await response.json().catch(() => ({ error: response.statusText }));
    throw new Error(body.error ?? "Request failed");
  }
  return response.json() as Promise<T>;
}

export const api = {
  login: (email: string, password: string) =>
    request<AuthResponse>("/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email, password })
    }),
  patients: (q = "") => request<Paged<Patient>>(`/api/patients?q=${encodeURIComponent(q)}`),
  createPatient: (payload: Record<string, unknown>) =>
    request<Patient>("/api/patients", { method: "POST", body: JSON.stringify(payload) }),
  appointments: () => request<Paged<Appointment>>("/api/appointments")
};

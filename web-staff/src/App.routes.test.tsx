import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import App from "./App";
import type { UserRole } from "./auth/roles";
import { useAuthStore } from "./store/authStore";

vi.mock("./api/doctors", () => ({
  listDoctors: vi.fn(async () => ({ items: [], totalCount: 0, page: 1, pageSize: 20 })),
  createDoctor: vi.fn(),
  updateDoctor: vi.fn(),
  deactivateDoctor: vi.fn(),
  uploadDoctorPhoto: vi.fn(),
  removeDoctorPhoto: vi.fn()
}));

vi.mock("./api/auditLogs", () => ({
  listAuditLogs: vi.fn(async () => ({ items: [], totalCount: 0, page: 1, pageSize: 20 }))
}));

function unsignedJwt(): string {
  const body = btoa(JSON.stringify({ sub: "user-1" }))
    .replace(/\+/g, "-")
    .replace(/\//g, "_")
    .replace(/=+$/g, "");
  return `e30.${body}.sig`;
}

function signIn(role: UserRole, fullName: string) {
  useAuthStore.setState({
    token: unsignedJwt(),
    user: {
      id: "user-1",
      fullName,
      email: "user@hospital.local",
      phoneNumber: "0770000000",
      role
    }
  });
}

function renderAt(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]} future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
      <App />
    </MemoryRouter>
  );
}

describe("staff routes", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  beforeEach(() => {
    class ResizeObserverStub {
      observe() {}
      unobserve() {}
      disconnect() {}
    }
    vi.stubGlobal("ResizeObserver", ResizeObserverStub);
    Object.defineProperty(window, "matchMedia", {
      writable: true,
      configurable: true,
      value: (query: string) => ({
        matches: true,
        media: query,
        onchange: null,
        addEventListener: () => undefined,
        removeEventListener: () => undefined,
        addListener: () => undefined,
        removeListener: () => undefined,
        dispatchEvent: () => false
      })
    });
    localStorage.clear();
    useAuthStore.setState({ token: null, user: null });
    vi.spyOn(globalThis, "fetch").mockImplementation(async (input) => {
      const url = String(input);
      const body =
        url.includes("/wards") || url.includes("/admissions") || url.includes("/complaints") || url.includes("/notifications")
          ? []
          : url.includes("/feedback/summary")
            ? { total: 0, averageRating: 0, pendingModeration: 0, negative: 0 }
            : { items: [], totalCount: 0, page: 1, pageSize: 20 };
      return new Response(JSON.stringify(body), {
        status: 200,
        headers: { "Content-Type": "application/json" }
      });
    });
  });

  it("sends an unknown visitor from the root to staff sign-in", async () => {
    renderAt("/");
    expect(await screen.findByRole("heading", { name: /staff sign-in/i })).toBeInTheDocument();
  });

  it("sends a signed-in staff member from the root to the dashboard", async () => {
    signIn("FrontDeskStaff", "Nimal Perera");
    renderAt("/");
    expect(await screen.findByRole("heading", { name: /nimal perera/i })).toBeInTheDocument();
  });

  it("keeps the patient-account block when a patient opens the portal", async () => {
    signIn("Patient", "Meera Nair");
    renderAt("/");
    expect(await screen.findByRole("heading", { name: /staff sign-in/i })).toBeInTheDocument();
    expect(await screen.findByText(/patients, please use the smart ayurveda mobile app/i)).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: /nimal perera|meera nair/i })).not.toBeInTheDocument();
  });

  it("lets an administrator open doctors and the audit log", async () => {
    signIn("Admin", "Hospital Administrator");
    renderAt("/doctors");
    expect(await screen.findByRole("heading", { name: "Doctors" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Doctors" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Audit log" })).toBeInTheDocument();
    expect(await screen.findByText("No doctors found.")).toBeInTheDocument();
  });

  it("lets a doctor open prescriptions and keeps billing at the desk", async () => {
    signIn("Doctor", "Vd. Ananya Sharma");
    const { unmount } = renderAt("/prescriptions");
    expect(await screen.findByRole("heading", { name: "Prescriptions" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Prescriptions" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Billing" })).not.toBeInTheDocument();
    expect(await screen.findByText("No prescriptions found.")).toBeInTheDocument();
    unmount();

    renderAt("/billing");
    expect(await screen.findByRole("heading", { name: "Access denied" })).toBeInTheDocument();
  });

  it("lets the front desk and an administrator open billing", async () => {
    signIn("FrontDeskStaff", "Nimal Perera");
    const { unmount } = renderAt("/billing");
    expect(await screen.findByRole("heading", { name: "Billing" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Billing" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Prescriptions" })).not.toBeInTheDocument();
    expect(await screen.findByText("No invoices found.")).toBeInTheDocument();
    unmount();

    signIn("Admin", "Hospital Administrator");
    renderAt("/billing");
    expect(await screen.findByRole("heading", { name: "Billing" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Billing" })).toBeInTheDocument();
  });

  it("lets staff open documents, notifications, and exports", async () => {
    signIn("Doctor", "Vd. Ananya Sharma");
    const { unmount } = renderAt("/documents");
    expect(await screen.findByRole("heading", { name: "Documents" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Documents" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Notifications" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Exports" })).toBeInTheDocument();
    expect(await screen.findByText("Choose a patient")).toBeInTheDocument();
    unmount();

    renderAt("/exports");
    expect(await screen.findByRole("heading", { name: "Exports" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Download appointments" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Download invoices" })).not.toBeInTheDocument();
  });

  it("lets the front desk export invoices and read notifications", async () => {
    signIn("FrontDeskStaff", "Nimal Perera");
    const { unmount } = renderAt("/exports");
    expect(await screen.findByRole("heading", { name: "Exports" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Download invoices" })).toBeInTheDocument();
    unmount();

    renderAt("/notifications");
    expect(await screen.findByRole("heading", { name: "Notifications" })).toBeInTheDocument();
    expect(await screen.findByText("No notifications.")).toBeInTheDocument();
  });

  it("refuses doctors and the audit log to other staff", async () => {
    signIn("Doctor", "Vd. Ananya Sharma");
    const { unmount } = renderAt("/doctors");
    expect(await screen.findByRole("heading", { name: "Access denied" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Doctors" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Audit log" })).not.toBeInTheDocument();
    unmount();

    renderAt("/audit-logs");
    expect(await screen.findByRole("heading", { name: "Access denied" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Audit log" })).not.toBeInTheDocument();
  });
});

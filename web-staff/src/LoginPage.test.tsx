import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { LoginPage } from "./pages/LoginPage";
import { useAuthStore } from "./store/authStore";

function unsignedJwt(payload: Record<string, unknown>): string {
  const body = btoa(JSON.stringify(payload))
    .replace(/\+/g, "-")
    .replace(/\//g, "_")
    .replace(/=+$/g, "");
  return `e30.${body}.sig`;
}

describe("LoginPage", () => {
  beforeEach(() => {
    localStorage.clear();
    useAuthStore.setState({ token: null, user: null });
  });

  it("renders staff sign-in", () => {
    render(
      <MemoryRouter future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
        <LoginPage />
      </MemoryRouter>
    );

    expect(screen.getByRole("heading", { name: /staff sign-in/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /sign in/i })).toBeInTheDocument();
  });

  it("sends a signed-in patient back to the form without a dashboard redirect", async () => {
    const consoleError = vi.spyOn(console, "error").mockImplementation(() => {});
    useAuthStore.setState({
      token: unsignedJwt({ sub: "patient-1" }),
      user: {
        id: "patient-1",
        fullName: "Meera Nair",
        email: "meera.nair@example.local",
        phoneNumber: "9876500001",
        role: "Patient"
      }
    });

    render(
      <MemoryRouter initialEntries={["/login"]} future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/dashboard" element={<h1>Dashboard</h1>} />
        </Routes>
      </MemoryRouter>
    );

    expect(await screen.findByRole("heading", { name: /staff sign-in/i })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Dashboard" })).not.toBeInTheDocument();
    await waitFor(() => expect(useAuthStore.getState().token).toBeNull());
    expect(await screen.findByText(/patients, please use the smart ayurveda mobile app/i)).toBeInTheDocument();
    expect(consoleError).not.toHaveBeenCalled();
    consoleError.mockRestore();
  });
});

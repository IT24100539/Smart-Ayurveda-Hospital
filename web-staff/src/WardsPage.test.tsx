import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { BedGrid, WardsPage } from "./pages/WardsPage";
import { useAuthStore } from "./store/authStore";

describe("Ward dashboard", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    useAuthStore.setState({
      token: null,
      user: { id: "staff-42", fullName: "Ward Admin", email: "admin@test.local", phoneNumber: "0", role: "Admin" }
    });
  });

  it("renders occupied and free beds from ward data", () => {
    render(<BedGrid beds={[{ id: "1", bedLabel: "A-01", isOccupied: true }, { id: "2", bedLabel: "A-02", isOccupied: false }]} />);
    expect(screen.getByTestId("bed-occupied")).toHaveTextContent("A-01");
    expect(screen.getByTestId("bed-free")).toHaveTextContent("A-02");
    expect(screen.getByTitle("A-01: Occupied")).toHaveClass("bg-primary");
    expect(screen.getByTitle("A-02: Free")).toHaveClass("bg-transparent");
  });

  it("approve sends the admission decision PATCH", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockImplementation(async (input, init) => {
      const url = String(input);
      if (url.endsWith("/wards")) return new Response(JSON.stringify([]), { status: 200 });
      if (url.endsWith("/admissions") && (!init?.method || init.method === "GET")) {
        return new Response(JSON.stringify([{ id: "request-7", patientName: "Nimali Perera", wardId: "ward-1", reason: "Observation", preferredDate: "2026-09-20", requestedByAgent: true }]), { status: 200 });
      }
      if (url.endsWith("/admissions/request-7/decision")) return new Response(null, { status: 204 });
      return new Response(null, { status: 404 });
    });
    vi.spyOn(window, "confirm").mockReturnValue(true);

    render(<WardsPage />);
    fireEvent.click(await screen.findByRole("button", { name: "Approve" }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining("/admissions/request-7/decision"),
      expect.objectContaining({ method: "PATCH", body: JSON.stringify({ approve: true, decidedBy: "staff-42" }) })
    ));
  });
});

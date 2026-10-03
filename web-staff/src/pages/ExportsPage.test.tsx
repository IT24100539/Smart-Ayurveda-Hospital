import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "../api/client";
import * as exportsApi from "../api/exports";
import type { AppointmentExportRow } from "../api/exports";
import * as csv from "../exports/csv";
import type { Invoice } from "../api/invoices";
import { useAuthStore } from "../store/authStore";
import { ExportsPage } from "./ExportsPage";

vi.mock("../api/exports", () => ({
  collectAppointments: vi.fn(),
  collectInvoices: vi.fn()
}));

vi.mock("../exports/csv", async () => {
  const actual = await vi.importActual<typeof import("../exports/csv")>("../exports/csv");
  return { ...actual, downloadCsv: vi.fn() };
});

const visit: AppointmentExportRow = {
  id: "appt-1",
  patientName: "Meera Nair",
  patientUhid: "UH100",
  treatmentName: "Abhyanga",
  requestedDate: "2026-10-02",
  requestedTimeSlot: "Morning",
  status: "Approved"
};

function invoice(): Invoice {
  return {
    id: "inv-1",
    invoiceNumber: "INV-1001",
    patientId: "11111111-1111-1111-1111-111111111111",
    currency: "LKR",
    status: "Issued",
    total: 2500,
    amountPaid: 0,
    balance: 2500,
    createdByUserId: "user-1",
    createdAt: "2026-10-02T06:30:00.000Z",
    updatedAt: "2026-10-02T06:30:00.000Z",
    issuedAt: "2026-10-02T07:00:00.000Z",
    paidAt: null,
    cancelledAt: null,
    notes: null,
    lines: [],
    payments: []
  };
}

function signIn(role: "FrontDeskStaff" | "Doctor" | "Admin") {
  useAuthStore.setState({
    token: "e30.e30.sig",
    user: {
      id: "user-1",
      fullName: role === "Doctor" ? "Vd. Ananya Sharma" : "Nimal Perera",
      email: "staff@hospital.local",
      phoneNumber: "0770000000",
      role
    }
  });
}

describe("ExportsPage", () => {
  beforeEach(() => {
    signIn("FrontDeskStaff");
    vi.mocked(exportsApi.collectAppointments).mockReset();
    vi.mocked(exportsApi.collectInvoices).mockReset();
    vi.mocked(csv.downloadCsv).mockReset();
  });

  it("validates the visit range, then downloads a file", async () => {
    vi.mocked(exportsApi.collectAppointments).mockResolvedValue([visit]);
    render(<ExportsPage />);

    fireEvent.click(screen.getByRole("button", { name: "Download appointments" }));
    expect(await screen.findByRole("alert")).toHaveTextContent("Start date is required.");

    fireEvent.change(screen.getByLabelText("Appointments from"), { target: { value: "2026-10-03" } });
    fireEvent.change(screen.getByLabelText("Appointments to"), { target: { value: "2026-10-01" } });
    fireEvent.click(screen.getByRole("button", { name: "Download appointments" }));
    expect(await screen.findByRole("alert")).toHaveTextContent("The end date must be on or after the start date.");

    fireEvent.change(screen.getByLabelText("Appointments from"), { target: { value: "2026-10-01" } });
    fireEvent.change(screen.getByLabelText("Appointments to"), { target: { value: "2026-10-02" } });
    fireEvent.click(screen.getByRole("button", { name: "Download appointments" }));

    await waitFor(() => expect(exportsApi.collectAppointments).toHaveBeenCalledWith("2026-10-01", "2026-10-02"));
    expect(csv.downloadCsv).toHaveBeenCalledWith(
      "appointments-2026-10-01-to-2026-10-02.csv",
      expect.stringContaining("Meera Nair")
    );
  });

  it("shows an empty range and an error that can be retried", async () => {
    let resolveEmpty: (value: AppointmentExportRow[]) => void = () => {};
    vi.mocked(exportsApi.collectAppointments)
      .mockImplementationOnce(
        () =>
          new Promise((resolve) => {
            resolveEmpty = resolve;
          })
      )
      .mockRejectedValueOnce(new ApiError("The visit book is unavailable.", 503))
      .mockResolvedValueOnce([visit]);

    render(<ExportsPage />);
    fireEvent.change(screen.getByLabelText("Appointments from"), { target: { value: "2026-10-01" } });
    fireEvent.change(screen.getByLabelText("Appointments to"), { target: { value: "2026-10-02" } });
    fireEvent.click(screen.getByRole("button", { name: "Download appointments" }));
    expect(screen.getByText("Preparing the appointments file…")).toBeInTheDocument();
    resolveEmpty([]);
    expect(await screen.findByText("No appointments in this range.")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Download appointments" }));
    expect(await screen.findByText("The visit book is unavailable.")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Try again" }));
    await waitFor(() => expect(csv.downloadCsv).toHaveBeenCalled());
  });

  it("downloads bills for the front desk and hides that export from a doctor", async () => {
    vi.mocked(exportsApi.collectInvoices).mockResolvedValue([invoice()]);
    const { unmount } = render(<ExportsPage />);
    fireEvent.change(screen.getByLabelText("Invoices from"), { target: { value: "2026-10-01" } });
    fireEvent.change(screen.getByLabelText("Invoices to"), { target: { value: "2026-10-02" } });
    fireEvent.click(screen.getByRole("button", { name: "Download invoices" }));
    await waitFor(() => expect(exportsApi.collectInvoices).toHaveBeenCalledWith("2026-10-01", "2026-10-02"));
    expect(csv.downloadCsv).toHaveBeenCalledWith(
      "invoices-2026-10-01-to-2026-10-02.csv",
      expect.stringContaining("INV-1001")
    );
    unmount();

    signIn("Doctor");
    render(<ExportsPage />);
    expect(screen.getByRole("button", { name: "Download appointments" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Download invoices" })).not.toBeInTheDocument();
  });
});

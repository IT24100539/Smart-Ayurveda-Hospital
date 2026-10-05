import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "../api/client";
import * as invoicesApi from "../api/invoices";
import type { Invoice } from "../api/invoices";
import { BillingPage } from "./BillingPage";

vi.mock("../api/invoices", () => ({
  listInvoices: vi.fn(),
  createInvoice: vi.fn(),
  issueInvoice: vi.fn(),
  cancelInvoice: vi.fn(),
  recordInvoicePayment: vi.fn(),
  listPatientVisits: vi.fn()
}));

vi.mock("../api/patients", () => ({
  searchPatients: vi.fn()
}));

const patientId = "11111111-1111-1111-1111-111111111111";
const appointmentId = "22222222-2222-2222-2222-222222222222";
const admissionId = "33333333-3333-3333-3333-333333333333";

const draft: Invoice = {
  id: "inv-1",
  invoiceNumber: "INV-1001",
  patientId,
  currency: "LKR",
  status: "Draft",
  total: 2500,
  amountPaid: 0,
  balance: 2500,
  createdByUserId: "desk-1",
  createdAt: "2026-10-01T08:00:00Z",
  updatedAt: "2026-10-01T08:00:00Z",
  issuedAt: null,
  paidAt: null,
  cancelledAt: null,
  notes: null,
  lines: [{
    id: "line-1",
    source: "Treatment",
    appointmentId,
    treatmentId: "treat-1",
    admissionId: null,
    description: "Abhyanga",
    quantity: 1,
    unitPrice: 2500,
    lineTotal: 2500
  }],
  payments: []
};

const issued: Invoice = {
  ...draft,
  id: "inv-2",
  invoiceNumber: "INV-1002",
  status: "Issued",
  issuedAt: "2026-10-02T08:00:00Z",
  lines: [{ ...draft.lines[0], id: "line-2", description: "Shirodhara" }]
};

const page = { items: [draft, issued], totalCount: 2, page: 1, pageSize: 20 };

describe("BillingPage", () => {
  beforeEach(() => {
    vi.mocked(invoicesApi.listInvoices).mockReset();
    vi.mocked(invoicesApi.listInvoices).mockResolvedValue(page);
    vi.mocked(invoicesApi.createInvoice).mockReset();
    vi.mocked(invoicesApi.issueInvoice).mockReset();
    vi.mocked(invoicesApi.cancelInvoice).mockReset();
    vi.mocked(invoicesApi.recordInvoicePayment).mockReset();
  });

  it("shows a loading state, then invoices", async () => {
    let resolveList: (value: typeof page) => void = () => {};
    vi.mocked(invoicesApi.listInvoices).mockImplementation(
      () => new Promise((resolve) => {
        resolveList = resolve;
      })
    );
    render(<BillingPage />);
    expect(screen.getByText("Loading invoices…")).toBeInTheDocument();
    resolveList(page);
    expect(await screen.findByText("INV-1001")).toBeInTheDocument();
    expect(screen.getByText("Abhyanga")).toBeInTheDocument();
  });

  it("shows an empty state", async () => {
    vi.mocked(invoicesApi.listInvoices).mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });
    render(<BillingPage />);
    expect(await screen.findByText("No invoices found.")).toBeInTheDocument();
  });

  it("shows an error and retries", async () => {
    vi.mocked(invoicesApi.listInvoices)
      .mockRejectedValueOnce(new ApiError("The ledger is unavailable.", 503))
      .mockResolvedValue(page);
    render(<BillingPage />);
    expect(await screen.findByText("The ledger is unavailable.")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Try again" }));
    expect(await screen.findByText("INV-1001")).toBeInTheDocument();
  });

  it("validates a bill and creates it from a visit and an admission", async () => {
    vi.mocked(invoicesApi.createInvoice).mockResolvedValue({
      ...draft,
      id: "inv-3",
      invoiceNumber: "INV-1003"
    });
    render(<BillingPage />);
    await screen.findByText("INV-1001");
    fireEvent.click(screen.getByRole("button", { name: "New invoice" }));
    fireEvent.click(screen.getByRole("button", { name: "Save invoice" }));
    expect(screen.getByRole("alert")).toHaveTextContent("Patient is required.");

    fireEvent.change(screen.getByLabelText("Patient"), { target: { value: patientId } });
    fireEvent.change(screen.getByLabelText("Currency"), { target: { value: "rupee" } });
    fireEvent.click(screen.getByRole("button", { name: "Save invoice" }));
    expect(screen.getByRole("alert")).toHaveTextContent("Currency must be a three-letter code, or left blank for LKR.");

    fireEvent.change(screen.getByLabelText("Currency"), { target: { value: "" } });
    fireEvent.click(screen.getByRole("button", { name: "Save invoice" }));
    expect(screen.getByRole("alert")).toHaveTextContent("Line 1: a treatment visit is required.");

    fireEvent.change(screen.getByLabelText("Appointment 1"), { target: { value: appointmentId } });
    fireEvent.click(screen.getByRole("button", { name: "Add line" }));
    fireEvent.change(screen.getByLabelText("Source 2"), { target: { value: "admission" } });
    fireEvent.change(screen.getByLabelText("Admission 2"), { target: { value: admissionId } });
    fireEvent.change(screen.getByLabelText("Days 2"), { target: { value: "0" } });
    fireEvent.change(screen.getByLabelText("Daily rate 2"), { target: { value: "12.345" } });
    fireEvent.click(screen.getByRole("button", { name: "Save invoice" }));
    expect(screen.getByRole("alert")).toHaveTextContent("Line 2: an admission is billed in days, from 1 to 366.");

    fireEvent.change(screen.getByLabelText("Days 2"), { target: { value: "3" } });
    fireEvent.click(screen.getByRole("button", { name: "Save invoice" }));
    expect(screen.getByRole("alert")).toHaveTextContent("Line 2: daily rate can have at most two decimal places.");
    expect(invoicesApi.createInvoice).not.toHaveBeenCalled();

    fireEvent.change(screen.getByLabelText("Daily rate 2"), { target: { value: "1500.50" } });
    fireEvent.click(screen.getByRole("button", { name: "Save invoice" }));
    await waitFor(() => {
      expect(invoicesApi.createInvoice).toHaveBeenCalledWith({
        patientId,
        currency: null,
        notes: null,
        lines: [
          { appointmentId, admissionId: null, quantity: 1, unitPrice: null },
          { appointmentId: null, admissionId, quantity: 3, unitPrice: 1500.5 }
        ]
      });
    });
    expect(await screen.findByText("INV-1003")).toBeInTheDocument();
  });

  it("issues, records a manual payment, and cancels a draft", async () => {
    vi.mocked(invoicesApi.issueInvoice).mockResolvedValue({ ...draft, status: "Issued", invoiceNumber: "INV-1001" });
    vi.mocked(invoicesApi.recordInvoicePayment).mockResolvedValue({ ...issued, status: "Paid", amountPaid: 2500, balance: 0 });
    vi.mocked(invoicesApi.cancelInvoice).mockResolvedValue({ ...draft, status: "Cancelled" });

    render(<BillingPage />);
    await screen.findByText("INV-1001");
    fireEvent.click(screen.getByRole("button", { name: "Issue INV-1001" }));
    await waitFor(() => expect(invoicesApi.issueInvoice).toHaveBeenCalledWith("inv-1"));

    fireEvent.click(screen.getByRole("button", { name: "Record payment for INV-1002" }));
    fireEvent.change(screen.getByLabelText("Paid on"), { target: { value: "2027-01-01" } });
    fireEvent.click(screen.getByRole("button", { name: "Save payment" }));
    expect(screen.getByRole("alert")).toHaveTextContent("The payment date cannot be in the future.");
    expect(invoicesApi.recordInvoicePayment).not.toHaveBeenCalled();

    fireEvent.change(screen.getByLabelText("Paid on"), { target: { value: "2026-10-01" } });
    fireEvent.change(screen.getByLabelText("Reference"), { target: { value: "counter-12" } });
    fireEvent.click(screen.getByRole("button", { name: "Save payment" }));
    await waitFor(() => {
      expect(invoicesApi.recordInvoicePayment).toHaveBeenCalledWith("inv-2", expect.objectContaining({
        amount: 2500,
        method: "Cash",
        reference: "counter-12"
      }));
    });

    fireEvent.click(screen.getByRole("button", { name: "Cancel INV-1001" }));
    fireEvent.click(screen.getByRole("button", { name: "Confirm cancel INV-1001" }));
    await waitFor(() => expect(invoicesApi.cancelInvoice).toHaveBeenCalledWith("inv-1"));
  });

  it("filters the ledger by status", async () => {
    render(<BillingPage />);
    await screen.findByText("INV-1001");
    fireEvent.change(screen.getByLabelText("Status"), { target: { value: "Issued" } });
    fireEvent.click(screen.getByRole("button", { name: "Search" }));
    await waitFor(() => {
      expect(invoicesApi.listInvoices).toHaveBeenLastCalledWith(expect.objectContaining({ status: "Issued", page: 1 }));
    });
  });
});

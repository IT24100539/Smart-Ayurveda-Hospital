import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "../api/client";
import * as prescriptionsApi from "../api/prescriptions";
import type { Prescription } from "../api/prescriptions";
import { PrescriptionsPage } from "./PrescriptionsPage";

vi.mock("../api/prescriptions", () => ({
  listPrescriptions: vi.fn(),
  getPrescriptionHistory: vi.fn(),
  createPrescription: vi.fn(),
  issuePrescription: vi.fn(),
  revisePrescription: vi.fn()
}));

vi.mock("../api/patients", () => ({
  searchPatients: vi.fn()
}));

const patientId = "11111111-1111-1111-1111-111111111111";
const appointmentId = "22222222-2222-2222-2222-222222222222";

const draft: Prescription = {
  id: "rx-draft",
  patientId,
  appointmentId,
  doctorUserId: "doc-1",
  doctorName: "Vd. Ananya Sharma",
  status: "Draft",
  revisionNumber: 1,
  rootPrescriptionId: "rx-draft",
  revisesPrescriptionId: null,
  supersededByPrescriptionId: null,
  createdAt: "2026-10-01T08:00:00Z",
  updatedAt: "2026-10-01T08:00:00Z",
  issuedAt: null,
  cancelledAt: null,
  supersededAt: null,
  items: [{ id: "item-1", name: "Triphala", dosage: "5 g", frequency: "Night", duration: "7 days", instructions: "" }]
};

const issued: Prescription = {
  ...draft,
  id: "rx-issued",
  status: "Issued",
  issuedAt: "2026-10-01T09:00:00Z",
  items: [{ id: "item-2", name: "Ashwagandha", dosage: "3 g", frequency: "Morning", duration: "14 days", instructions: "With warm water" }]
};

const page = { items: [draft, issued], totalCount: 2, page: 1, pageSize: 20 };

function fillMedicine() {
  fireEvent.change(screen.getByLabelText("Medicine name 1"), { target: { value: "Brahmi" } });
  fireEvent.change(screen.getByLabelText("Dosage 1"), { target: { value: "2 g" } });
  fireEvent.change(screen.getByLabelText("Frequency 1"), { target: { value: "Twice daily" } });
  fireEvent.change(screen.getByLabelText("Duration 1"), { target: { value: "10 days" } });
}

describe("PrescriptionsPage", () => {
  beforeEach(() => {
    vi.mocked(prescriptionsApi.listPrescriptions).mockReset();
    vi.mocked(prescriptionsApi.listPrescriptions).mockResolvedValue(page);
    vi.mocked(prescriptionsApi.createPrescription).mockReset();
    vi.mocked(prescriptionsApi.issuePrescription).mockReset();
    vi.mocked(prescriptionsApi.revisePrescription).mockReset();
    vi.mocked(prescriptionsApi.getPrescriptionHistory).mockReset();
  });

  it("shows a loading state, then issued and draft charts", async () => {
    let resolveList: (value: typeof page) => void = () => {};
    vi.mocked(prescriptionsApi.listPrescriptions).mockImplementation(
      () => new Promise((resolve) => {
        resolveList = resolve;
      })
    );
    render(<PrescriptionsPage />);
    expect(screen.getByText("Loading prescriptions…")).toBeInTheDocument();
    resolveList(page);
    expect(await screen.findByText("Ashwagandha")).toBeInTheDocument();
    expect(screen.getByText("Triphala")).toBeInTheDocument();
  });

  it("shows an empty state", async () => {
    vi.mocked(prescriptionsApi.listPrescriptions).mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });
    render(<PrescriptionsPage />);
    expect(await screen.findByText("No prescriptions found.")).toBeInTheDocument();
  });

  it("shows an error and retries", async () => {
    vi.mocked(prescriptionsApi.listPrescriptions)
      .mockRejectedValueOnce(new ApiError("The chart is unavailable.", 503))
      .mockResolvedValue(page);
    render(<PrescriptionsPage />);
    expect(await screen.findByText("The chart is unavailable.")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Try again" }));
    expect(await screen.findByText("Ashwagandha")).toBeInTheDocument();
  });

  it("blocks an incomplete prescription and then saves a valid one", async () => {
    vi.mocked(prescriptionsApi.createPrescription).mockResolvedValue({
      ...draft,
      id: "rx-new",
      items: [{ id: "item-3", name: "Brahmi", dosage: "2 g", frequency: "Twice daily", duration: "10 days", instructions: "" }]
    });
    render(<PrescriptionsPage />);
    await screen.findByText("Ashwagandha");
    fireEvent.click(screen.getByRole("button", { name: "New prescription" }));
    fireEvent.click(screen.getByRole("button", { name: "Save prescription" }));
    expect(screen.getByRole("alert")).toHaveTextContent("Patient is required.");

    fireEvent.change(screen.getByLabelText("Patient"), { target: { value: patientId } });
    fireEvent.change(screen.getByLabelText("Appointment"), { target: { value: "not-a-visit" } });
    fireEvent.click(screen.getByRole("button", { name: "Save prescription" }));
    expect(screen.getByRole("alert")).toHaveTextContent("Enter a valid appointment id.");

    fireEvent.change(screen.getByLabelText("Appointment"), { target: { value: appointmentId } });
    fireEvent.click(screen.getByRole("button", { name: "Save prescription" }));
    expect(screen.getByRole("alert")).toHaveTextContent("Medicine 1: name is required.");
    expect(prescriptionsApi.createPrescription).not.toHaveBeenCalled();

    fillMedicine();
    fireEvent.click(screen.getByRole("button", { name: "Save prescription" }));
    await waitFor(() => {
      expect(prescriptionsApi.createPrescription).toHaveBeenCalledWith({
        patientId,
        appointmentId,
        items: [{ name: "Brahmi", dosage: "2 g", frequency: "Twice daily", duration: "10 days", instructions: null }]
      });
    });
    expect(await screen.findByText("Brahmi")).toBeInTheDocument();
  });

  it("issues a draft, shows an issued chart, and records a revision", async () => {
    vi.mocked(prescriptionsApi.issuePrescription).mockResolvedValue({ ...draft, status: "Issued" });
    vi.mocked(prescriptionsApi.getPrescriptionHistory).mockResolvedValue({
      rootPrescriptionId: issued.rootPrescriptionId,
      versions: [issued],
      revisions: [{
        id: "rev-1",
        previousPrescriptionId: "rx-old",
        revisedPrescriptionId: issued.id,
        revisionNumber: 2,
        revisedByUserId: "doc-1",
        reason: "Dose reduced after nadi pariksha",
        revisedAt: "2026-10-02T09:00:00Z"
      }]
    });
    vi.mocked(prescriptionsApi.revisePrescription).mockResolvedValue({
      ...issued,
      id: "rx-next",
      revisionNumber: 3,
      items: [{ ...issued.items[0], id: "item-4", name: "Ashwagandha" }]
    });

    render(<PrescriptionsPage />);
    await screen.findByText("Triphala");
    fireEvent.click(screen.getByRole("checkbox", { name: "Issued only" }));
    expect(screen.queryByText("Triphala")).not.toBeInTheDocument();
    expect(screen.getByText("Ashwagandha")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("checkbox", { name: "Issued only" }));
    fireEvent.click(screen.getByRole("button", { name: "Issue rx-draft" }));
    expect(await screen.findByRole("button", { name: "View rx-draft" })).toBeInTheDocument();
    expect(prescriptionsApi.issuePrescription).toHaveBeenCalledWith("rx-draft");

    fireEvent.click(screen.getByRole("button", { name: "View rx-issued" }));
    expect(await screen.findByText("Dose reduced after nadi pariksha")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Save revision" }));
    expect(screen.getByRole("alert")).toHaveTextContent("A reason is required.");

    fireEvent.change(screen.getByLabelText("Reason"), { target: { value: "Continue the same herbs" } });
    fireEvent.click(screen.getByRole("button", { name: "Save revision" }));
    await waitFor(() => {
      expect(prescriptionsApi.revisePrescription).toHaveBeenCalledWith("rx-issued", {
        reason: "Continue the same herbs",
        items: [{
          name: "Ashwagandha",
          dosage: "3 g",
          frequency: "Morning",
          duration: "14 days",
          instructions: "With warm water"
        }]
      });
    });
  });
});

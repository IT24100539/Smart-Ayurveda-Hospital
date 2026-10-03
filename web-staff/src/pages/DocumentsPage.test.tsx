import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "../api/client";
import * as documentsApi from "../api/documents";
import type { MedicalDocument } from "../api/documents";
import { DocumentsPage } from "./DocumentsPage";

vi.mock("../api/documents", () => ({
  DOCUMENT_CATEGORIES: ["LabReport", "PrescriptionScan", "DiagnosticScan", "DischargeSummary", "TreatmentPlan", "General"],
  listMedicalDocuments: vi.fn(),
  uploadMedicalDocument: vi.fn(),
  downloadMedicalDocument: vi.fn()
}));

vi.mock("../api/patients", () => ({
  searchPatients: vi.fn()
}));

const patientId = "11111111-1111-1111-1111-111111111111";

const chart: MedicalDocument = {
  id: "doc-1",
  patientId,
  title: "Nadi report",
  category: "DiagnosticScan",
  contentType: "application/pdf",
  fileSizeBytes: 2048,
  uploadedAt: "2026-10-02T08:00:00.000Z",
  fileUrl: "/api/medical-documents/doc-1/file",
  summary: "Morning nadi pariksha"
};

const page = { items: [chart], totalCount: 1, page: 1, pageSize: 20 };

function pdfFile(name = "nadi.pdf"): File {
  return new File(["%PDF-1.4"], name, { type: "application/pdf" });
}

function alertText(): string {
  return screen.getAllByRole("alert").map((node) => node.textContent ?? "").join(" ");
}

describe("DocumentsPage", () => {
  beforeEach(() => {
    vi.mocked(documentsApi.listMedicalDocuments).mockResolvedValue(page);
    vi.mocked(documentsApi.uploadMedicalDocument).mockReset();
    vi.mocked(documentsApi.downloadMedicalDocument).mockReset();
    URL.createObjectURL = vi.fn(() => "blob:chart");
    URL.revokeObjectURL = vi.fn();
    HTMLAnchorElement.prototype.click = vi.fn();
  });

  it("asks for a patient before listing files", () => {
    render(<DocumentsPage />);
    expect(screen.getByText("Choose a patient")).toBeInTheDocument();
    expect(documentsApi.listMedicalDocuments).not.toHaveBeenCalled();
  });

  it("shows a loading state, then the chart files", async () => {
    let resolveList: (value: typeof page) => void = () => {};
    vi.mocked(documentsApi.listMedicalDocuments).mockImplementation(
      () =>
        new Promise((resolve) => {
          resolveList = resolve;
        })
    );

    render(<DocumentsPage />);
    fireEvent.change(screen.getByLabelText("Patient"), { target: { value: patientId } });
    fireEvent.click(screen.getByRole("button", { name: "Search" }));
    expect(screen.getByText("Loading documents…")).toBeInTheDocument();

    resolveList(page);
    expect(await screen.findByText("Nadi report")).toBeInTheDocument();
    expect(screen.getByText("Diagnostic scan")).toBeInTheDocument();
    expect(screen.getByText("2.0 KB")).toBeInTheDocument();
  });

  it("shows an empty state after a search with no files", async () => {
    vi.mocked(documentsApi.listMedicalDocuments).mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });
    render(<DocumentsPage />);
    fireEvent.change(screen.getByLabelText("Patient"), { target: { value: patientId } });
    fireEvent.click(screen.getByRole("button", { name: "Search" }));
    expect(await screen.findByText("No documents found.")).toBeInTheDocument();
  });

  it("shows an error and retries", async () => {
    vi.mocked(documentsApi.listMedicalDocuments)
      .mockRejectedValueOnce(new ApiError("The chart store is unavailable.", 503))
      .mockResolvedValue(page);
    render(<DocumentsPage />);
    fireEvent.change(screen.getByLabelText("Patient"), { target: { value: patientId } });
    fireEvent.click(screen.getByRole("button", { name: "Search" }));
    expect(await screen.findByText("The chart store is unavailable.")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Try again" }));
    expect(await screen.findByText("Nadi report")).toBeInTheDocument();
  });

  it("validates the upload, then saves a chart file and downloads it", async () => {
    vi.mocked(documentsApi.uploadMedicalDocument).mockResolvedValue(chart);
    vi.mocked(documentsApi.downloadMedicalDocument).mockResolvedValue({
      blob: new Blob(["%PDF"]),
      fileName: "document.pdf"
    });

    render(<DocumentsPage />);
    fireEvent.click(screen.getByRole("button", { name: "Search" }));
    expect(await screen.findByRole("alert")).toHaveTextContent("Patient is required.");

    fireEvent.change(screen.getByLabelText("Patient"), { target: { value: "not-a-guid" } });
    fireEvent.click(screen.getByRole("button", { name: "Search" }));
    expect(await screen.findByRole("alert")).toHaveTextContent("Enter a valid patient id.");

    fireEvent.click(screen.getByRole("button", { name: "Upload document" }));
    fireEvent.change(screen.getByLabelText("Patient"), { target: { value: "" } });
    fireEvent.click(screen.getByRole("button", { name: "Save document" }));
    expect(alertText()).toContain("Patient is required.");

    fireEvent.change(screen.getByLabelText("Patient"), { target: { value: patientId } });
    fireEvent.click(screen.getByRole("button", { name: "Save document" }));
    expect(alertText()).toContain("Title is required.");

    fireEvent.change(screen.getByLabelText("Title"), { target: { value: "Nadi report" } });
    fireEvent.change(screen.getByLabelText("File"), {
      target: { files: [new File(["hello"], "notes.txt", { type: "text/plain" })] }
    });
    fireEvent.click(screen.getByRole("button", { name: "Save document" }));
    expect(alertText()).toContain("Document must be a PDF, JPEG, PNG, WebP, or GIF.");

    const oversized = pdfFile("large.pdf");
    Object.defineProperty(oversized, "size", { value: 10 * 1024 * 1024 + 1 });
    fireEvent.change(screen.getByLabelText("File"), { target: { files: [oversized] } });
    fireEvent.click(screen.getByRole("button", { name: "Save document" }));
    expect(alertText()).toContain("no larger than 10 MB");

    const file = pdfFile();
    fireEvent.change(screen.getByLabelText("File"), { target: { files: [file] } });
    fireEvent.click(screen.getByRole("button", { name: "Save document" }));
    await waitFor(() => expect(documentsApi.uploadMedicalDocument).toHaveBeenCalledTimes(1));
    expect(documentsApi.uploadMedicalDocument).toHaveBeenCalledWith({
      patientId,
      title: "Nadi report",
      category: "General",
      summary: null,
      file
    });

    fireEvent.click(await screen.findByRole("button", { name: "Download Nadi report" }));
    await waitFor(() => expect(documentsApi.downloadMedicalDocument).toHaveBeenCalledWith("doc-1"));
    expect(URL.createObjectURL).toHaveBeenCalled();
  });
});

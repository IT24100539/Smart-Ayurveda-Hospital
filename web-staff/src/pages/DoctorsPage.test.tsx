import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "../api/client";
import * as doctorsApi from "../api/doctors";
import type { Doctor } from "../api/doctors";
import { DoctorsPage } from "./DoctorsPage";

vi.mock("../api/doctors", () => ({
  listDoctors: vi.fn(),
  createDoctor: vi.fn(),
  updateDoctor: vi.fn(),
  deactivateDoctor: vi.fn(),
  uploadDoctorPhoto: vi.fn(),
  removeDoctorPhoto: vi.fn()
}));

const meera: Doctor = {
  id: "d1",
  name: "Vd. Meera Iyer",
  specialty: "Kayachikitsa",
  qualifications: "BAMS",
  bio: "Panchakarma physician",
  isActive: true,
  isSample: false,
  hasPhoto: false,
  photoUrl: null,
  rating: 4.5,
  ratingCount: 2
};

const page = {
  items: [meera],
  totalCount: 1,
  page: 1,
  pageSize: 20
};

function fillRequired(name = "Vd. Haritha Bandara") {
  fireEvent.change(screen.getByLabelText("Name"), { target: { value: name } });
  fireEvent.change(screen.getByLabelText("Specialty"), { target: { value: "Panchakarma" } });
  fireEvent.change(screen.getByLabelText("Qualifications"), { target: { value: "BAMS" } });
}

describe("DoctorsPage", () => {
  beforeEach(() => {
    vi.mocked(doctorsApi.listDoctors).mockResolvedValue(page);
    vi.mocked(doctorsApi.createDoctor).mockReset();
    vi.mocked(doctorsApi.updateDoctor).mockReset();
    vi.mocked(doctorsApi.deactivateDoctor).mockReset();
    vi.mocked(doctorsApi.uploadDoctorPhoto).mockReset();
  });

  it("shows a loading state, then the directory", async () => {
    let resolveList: (value: typeof page) => void = () => {};
    vi.mocked(doctorsApi.listDoctors).mockImplementation(
      () =>
        new Promise((resolve) => {
          resolveList = resolve;
        })
    );

    render(<DoctorsPage />);
    expect(screen.getByText("Loading doctors…")).toBeInTheDocument();

    resolveList(page);
    expect(await screen.findByText("Vd. Meera Iyer")).toBeInTheDocument();
    expect(screen.getByText("Kayachikitsa")).toBeInTheDocument();
    expect(screen.getByText("4.5 (2)")).toBeInTheDocument();
  });

  it("shows an empty state", async () => {
    vi.mocked(doctorsApi.listDoctors).mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });
    render(<DoctorsPage />);
    expect(await screen.findByText("No doctors found.")).toBeInTheDocument();
  });

  it("shows an error and retries", async () => {
    vi.mocked(doctorsApi.listDoctors)
      .mockRejectedValueOnce(new ApiError("The directory is unavailable.", 503))
      .mockResolvedValue(page);

    render(<DoctorsPage />);
    expect(await screen.findByText("The directory is unavailable.")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Try again" }));
    expect(await screen.findByText("Vd. Meera Iyer")).toBeInTheDocument();
  });

  it("blocks an incomplete doctor form", async () => {
    render(<DoctorsPage />);
    await screen.findByText("Vd. Meera Iyer");
    fireEvent.click(screen.getByRole("button", { name: "Add doctor" }));
    fireEvent.click(screen.getByRole("button", { name: "Save doctor" }));

    expect(screen.getByText("Name is required.")).toBeInTheDocument();
    expect(screen.getByText("Specialty is required.")).toBeInTheDocument();
    expect(screen.getByText("Qualifications are required.")).toBeInTheDocument();
    expect(doctorsApi.createDoctor).not.toHaveBeenCalled();
  });

  it("rejects a name that is too long and a portrait that is not an image", async () => {
    render(<DoctorsPage />);
    await screen.findByText("Vd. Meera Iyer");
    fireEvent.click(screen.getByRole("button", { name: "Add doctor" }));
    fillRequired("N".repeat(161));
    fireEvent.change(screen.getByLabelText("Bio"), { target: { value: "B".repeat(2001) } });
    const portrait = new File(["notes"], "notes.txt", { type: "text/plain" });
    fireEvent.change(screen.getByLabelText(/portrait/i), { target: { files: [portrait] } });
    fireEvent.click(screen.getByRole("button", { name: "Save doctor" }));

    expect(screen.getByText("Name must be at most 160 characters.")).toBeInTheDocument();
    expect(screen.getByText("Bio must be at most 2000 characters.")).toBeInTheDocument();
    expect(screen.getByText("Photo must be a JPEG, PNG, or WebP image.")).toBeInTheDocument();
    expect(doctorsApi.createDoctor).not.toHaveBeenCalled();
  });

  it("creates a doctor and searches active profiles", async () => {
    const created: Doctor = {
      ...meera,
      id: "d2",
      name: "Vd. Haritha Bandara",
      specialty: "Panchakarma",
      qualifications: "BAMS",
      bio: "Nadi pariksha",
      rating: null,
      ratingCount: null
    };
    vi.mocked(doctorsApi.createDoctor).mockResolvedValue(created);

    render(<DoctorsPage />);
    await screen.findByText("Vd. Meera Iyer");
    fireEvent.click(screen.getByRole("button", { name: "Add doctor" }));
    fillRequired();
    fireEvent.change(screen.getByLabelText("Bio"), { target: { value: "  Nadi pariksha  " } });
    fireEvent.click(screen.getByRole("button", { name: "Save doctor" }));

    expect(await screen.findByText("Vd. Haritha Bandara")).toBeInTheDocument();
    expect(doctorsApi.createDoctor).toHaveBeenCalledWith({
      name: "Vd. Haritha Bandara",
      specialty: "Panchakarma",
      qualifications: "BAMS",
      bio: "Nadi pariksha"
    });

    fireEvent.change(screen.getByLabelText("Search"), { target: { value: " Kayachikitsa " } });
    fireEvent.change(screen.getByLabelText("Status"), { target: { value: "active" } });
    fireEvent.click(screen.getByRole("button", { name: "Search" }));

    await waitFor(() => {
      expect(doctorsApi.listDoctors).toHaveBeenLastCalledWith({
        query: "Kayachikitsa",
        activeOnly: true,
        page: 1,
        pageSize: 20
      });
    });
  });

  it("updates a doctor and deactivates them after confirmation", async () => {
    vi.mocked(doctorsApi.updateDoctor).mockResolvedValue({ ...meera, specialty: "Shalya Tantra" });
    vi.mocked(doctorsApi.deactivateDoctor).mockResolvedValue({ ...meera, specialty: "Shalya Tantra", isActive: false });

    render(<DoctorsPage />);
    await screen.findByText("Vd. Meera Iyer");
    fireEvent.click(screen.getByRole("button", { name: "Edit Vd. Meera Iyer" }));
    fireEvent.change(screen.getByLabelText("Specialty"), { target: { value: "Shalya Tantra" } });
    fireEvent.click(screen.getByRole("button", { name: "Save changes" }));

    await waitFor(() => {
      expect(doctorsApi.updateDoctor).toHaveBeenCalledWith("d1", {
        name: "Vd. Meera Iyer",
        specialty: "Shalya Tantra",
        qualifications: "BAMS",
        bio: "Panchakarma physician"
      });
    });

    fireEvent.click(screen.getByRole("button", { name: "Deactivate Vd. Meera Iyer" }));
    fireEvent.click(screen.getByRole("button", { name: "Confirm deactivation of Vd. Meera Iyer" }));
    expect(await screen.findByText("Inactive")).toBeInTheDocument();
    expect(doctorsApi.deactivateDoctor).toHaveBeenCalledWith("d1");
  });

  it("pages the directory", async () => {
    vi.mocked(doctorsApi.listDoctors).mockResolvedValue({ ...page, totalCount: 21 });
    render(<DoctorsPage />);
    await screen.findByText("Vd. Meera Iyer");
    fireEvent.click(screen.getByRole("button", { name: "Next" }));
    await waitFor(() => {
      expect(doctorsApi.listDoctors).toHaveBeenLastCalledWith(
        expect.objectContaining({ page: 2, pageSize: 20 })
      );
    });
  });
});

import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { PatientsPage } from "../pages/PatientsPage";
import * as api from "../api/patients";

vi.mock("../api/patients", () => ({
  searchPatients: vi.fn(),
  createPatient: vi.fn()
}));

const existing = {
  id: "p1",
  uhid: "UH-1001",
  firstName: "Meera",
  lastName: "Nair",
  dateOfBirth: "1992-04-12",
  gender: "Female" as const,
  phone: "0770000001",
  email: null,
  address: null,
  bloodGroup: null,
  allergies: null,
  prakriti: "Vata",
  vikriti: "None",
  isActive: true,
  createdAt: "2026-09-25T00:00:00Z"
};

describe("PatientsPage", () => {
  beforeEach(() => {
    vi.mocked(api.searchPatients).mockResolvedValue({
      items: [existing],
      totalCount: 1,
      page: 1,
      pageSize: 50
    });
    vi.mocked(api.createPatient).mockResolvedValue({
      ...existing,
      id: "p2",
      uhid: "UH-1002",
      firstName: "Nimal",
      lastName: "Silva",
      phone: "0771111111"
    });
  });

  it("lists patients and creates a new record", async () => {
    render(<PatientsPage />);

    expect(await screen.findByText("Meera Nair")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "New patient" }));
    fireEvent.change(screen.getByLabelText("First name"), { target: { value: "Nimal" } });
    fireEvent.change(screen.getByLabelText("Last name"), { target: { value: "Silva" } });
    fireEvent.change(screen.getByLabelText("Date of birth"), { target: { value: "1990-01-02" } });
    fireEvent.change(screen.getByLabelText("Phone"), { target: { value: "0771111111" } });
    fireEvent.click(screen.getByRole("button", { name: "Save patient" }));

    await waitFor(() => {
      expect(api.createPatient).toHaveBeenCalledWith(
        expect.objectContaining({
          firstName: "Nimal",
          lastName: "Silva",
          phone: "0771111111",
          dateOfBirth: "1990-01-02"
        })
      );
    });
    expect(await screen.findByText("Nimal Silva")).toBeInTheDocument();
  });
});

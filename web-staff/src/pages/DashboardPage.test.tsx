import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { api } from "../api/client";
import { getFeedbackStats } from "../api/feedback";
import { listWorkflows } from "../api/workflows";
import { useAuthStore } from "../store/authStore";
import { DashboardPage } from "./DashboardPage";

vi.mock("../api/client", () => ({
  api: { request: vi.fn() },
  ApiError: class ApiError extends Error {
    status = 500;
    fields = {};
  }
}));

vi.mock("../api/feedback", () => ({
  getFeedbackStats: vi.fn()
}));

vi.mock("../api/workflows", () => ({
  listWorkflows: vi.fn()
}));

describe("DashboardPage", () => {
  beforeEach(() => {
    useAuthStore.setState({
      token: "token",
      user: {
        id: "u1",
        fullName: "Hospital Administrator",
        email: "admin@smartayurveda.local",
        phoneNumber: "000",
        role: "Admin"
      }
    });
    vi.mocked(api.request).mockImplementation(async (path: string) => {
      if (path.startsWith("/patients")) return { items: [], totalCount: 12, page: 1, pageSize: 1 };
      if (path.startsWith("/appointments")) {
        return {
          items: [
            {
              id: "a1",
              patientName: "Meera Nair",
              treatmentName: "Abhyanga",
              requestedTimeSlot: "09:00",
              status: "Pending"
            }
          ],
          totalCount: 1,
          page: 1,
          pageSize: 100
        };
      }
      if (path.startsWith("/wards")) {
        return [{ id: "w1", name: "Panchakarma ward", totalCapacity: 4, occupiedBeds: 1 }];
      }
      if (path.startsWith("/admissions")) return [];
      if (path.startsWith("/complaints")) return [];
      throw new Error(path);
    });
    vi.mocked(getFeedbackStats).mockResolvedValue({
      total: 3,
      averageRating: 4.5,
      pendingModeration: 1,
      negative: 0
    });
    vi.mocked(listWorkflows).mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 50 });
  });

  it("shows today's schedule and hospital figures", async () => {
    render(
      <MemoryRouter>
        <DashboardPage />
      </MemoryRouter>
    );

    expect(await screen.findByText("Meera Nair")).toBeInTheDocument();
    expect(screen.getByText("Abhyanga")).toBeInTheDocument();
    expect(screen.getByText("Panchakarma ward")).toBeInTheDocument();
    expect(screen.getByText("12")).toBeInTheDocument();
  });
});

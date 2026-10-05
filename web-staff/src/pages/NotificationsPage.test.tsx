import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "../api/client";
import * as notificationsApi from "../api/notifications";
import type { StaffNotification } from "../api/notifications";
import { NotificationsPage } from "./NotificationsPage";

vi.mock("../api/notifications", () => ({
  listStaffNotifications: vi.fn()
}));

const notice: StaffNotification = {
  id: "note-1",
  title: "Brahmi ghrita issued",
  message: "Brahmi ghrita was issued after nadi pariksha.",
  type: "PrescriptionIssued",
  isRead: false,
  createdAt: "2026-10-02T09:30:00.000Z"
};

describe("NotificationsPage", () => {
  beforeEach(() => {
    vi.mocked(notificationsApi.listStaffNotifications).mockResolvedValue([notice]);
  });

  it("shows a loading state, then staff notices", async () => {
    let resolveList: (value: StaffNotification[]) => void = () => {};
    vi.mocked(notificationsApi.listStaffNotifications).mockImplementation(
      () =>
        new Promise((resolve) => {
          resolveList = resolve;
        })
    );

    render(<NotificationsPage />);
    expect(screen.getByText("Loading notifications…")).toBeInTheDocument();
    resolveList([notice]);
    expect(await screen.findByText("Brahmi ghrita issued")).toBeInTheDocument();
    expect(screen.getByText("Prescription issued")).toBeInTheDocument();
    expect(screen.getByText("Brahmi ghrita was issued after nadi pariksha.")).toBeInTheDocument();
    expect(screen.getByText("New")).toBeInTheDocument();
  });

  it("shows an empty state", async () => {
    vi.mocked(notificationsApi.listStaffNotifications).mockResolvedValue([]);
    render(<NotificationsPage />);
    expect(await screen.findByText("No notifications.")).toBeInTheDocument();
  });

  it("shows an error and retries", async () => {
    vi.mocked(notificationsApi.listStaffNotifications)
      .mockRejectedValueOnce(new ApiError("The notice board is unavailable.", 503))
      .mockResolvedValue([notice]);
    render(<NotificationsPage />);
    expect(await screen.findByText("The notice board is unavailable.")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Try again" }));
    expect(await screen.findByText("Brahmi ghrita issued")).toBeInTheDocument();
    expect(screen.getByText("Prescription issued")).toBeInTheDocument();
  });
});

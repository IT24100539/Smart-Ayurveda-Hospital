import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { StaffManagementPage } from "./StaffManagementPage";
import { useAuthStore } from "../store/authStore";

describe("StaffManagementPage", () => {
  const currentAdmin = {
    id: "admin-1",
    fullName: "Super Admin",
    email: "admin@hospital.local",
    phoneNumber: "0770000000",
    role: "Admin" as const
  };

  const sampleStaff = [
    {
      id: "admin-1",
      fullName: "Super Admin",
      email: "admin@hospital.local",
      phoneNumber: "0770000000",
      role: "Admin" as const,
      isActive: true,
      mustChangePassword: false,
      createdAt: "2026-09-01T00:00:00Z",
      updatedAt: "2026-09-01T00:00:00Z"
    },
    {
      id: "doc-2",
      fullName: "Dr. Ananya Sharma",
      email: "ananya@hospital.local",
      phoneNumber: "0771112233",
      role: "Doctor" as const,
      isActive: true,
      mustChangePassword: true,
      createdAt: "2026-09-02T00:00:00Z",
      updatedAt: "2026-09-02T00:00:00Z"
    }
  ];

  beforeEach(() => {
    vi.restoreAllMocks();
    useAuthStore.setState({
      token: "valid-mock-token",
      user: currentAdmin
    });
  });

  it("renders staff directory and disables self-modification actions", async () => {
    vi.spyOn(globalThis, "fetch").mockImplementation(async (input) => {
      const url = String(input);
      if (url.includes("/admin/staff")) {
        return new Response(JSON.stringify({ items: sampleStaff, total: 2, page: 1, pageSize: 15 }), {
          status: 200,
          headers: { "Content-Type": "application/json" }
        });
      }
      return new Response(null, { status: 404 });
    });

    render(<StaffManagementPage />);

    expect(await screen.findByText("Staff Account Management")).toBeInTheDocument();
    expect(await screen.findByText("Dr. Ananya Sharma")).toBeInTheDocument();
    expect(await screen.findByText("Super Admin")).toBeInTheDocument();
    expect(await screen.findByText("Must Change Password")).toBeInTheDocument();

    // Verify self actions are disabled for admin-1
    const roleButtons = screen.getAllByRole("button", { name: "Role" });
    const deactivateButtons = screen.getAllByRole("button", { name: "Deactivate" });

    // Super Admin's buttons are disabled
    expect(roleButtons[0]).toBeDisabled();
    expect(deactivateButtons[0]).toBeDisabled();

    // Dr. Ananya's buttons are enabled
    expect(roleButtons[1]).not.toBeDisabled();
    expect(deactivateButtons[1]).not.toBeDisabled();
  });

  it("switches to audit log tab and displays audit events", async () => {
    const mockAudit = [
      {
        id: "audit-1",
        timestamp: "2026-10-01T10:00:00Z",
        actorUserId: "admin-1",
        actorEmail: "admin@hospital.local",
        action: "StaffRoleUpdated",
        targetUserId: "doc-2",
        targetEmail: "ananya@hospital.local",
        details: "Role changed from FrontDeskStaff to Doctor"
      }
    ];

    vi.spyOn(globalThis, "fetch").mockImplementation(async (input) => {
      const url = String(input);
      if (url.includes("/admin/staff/audit-logs")) {
        return new Response(JSON.stringify({ items: mockAudit, total: 1, page: 1, pageSize: 50 }), {
          status: 200,
          headers: { "Content-Type": "application/json" }
        });
      }
      if (url.includes("/admin/staff")) {
        return new Response(JSON.stringify({ items: sampleStaff, total: 2, page: 1, pageSize: 15 }), {
          status: 200,
          headers: { "Content-Type": "application/json" }
        });
      }
      return new Response(null, { status: 404 });
    });

    render(<StaffManagementPage />);
    await screen.findByText("Staff Directory");

    const auditTabBtn = screen.getByRole("button", { name: "Audit Logs" });
    fireEvent.click(auditTabBtn);

    expect(await screen.findByText("Staff Management Audit Trail")).toBeInTheDocument();
    expect(await screen.findByText("StaffRoleUpdated")).toBeInTheDocument();
    expect(await screen.findByText("Role changed from FrontDeskStaff to Doctor")).toBeInTheDocument();
  });
});

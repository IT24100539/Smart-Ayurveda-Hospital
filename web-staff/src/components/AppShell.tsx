import { NavLink, Outlet } from "react-router-dom";
import { ROUTE_ROLES } from "../auth/roles";
import { useAuthStore } from "../store/authStore";

const NAV = [
  { to: "/dashboard", label: "Dashboard", roles: ROUTE_ROLES.dashboard },
  { to: "/patients", label: "Patients", roles: ROUTE_ROLES.patients },
  { to: "/treatments", label: "Treatments", roles: ROUTE_ROLES.treatments },
  { to: "/appointments", label: "Appointments", roles: ROUTE_ROLES.appointments },
  { to: "/wards", label: "Wards", roles: ROUTE_ROLES.wards },
  { to: "/feedback", label: "Feedback", roles: ROUTE_ROLES.feedback },
  { to: "/ai-approvals", label: "AI approvals", roles: ROUTE_ROLES.aiApprovals }
] as const;

export function AppShell() {
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);

  const links = NAV.filter((item) => user && item.roles.includes(user.role));

  return (
    <div className="flex min-h-screen">
      <aside className="flex w-60 shrink-0 flex-col bg-primary-dark px-3 py-6 text-primary-muted">
        <div className="px-3 pb-6">
          <p className="font-serif text-lg font-semibold text-white">Smart Ayurveda</p>
          <p className="mt-1 text-sm text-primary-muted">Staff portal</p>
        </div>
        <nav className="flex flex-1 flex-col gap-1" aria-label="Staff">
          {links.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                [
                  "rounded-lg px-3 py-2 text-sm font-medium",
                  isActive ? "bg-white/15 text-white" : "text-primary-muted hover:bg-white/10 hover:text-white"
                ].join(" ")
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
      </aside>
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex h-14 items-center justify-end gap-4 border-b border-surface-border bg-surface-raised px-6">
          <div className="text-right">
            <p className="text-sm font-medium text-ink">{user?.fullName ?? "Staff"}</p>
            <p className="text-xs text-muted">{user?.role}</p>
          </div>
          <button
            type="button"
            className="rounded-lg border border-surface-border px-3 py-1.5 text-sm font-medium text-primary hover:bg-primary-muted"
            onClick={() => {
              logout();
              window.location.assign("/login");
            }}
          >
            Sign out
          </button>
        </header>
        <main className="flex-1 p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

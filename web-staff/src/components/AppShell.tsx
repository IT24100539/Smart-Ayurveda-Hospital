import { useState, type ReactNode } from "react";
import { NavLink, Outlet } from "react-router-dom";
import { ROUTE_ROLES } from "../auth/roles";
import { useAuthStore } from "../store/authStore";
import { HospitalLogo, LeafImageSlide } from "./HospitalMark";
import { Button } from "./ui";

function Icon({ children }: { children: ReactNode }) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" className="h-4 w-4 shrink-0" fill="none" stroke="currentColor" strokeWidth="1.8">
      {children}
    </svg>
  );
}

const NAV = [
  { to: "/dashboard", label: "Dashboard", roles: ROUTE_ROLES.dashboard, icon: <Icon><path d="M4 10.5 12 4l8 6.5V20a1 1 0 0 1-1 1h-5v-6H10v6H5a1 1 0 0 1-1-1v-9.5Z" /></Icon> },
  { to: "/patients", label: "Patients", roles: ROUTE_ROLES.patients, icon: <Icon><circle cx="9" cy="8" r="3" /><path d="M3.5 19c.6-3 2.8-4.5 5.5-4.5S14.4 16 15 19" /><circle cx="17" cy="9" r="2.2" /><path d="M16.2 14.6c2.2.3 3.8 1.6 4.3 4.4" /></Icon> },
  { to: "/treatments", label: "Treatments", roles: ROUTE_ROLES.treatments, icon: <Icon><path d="M8 4h8v4a4 4 0 0 1-8 0V4Z" /><path d="M8 6H6a2 2 0 0 0 0 4h2M16 6h2a2 2 0 0 1 0 4h-2M12 12v8M9 20h6" /></Icon> },
  { to: "/appointments", label: "Appointments", roles: ROUTE_ROLES.appointments, icon: <Icon><rect x="4" y="5" width="16" height="15" rx="2" /><path d="M8 3v4M16 3v4M4 10h16" /></Icon> },
  { to: "/wards", label: "Wards", roles: ROUTE_ROLES.wards, icon: <Icon><path d="M4 19V9l8-4 8 4v10" /><path d="M9 19v-5h6v5" /></Icon> },
  { to: "/feedback", label: "Feedback", roles: ROUTE_ROLES.feedback, icon: <Icon><path d="M5 6h14v9H8l-3 3V6Z" /></Icon> },
  { to: "/ai-approvals", label: "AI approvals", roles: ROUTE_ROLES.aiApprovals, icon: <Icon><path d="M12 3v3M12 18v3M4.9 6.2l2.1 2.1M17 15.7l2.1 2.1M3 12h3M18 12h3M4.9 17.8 7 15.7M17 8.3l2.1-2.1" /><circle cx="12" cy="12" r="3" /></Icon> }
] as const;

export function AppShell() {
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const [navOpen, setNavOpen] = useState(false);

  const links = NAV.filter((item) => user && item.roles.includes(user.role));

  return (
    <div className="flex min-h-screen bg-surface">
      {navOpen ? (
        <button
          type="button"
          className="fixed inset-0 z-30 bg-ink/40 md:hidden"
          aria-label="Close menu"
          onClick={() => setNavOpen(false)}
        />
      ) : null}
      <aside
        className={[
          "fixed inset-y-0 left-0 z-40 flex h-screen w-80 shrink-0 flex-col overflow-hidden border-r border-[#c6a15a]/30 bg-gradient-to-b from-[#14615f] via-[#0c4a49] to-[#072f2e] px-4 py-5 text-primary-muted shadow-lg",
          "transition-transform md:sticky md:top-0 md:translate-x-0 md:shadow-md",
          navOpen ? "translate-x-0" : "-translate-x-full"
        ].join(" ")}
      >
        <div className="flex items-center gap-3 border-b border-white/10 pb-5">
          <HospitalLogo className="h-12 w-12 shrink-0 shadow-md" />
          <div>
            <p className="font-display text-lg font-semibold leading-tight text-white">Smart Ayurveda</p>
            <p className="mt-0.5 text-xs uppercase tracking-[0.18em] text-[#e7d7a8]">Staff portal</p>
          </div>
        </div>
        <nav id="staff-nav" className="mt-4 flex min-h-0 flex-1 flex-col gap-1 overflow-y-auto" aria-label="Staff">
          {links.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              onClick={() => setNavOpen(false)}
              className={({ isActive }) =>
                [
                  "flex min-h-11 items-center gap-3 rounded-xl px-3 py-2 text-sm font-medium transition",
                  isActive
                    ? "bg-white text-primary-dark shadow-md"
                    : "text-white/90 hover:bg-white/10 hover:text-white"
                ].join(" ")
              }
            >
              {item.icon}
              {item.label}
            </NavLink>
          ))}
        </nav>
        <div className="relative mt-auto overflow-hidden rounded-2xl border border-[#e7d7a8]/25 bg-black/15 pb-1">
          <LeafImageSlide />
        </div>
      </aside>
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-20 flex min-h-16 items-center justify-between gap-3 border-b border-surface-border bg-surface-raised/90 px-4 shadow-sm backdrop-blur-md md:justify-end md:px-6">
          <Button
            variant="secondary"
            className="min-w-11 px-3 md:hidden"
            aria-expanded={navOpen}
            aria-controls="staff-nav"
            onClick={() => setNavOpen((open) => !open)}
          >
            <span className="sr-only">{navOpen ? "Close menu" : "Open menu"}</span>
            <svg viewBox="0 0 24 24" aria-hidden="true" className="h-5 w-5" fill="none" stroke="currentColor" strokeWidth="2">
              {navOpen ? <path d="M6 6l12 12M18 6L6 18" /> : <path d="M4 7h16M4 12h16M4 17h16" />}
            </svg>
          </Button>
          <div className="ml-auto flex items-center gap-3">
            <p className="hidden rounded-full bg-surface px-3 py-1 text-sm text-muted lg:block">
              {new Date().toLocaleDateString(undefined, { weekday: "short", month: "short", day: "numeric" })}
            </p>
            <div className="flex h-10 w-10 items-center justify-center rounded-full bg-primary text-sm font-semibold text-white ring-2 ring-[#c6a15a]/70 ring-offset-2 ring-offset-surface-raised">
              {(user?.fullName ?? "S")
                .split(" ")
                .slice(0, 2)
                .map((part) => part[0])
                .join("")
                .toUpperCase()}
            </div>
            <div className="text-right">
              <p className="max-w-[9rem] truncate text-sm font-medium text-ink sm:max-w-none">{user?.fullName ?? "Staff"}</p>
              <p className="text-xs text-muted">{user?.role}</p>
            </div>
            <Button
              variant="secondary"
              onClick={() => {
                logout();
                window.location.assign("/login");
              }}
            >
              Sign out
            </Button>
          </div>
        </header>
        <main
          className="relative flex-1 bg-surface p-4 md:p-6"
          style={{
            backgroundImage: "url(/images/line-art-bg.svg?v=2)",
            backgroundSize: "320px 320px"
          }}
        >
          <Outlet />
        </main>
      </div>
    </div>
  );
}

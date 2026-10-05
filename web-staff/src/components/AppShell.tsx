import { useEffect, useState, type ReactNode } from "react";
import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { ROUTE_ROLES } from "../auth/roles";
import { useAuthStore } from "../store/authStore";
import { HospitalLogo, LeafImageSlide } from "./HospitalMark";
import { MustChangePasswordModal } from "./MustChangePasswordModal";
import { ProfileMenu } from "./ProfileMenu";
import { ThemeToggle } from "./ThemeToggle";
import { Button } from "./ui";

const SIDEBAR_KEY = "sah-staff-sidebar-collapsed";

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
  { to: "/prescriptions", label: "Prescriptions", roles: ROUTE_ROLES.prescriptions, icon: <Icon><path d="M8 3h6l4 4v14H8V3Z" /><path d="M14 3v5h5M10 12h6M10 16h4" /></Icon> },
  { to: "/documents", label: "Documents", roles: ROUTE_ROLES.documents, icon: <Icon><path d="M7 3h7l4 4v14H7V3Z" /><path d="M14 3v5h4M9 13h6M9 17h4" /></Icon> },
  { to: "/wards", label: "Wards", roles: ROUTE_ROLES.wards, icon: <Icon><path d="M4 19V9l8-4 8 4v10" /><path d="M9 19v-5h6v5" /></Icon> },
  { to: "/doctors", label: "Doctors", roles: ROUTE_ROLES.doctors, icon: <Icon><circle cx="12" cy="8" r="3" /><path d="M6 19c.8-3 3-4.5 6-4.5s5.2 1.5 6 4.5" /><path d="M18 4.5h3M19.5 3v3" /></Icon> },
  { to: "/billing", label: "Billing", roles: ROUTE_ROLES.billing, icon: <Icon><path d="M6 3h12v18l-2-1.5L14 21l-2-1.5L10 21l-2-1.5L6 21V3Z" /><path d="M9 8h6M9 12h6M9 16h3" /></Icon> },
  { to: "/exports", label: "Exports", roles: ROUTE_ROLES.exports, icon: <Icon><path d="M12 4v10M8 10l4 4 4-4" /><path d="M5 18h14" /></Icon> },
  { to: "/feedback", label: "Feedback", roles: ROUTE_ROLES.feedback, icon: <Icon><path d="M5 6h14v9H8l-3 3V6Z" /></Icon> },
  { to: "/notifications", label: "Notifications", roles: ROUTE_ROLES.notifications, icon: <Icon><path d="M6 16V10a6 6 0 1 1 12 0v6l1.5 2h-15L6 16Z" /><path d="M10 19a2 2 0 0 0 4 0" /></Icon> },
  { to: "/ai-approvals", label: "AI approvals", roles: ROUTE_ROLES.aiApprovals, icon: <Icon><path d="M12 3v3M12 18v3M4.9 6.2l2.1 2.1M17 15.7l2.1 2.1M3 12h3M18 12h3M4.9 17.8 7 15.7M17 8.3l2.1-2.1" /><circle cx="12" cy="12" r="3" /></Icon> },
  { to: "/staff-management", label: "Staff management", roles: ROUTE_ROLES.staffManagement, icon: <Icon><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M22 21v-2a4 4 0 0 0-3-3.87" /><path d="M16 3.13a4 4 0 0 1 0 7.75" /></Icon> },
  { to: "/audit-logs", label: "Audit log", roles: ROUTE_ROLES.auditLogs, icon: <Icon><path d="M8 4h8a2 2 0 0 1 2 2v14l-3-2-3 2-3-2-3 2V6a2 2 0 0 1 2-2Z" /><path d="M9 9h6M9 13h6" /></Icon> }
] as const;

function readCollapsed(): boolean {
  try {
    return window.localStorage.getItem(SIDEBAR_KEY) === "1";
  } catch {
    return false;
  }
}

export function AppShell() {
  const user = useAuthStore((state) => state.user);
  const navigate = useNavigate();
  const [navOpen, setNavOpen] = useState(false);
  const [collapsed, setCollapsed] = useState(false);
  const [pageQuery, setPageQuery] = useState("");
  const [bgImageFailed, setBgImageFailed] = useState(false);

  useEffect(() => {
    setCollapsed(readCollapsed());
  }, []);

  const links = NAV.filter((item) => user && item.roles.includes(user.role));
  const needle = pageQuery.trim().toLowerCase();
  const jumpMatches = needle ? links.filter((item) => item.label.toLowerCase().includes(needle)) : [];

  function persistCollapsed(next: boolean) {
    setCollapsed(next);
    try {
      window.localStorage.setItem(SIDEBAR_KEY, next ? "1" : "0");
    } catch {
      // Private mode can block storage; the choice still applies for this session.
    }
  }

  return (
    <div className="flex min-h-screen bg-surface">
      {navOpen ? (
        <button
          type="button"
          className="fixed inset-0 z-30 bg-scrim/50 md:hidden"
          aria-label="Close menu"
          onClick={() => setNavOpen(false)}
        />
      ) : null}
      <aside
        className={[
          "on-dark fixed inset-y-0 left-0 z-40 flex h-screen shrink-0 flex-col overflow-hidden border-r border-hero-accent/30 bg-gradient-to-b from-hero via-hero-deep to-hero-deep py-5 text-hero-muted shadow-lg",
          "transition-[width,transform] md:sticky md:top-0 md:translate-x-0 md:shadow-md",
          collapsed ? "w-80 px-2 md:w-[4.75rem]" : "w-80 px-4",
          navOpen ? "translate-x-0" : "-translate-x-full"
        ].join(" ")}
      >
        <div className={["flex items-center border-b border-white/10 pb-5", collapsed ? "justify-center gap-0 md:px-0" : "gap-3"].join(" ")}>
          <HospitalLogo className="h-12 w-12 shrink-0 shadow-md" />
          <div className={collapsed ? "md:hidden" : ""}>
            <p className="font-display text-lg font-semibold leading-tight text-white">Smart Ayurveda</p>
            <p className="mt-0.5 text-xs uppercase tracking-[0.18em] text-hero-accent">Staff portal</p>
          </div>
        </div>
        <nav id="staff-nav" className="mt-4 flex min-h-0 flex-1 flex-col gap-1 overflow-y-auto" aria-label="Staff">
          {links.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              title={item.label}
              onClick={() => setNavOpen(false)}
              className={({ isActive }) =>
                [
                  "flex min-h-11 items-center rounded-xl py-2 text-sm font-medium transition",
                  collapsed ? "justify-center px-2 md:px-0" : "gap-3 px-3",
                  isActive ? "bg-white text-hero-deep shadow-md" : "text-white/90 hover:bg-white/10 hover:text-white"
                ].join(" ")
              }
            >
              {item.icon}
              <span className={collapsed ? "md:sr-only" : ""}>{item.label}</span>
            </NavLink>
          ))}
        </nav>
        {collapsed ? null : (
          <div className="relative mt-auto overflow-hidden rounded-2xl border border-hero-accent/25 bg-hero-deep/40 pb-1">
            <LeafImageSlide />
          </div>
        )}
      </aside>
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-20 flex min-h-16 items-center gap-3 border-b border-surface-border bg-surface-raised/90 px-4 shadow-sm backdrop-blur-md md:px-6">
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
          <Button
            variant="secondary"
            className="hidden min-w-11 px-3 md:inline-flex"
            aria-pressed={collapsed}
            onClick={() => persistCollapsed(!collapsed)}
          >
            <span className="sr-only">{collapsed ? "Expand sidebar" : "Collapse sidebar"}</span>
            <svg viewBox="0 0 24 24" aria-hidden="true" className="h-5 w-5" fill="none" stroke="currentColor" strokeWidth="1.8">
              <rect x="3" y="4" width="18" height="16" rx="2" />
              <path d="M9 4v16" />
              {collapsed ? <path d="M13 9l4 3-4 3" /> : <path d="M17 9l-4 3 4 3" />}
            </svg>
          </Button>
          <form
            className="relative min-w-0 flex-1"
            role="search"
            onSubmit={(event) => {
              event.preventDefault();
              const first = jumpMatches[0];
              if (!first) return;
              setPageQuery("");
              navigate(first.to);
            }}
          >
            <label htmlFor="staff-page-search" className="sr-only">
              Search pages
            </label>
            <span className="pointer-events-none absolute inset-y-0 left-3 flex items-center text-muted" aria-hidden="true">
              <svg viewBox="0 0 24 24" className="h-4 w-4" fill="none" stroke="currentColor" strokeWidth="1.8">
                <circle cx="11" cy="11" r="6" />
                <path d="m20 20-3.5-3.5" />
              </svg>
            </span>
            <input
              id="staff-page-search"
              className="field pl-9"
              placeholder="Search pages…"
              value={pageQuery}
              autoComplete="off"
              onChange={(event) => setPageQuery(event.target.value)}
            />
            {jumpMatches.length > 0 ? (
              <ul className="absolute z-30 mt-1 w-full overflow-hidden rounded-xl border border-surface-border bg-surface-raised shadow-lg" role="listbox" aria-label="Matching pages">
                {jumpMatches.map((item) => (
                  <li key={item.to} role="option">
                    <button
                      type="button"
                      className="flex w-full items-center gap-2 px-3 py-2 text-left text-sm text-ink hover:bg-primary-muted"
                      onClick={() => {
                        setPageQuery("");
                        navigate(item.to);
                      }}
                    >
                      {item.icon}
                      {item.label}
                    </button>
                  </li>
                ))}
              </ul>
            ) : null}
          </form>
          <p className="hidden rounded-full bg-surface px-3 py-1 text-sm text-muted xl:block">
            {new Date().toLocaleDateString(undefined, { weekday: "short", month: "short", day: "numeric" })}
          </p>
          <ThemeToggle />
          <ProfileMenu />
        </header>
        <main
          className="relative flex-1 bg-surface p-4 md:p-6"
          style={
            bgImageFailed
              ? undefined
              : {
                  backgroundImage: "url(/images/line-art-bg.svg?v=2)",
                  backgroundSize: "320px 320px"
                }
          }
        >
          <img
            src="/images/line-art-bg.svg?v=2"
            alt=""
            className="hidden"
            onError={() => setBgImageFailed(true)}
          />
          <Outlet />
        </main>
      </div>
      <MustChangePasswordModal />
    </div>
  );
}

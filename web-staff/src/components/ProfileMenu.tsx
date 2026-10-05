import { useEffect, useRef, useState } from "react";
import { useAuthStore } from "../store/authStore";
import { Button } from "./ui";

function initials(name: string | undefined): string {
  return (name ?? "S")
    .split(" ")
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0])
    .join("")
    .toUpperCase();
}

export function ProfileMenu() {
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const [open, setOpen] = useState(false);
  const root = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const onPointer = (event: MouseEvent) => {
      if (!root.current?.contains(event.target as Node)) setOpen(false);
    };
    const onKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") setOpen(false);
    };
    document.addEventListener("mousedown", onPointer);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("mousedown", onPointer);
      document.removeEventListener("keydown", onKey);
    };
  }, [open]);

  return (
    <div ref={root} className="relative">
      <button
        type="button"
        aria-expanded={open}
        aria-haspopup="menu"
        aria-controls="staff-profile-menu"
        onClick={() => setOpen((value) => !value)}
        className="flex items-center gap-2 rounded-full py-1 pl-1 pr-2 text-left hover:bg-primary-muted"
      >
        <span className="flex h-10 w-10 items-center justify-center rounded-full bg-primary text-sm font-semibold text-primary-on ring-2 ring-gold/70 ring-offset-2 ring-offset-surface-raised">
          {initials(user?.fullName)}
        </span>
        <span className="hidden min-w-0 sm:block">
          <span className="block max-w-[9rem] truncate text-sm font-medium text-ink lg:max-w-[14rem]">{user?.fullName ?? "Staff"}</span>
          <span className="block text-xs text-muted">{user?.role}</span>
        </span>
        <span className="sr-only">Account menu</span>
      </button>
      {open ? (
        <div
          id="staff-profile-menu"
          role="menu"
          className="absolute right-0 z-30 mt-2 w-56 rounded-xl border border-surface-border bg-surface-raised p-3 shadow-lg"
        >
          <p className="truncate text-sm font-semibold text-ink">{user?.fullName ?? "Staff"}</p>
          <p className="mt-0.5 text-xs text-muted">{user?.role}</p>
          <p className="mt-0.5 truncate text-xs text-muted">{user?.email}</p>
          <Button
            variant="secondary"
            className="mt-3 w-full"
            role="menuitem"
            onClick={() => {
              logout();
              window.location.assign("/login");
            }}
          >
            Sign out
          </Button>
        </div>
      ) : null}
    </div>
  );
}

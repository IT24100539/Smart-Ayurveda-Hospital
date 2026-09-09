import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { clearSession } from "../api/client";

export function Shell() {
  const navigate = useNavigate();
  const name = localStorage.getItem("sah_staff_name") ?? "Staff";
  const role = localStorage.getItem("sah_staff_role") ?? "";

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <h2 className="brand">Smart Ayurveda</h2>
        <p className="muted" style={{ color: "#b7d4c4", marginTop: 0 }}>{name} · {role}</p>
        <NavLink to="/" end className={({ isActive }) => (isActive ? "active" : undefined)}>Dashboard</NavLink>
        <NavLink to="/patients" className={({ isActive }) => (isActive ? "active" : undefined)}>Patients</NavLink>
        <NavLink to="/appointments" className={({ isActive }) => (isActive ? "active" : undefined)}>Appointments</NavLink>
        <button
          className="btn-ghost"
          style={{ marginTop: "auto", color: "#e9f5ee" }}
          onClick={() => {
            clearSession();
            navigate("/login");
          }}
        >
          Sign out
        </button>
      </aside>
      <main className="main">
        <Outlet />
      </main>
    </div>
  );
}

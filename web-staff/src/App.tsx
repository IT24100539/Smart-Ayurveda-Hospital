import { Navigate, Route, Routes } from "react-router-dom";
import { ROUTE_ROLES } from "./auth/roles";
import { AppShell } from "./components/AppShell";
import { ProtectedRoute } from "./components/ProtectedRoute";
import { AiApprovalsPage } from "./pages/AiApprovalsPage";
import { AppointmentsPage } from "./pages/AppointmentsPage";
import { DashboardPage } from "./pages/DashboardPage";
import { FeedbackPage } from "./pages/FeedbackPage";
import { LoginPage } from "./pages/LoginPage";
import { PatientsPage } from "./pages/PatientsPage";
import { TreatmentsPage } from "./pages/TreatmentsPage";
import { WardsPage } from "./pages/WardsPage";

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        element={
          <ProtectedRoute>
            <AppShell />
          </ProtectedRoute>
        }
      >
        <Route
          path="/dashboard"
          element={
            <ProtectedRoute roles={ROUTE_ROLES.dashboard}>
              <DashboardPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/patients"
          element={
            <ProtectedRoute roles={ROUTE_ROLES.patients}>
              <PatientsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/treatments"
          element={
            <ProtectedRoute roles={ROUTE_ROLES.treatments}>
              <TreatmentsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/appointments"
          element={
            <ProtectedRoute roles={ROUTE_ROLES.appointments}>
              <AppointmentsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/wards"
          element={
            <ProtectedRoute roles={ROUTE_ROLES.wards}>
              <WardsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/feedback"
          element={
            <ProtectedRoute roles={ROUTE_ROLES.feedback}>
              <FeedbackPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/ai-approvals"
          element={
            <ProtectedRoute roles={ROUTE_ROLES.aiApprovals}>
              <AiApprovalsPage />
            </ProtectedRoute>
          }
        />
      </Route>
      <Route path="/" element={<Navigate to="/dashboard" replace />} />
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  );
}

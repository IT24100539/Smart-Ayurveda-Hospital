import { lazy, Suspense } from "react";
import { Navigate, Route, Routes } from "react-router-dom";
import { ROUTE_ROLES, isStaffRole } from "./auth/roles";
import { AppShell } from "./components/AppShell";
import { ProtectedRoute } from "./components/ProtectedRoute";
import { hasValidJwt, useAuthStore } from "./store/authStore";
import { AiApprovalsPage } from "./pages/AiApprovalsPage";
import { AppointmentsPage } from "./pages/AppointmentsPage";
import { AuditLogsPage } from "./pages/AuditLogsPage";
import { DashboardPage } from "./pages/DashboardPage";
import { DoctorsPage } from "./pages/DoctorsPage";
import { FeedbackPage } from "./pages/FeedbackPage";
import { LoginPage } from "./pages/LoginPage";
import { BillingPage } from "./pages/BillingPage";
import { DocumentsPage } from "./pages/DocumentsPage";
import { ExportsPage } from "./pages/ExportsPage";
import { NotificationsPage } from "./pages/NotificationsPage";
import { PatientsPage } from "./pages/PatientsPage";
import { PrescriptionsPage } from "./pages/PrescriptionsPage";
import { TreatmentsPage } from "./pages/TreatmentsPage";
import { WardsPage } from "./pages/WardsPage";
import { StaffManagementPage } from "./pages/StaffManagementPage";

const DevPreview = import.meta.env.DEV
  ? lazy(() => import("./pages/UiPreviewPage").then((module) => ({ default: module.UiPreviewPage })))
  : null;

/**
 * Directs root and wildcard requests:
 * - If authenticated with a valid staff role -> /dashboard
 * - If unauthenticated or role is Patient -> /login
 */
function RootRedirect() {
  const token = useAuthStore((state) => state.token);
  const user = useAuthStore((state) => state.user);

  if (hasValidJwt(token) && user && isStaffRole(user.role)) {
    return <Navigate to="/dashboard" replace />;
  }

  return <Navigate to="/login" replace />;
}

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
        <Route
          path="/staff-management"
          element={
            <ProtectedRoute roles={ROUTE_ROLES.staffManagement}>
              <StaffManagementPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/doctors"
          element={
            <ProtectedRoute roles={ROUTE_ROLES.doctors}>
              <DoctorsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/audit-logs"
          element={
            <ProtectedRoute roles={ROUTE_ROLES.auditLogs}>
              <AuditLogsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/prescriptions"
          element={
            <ProtectedRoute roles={ROUTE_ROLES.prescriptions}>
              <PrescriptionsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/billing"
          element={
            <ProtectedRoute roles={ROUTE_ROLES.billing}>
              <BillingPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/documents"
          element={
            <ProtectedRoute roles={ROUTE_ROLES.documents}>
              <DocumentsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/notifications"
          element={
            <ProtectedRoute roles={ROUTE_ROLES.notifications}>
              <NotificationsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/exports"
          element={
            <ProtectedRoute roles={ROUTE_ROLES.exports}>
              <ExportsPage />
            </ProtectedRoute>
          }
        />
      </Route>
      {import.meta.env.DEV && DevPreview ? (
        <Route
          path="/dev/ui"
          element={
            <Suspense fallback={null}>
              <DevPreview />
            </Suspense>
          }
        />
      ) : null}
      <Route path="/" element={<RootRedirect />} />
      <Route path="*" element={<RootRedirect />} />
    </Routes>
  );
}

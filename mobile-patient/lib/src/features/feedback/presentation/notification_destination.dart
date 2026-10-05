import '../../../router/app_routes.dart';
import '../domain/communication_models.dart';

/// Screen opened when a patient taps a notice.
///
/// The inbox payload has a type and no related record id, so each type opens
/// the list that contains that record. A general notice stays on the inbox.
String? notificationDestination(NotificationKind kind) {
  return switch (kind) {
    NotificationKind.reply => AppRoutes.myFeedback,
    NotificationKind.statusChange ||
    NotificationKind.escalation => AppRoutes.complaints,
    NotificationKind.appointmentApproved ||
    NotificationKind.appointmentRejected ||
    NotificationKind.appointmentRescheduled ||
    NotificationKind.appointmentCancelled => AppRoutes.appointments,
    NotificationKind.prescriptionIssued => AppRoutes.healthHubTab('prescriptions'),
    NotificationKind.invoiceIssued => AppRoutes.healthHubTab('invoices'),
    NotificationKind.general => null,
  };
}

import '../../../core/network/api_exception.dart';
import '../../../l10n/app_localizations.dart';
import '../domain/communication_models.dart';

bool isUnlinkedPatientError(Object error) {
  const marker = 'No patient record is linked';
  if (error is ApiException) {
    return error.message?.contains(marker) ?? false;
  }
  return error.toString().contains(marker);
}

String feedbackErrorText(Object error, AppLocalizations l10n) {
  if (error is ApiException) {
    if (error.isNetworkError) return l10n.networkErrorMessage;
    return error.message ?? l10n.genericErrorMessage;
  }
  return l10n.genericErrorMessage;
}

String feedbackStatusLabel(AppLocalizations l10n, String status) {
  return switch (status) {
    'Visible' => l10n.feedbackStatusVisible,
    'Hidden' => l10n.feedbackStatusHidden,
    'PendingModeration' => l10n.feedbackStatusPending,
    'Withdrawn' => l10n.feedbackStatusWithdrawn,
    _ => status,
  };
}

String complaintStatusLabel(AppLocalizations l10n, ComplaintStatus status) {
  return switch (status) {
    ComplaintStatus.open => l10n.statusOpen,
    ComplaintStatus.inProgress => l10n.statusInProgress,
    ComplaintStatus.escalated => l10n.statusEscalated,
    ComplaintStatus.resolved => l10n.statusResolved,
  };
}

String notificationKindLabel(AppLocalizations l10n, NotificationKind kind) {
  return switch (kind) {
    NotificationKind.reply => l10n.notificationReply,
    NotificationKind.statusChange => l10n.notificationStatus,
    NotificationKind.escalation => l10n.notificationEscalated,
    NotificationKind.general => l10n.notificationGeneral,
    NotificationKind.appointmentApproved => l10n.notificationAppointmentApproved,
    NotificationKind.appointmentRejected => l10n.notificationAppointmentRejected,
    NotificationKind.appointmentRescheduled =>
      l10n.notificationAppointmentRescheduled,
    NotificationKind.appointmentCancelled => l10n.notificationAppointmentCancelled,
    NotificationKind.prescriptionIssued => l10n.notificationPrescriptionIssued,
    NotificationKind.invoiceIssued => l10n.notificationInvoiceIssued,
  };
}

String formatWhen(DateTime value) {
  final local = value.toLocal();
  String two(int n) => n.toString().padLeft(2, '0');
  return '${local.year}-${two(local.month)}-${two(local.day)} ${two(local.hour)}:${two(local.minute)}';
}

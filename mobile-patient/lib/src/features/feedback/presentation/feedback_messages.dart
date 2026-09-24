import '../../../core/network/api_exception.dart';
import '../../../l10n/app_localizations.dart';
import '../domain/communication_models.dart';

String feedbackErrorText(Object error, AppLocalizations l10n) {
  if (error is ApiException) {
    if (error.isNetworkError) return l10n.networkErrorMessage;
    return error.message ?? l10n.genericErrorMessage;
  }
  return l10n.genericErrorMessage;
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
  };
}

String formatWhen(DateTime value) {
  final local = value.toLocal();
  String two(int n) => n.toString().padLeft(2, '0');
  return '${local.year}-${two(local.month)}-${two(local.day)} ${two(local.hour)}:${two(local.minute)}';
}

import '../../../l10n/app_localizations.dart';

String prescriptionStatusLabel(AppLocalizations l10n, String status) {
  return switch (status) {
    'Issued' => l10n.prescriptionStatusIssued,
    'Superseded' => l10n.prescriptionStatusSuperseded,
    'Cancelled' => l10n.prescriptionStatusCancelled,
    'Draft' => l10n.prescriptionStatusDraft,
    _ => status,
  };
}

String invoiceStatusLabel(AppLocalizations l10n, String status) {
  return switch (status) {
    'Issued' => l10n.invoiceStatusIssued,
    'Paid' => l10n.invoiceStatusPaid,
    'Cancelled' => l10n.invoiceStatusCancelled,
    'Draft' => l10n.invoiceStatusDraft,
    _ => status,
  };
}

String paymentMethodLabel(AppLocalizations l10n, String method) {
  return switch (method) {
    'Cash' => l10n.paymentMethodCash,
    'Card' => l10n.paymentMethodCard,
    'BankTransfer' => l10n.paymentMethodBankTransfer,
    _ => method,
  };
}

String documentCategoryLabel(AppLocalizations l10n, String category) {
  return switch (category) {
    'LabReport' => l10n.documentCategoryLabReport,
    'PrescriptionScan' => l10n.documentCategoryPrescriptionScan,
    'DiagnosticScan' => l10n.documentCategoryDiagnosticScan,
    'DischargeSummary' => l10n.documentCategoryDischargeSummary,
    'TreatmentPlan' => l10n.documentCategoryTreatmentPlan,
    'General' => l10n.documentCategoryGeneral,
    _ => category,
  };
}

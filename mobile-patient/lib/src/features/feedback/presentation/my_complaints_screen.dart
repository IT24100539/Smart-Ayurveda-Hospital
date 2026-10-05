import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/clinic_widgets.dart';
import '../../../shared/widgets/empty_state.dart';
import '../../../shared/widgets/page_layout.dart';
import '../../../shared/widgets/skeleton.dart';
import '../../../router/app_routes.dart';
import '../../../theme/app_theme.dart';
import '../application/communication_providers.dart';
import '../domain/communication_models.dart';
import 'feedback_keys.dart';
import 'feedback_messages.dart';

class MyComplaintsScreen extends ConsumerWidget {
  const MyComplaintsScreen({this.embedded = false, super.key});

  final bool embedded;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final complaints = ref.watch(myComplaintsProvider);

    return Scaffold(
      appBar: embedded ? null : AppBar(title: Text(l10n.complaintsTitle)),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => context.push(AppRoutes.submitComplaint),
        icon: const Icon(Icons.add),
        label: Text(l10n.newComplaint),
      ),
      body: complaints.when(
        loading: () => const SkeletonList(
          withBanner: false,
          lines: 2,
          listKey: FeedbackKeys.complaintsSkeleton,
        ),
        error: (error, _) => ErrorState(
          message: feedbackErrorText(error, l10n),
          actionLabel: l10n.retry,
          actionKey: FeedbackKeys.complaintsRetry,
          onAction: () => ref.invalidate(myComplaintsProvider),
        ),
        data: (items) {
          if (items.isEmpty) {
            return EmptyState(
              message: l10n.complaintsEmpty,
              imageAsset: 'assets/images/empty-feedback.png',
            );
          }
          return RefreshIndicator(
            onRefresh: () => ref.refresh(myComplaintsProvider.future),
            child: PageListView.builder(
              // Leaves room under the last card for the floating button.
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 96),
              itemCount: items.length,
              itemBuilder: (context, index) =>
                  _ComplaintCard(complaint: items[index]),
            ),
          );
        },
      ),
    );
  }
}

class _ComplaintCard extends StatelessWidget {
  const _ComplaintCard({required this.complaint});

  final PatientComplaint complaint;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final status = complaintStatusLabel(l10n, complaint.status);

    return ClinicCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Text(
                  complaint.subject,
                  style: theme.textTheme.titleSmall,
                ),
              ),
              const SizedBox(width: 8),
              _StatusChip(status: complaint.status, label: status),
            ],
          ),
          const SizedBox(height: 8),
          Text(complaint.description, style: const TextStyle(height: 1.4)),
          const SizedBox(height: 10),
          Text(
            '${(complaint.priority == ComplaintPriority.high ? l10n.priorityHigh : l10n.priorityNormal).toUpperCase()} · ${formatWhen(complaint.createdAt)}',
            style: AyurvedaType.eyebrow(context),
          ),
          if (complaint.escalatedAt != null)
            Padding(
              padding: const EdgeInsets.only(top: 8),
              child: ErrorLine(
                message:
                    '${l10n.escalatedOn} · ${formatWhen(complaint.escalatedAt!)}',
              ),
            ),
        ],
      ),
    );
  }
}

/// Status pill with an icon, from the shared status tokens (readable in dark mode).
class _StatusChip extends StatelessWidget {
  const _StatusChip({required this.status, required this.label});

  final ComplaintStatus status;
  final String label;

  @override
  Widget build(BuildContext context) {
    final brand = AyurvedaThemeExtension.of(context);
    final scheme = Theme.of(context).colorScheme;
    final (background, foreground, icon) = switch (status) {
      ComplaintStatus.open => (
        brand.neutralBackground,
        brand.neutralForeground,
        Icons.radio_button_unchecked,
      ),
      ComplaintStatus.inProgress => (
        brand.pendingBackground,
        brand.pendingForeground,
        Icons.schedule,
      ),
      ComplaintStatus.escalated => (
        scheme.errorContainer,
        scheme.error,
        Icons.priority_high,
      ),
      ComplaintStatus.resolved => (
        brand.approvedBackground,
        brand.approvedForeground,
        Icons.check,
      ),
    };
    return PillChip(
      label: label,
      icon: icon,
      background: background,
      foreground: foreground,
    );
  }
}

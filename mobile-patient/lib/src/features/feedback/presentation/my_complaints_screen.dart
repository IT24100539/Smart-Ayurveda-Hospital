import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../l10n/app_localizations.dart';
import '../../../router/app_routes.dart';
import '../../../theme/app_theme.dart';
import '../application/communication_providers.dart';
import '../domain/communication_models.dart';
import 'feedback_messages.dart';

class MyComplaintsScreen extends ConsumerWidget {
  const MyComplaintsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final complaints = ref.watch(myComplaintsProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.complaintsTitle)),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => context.push(AppRoutes.submitComplaint),
        icon: const Icon(Icons.add),
        label: Text(l10n.newComplaint),
      ),
      body: complaints.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => _ComplaintsMessage(
          message: feedbackErrorText(error, l10n),
          actionLabel: l10n.retry,
          onAction: () => ref.invalidate(myComplaintsProvider),
        ),
        data: (items) {
          if (items.isEmpty) {
            return _ComplaintsMessage(message: l10n.complaintsEmpty);
          }
          return RefreshIndicator(
            onRefresh: () => ref.refresh(myComplaintsProvider.future),
            child: ListView.separated(
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 96),
              itemCount: items.length,
              separatorBuilder: (_, _) => const SizedBox(height: 12),
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

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    complaint.subject,
                    style: theme.textTheme.titleSmall?.copyWith(
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
                _StatusChip(status: complaint.status, label: status),
              ],
            ),
            const SizedBox(height: 8),
            Text(complaint.description),
            const SizedBox(height: 10),
            Text(
              '${complaint.priority == ComplaintPriority.high ? l10n.priorityHigh : l10n.priorityNormal} · ${formatWhen(complaint.createdAt)}',
              style: theme.textTheme.labelSmall?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
            if (complaint.escalatedAt != null)
              Padding(
                padding: const EdgeInsets.only(top: 4),
                child: Text(
                  '${l10n.escalatedOn} · ${formatWhen(complaint.escalatedAt!)}',
                  style: theme.textTheme.labelSmall?.copyWith(
                    color: AyurvedaColors.danger,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

class _StatusChip extends StatelessWidget {
  const _StatusChip({required this.status, required this.label});

  final ComplaintStatus status;
  final String label;

  @override
  Widget build(BuildContext context) {
    final color = switch (status) {
      ComplaintStatus.open => AyurvedaColors.sage,
      ComplaintStatus.inProgress => AyurvedaColors.gold,
      ComplaintStatus.escalated => AyurvedaColors.danger,
      ComplaintStatus.resolved => AyurvedaColors.forest,
    };
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.15),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: color,
          fontSize: 12,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}

class _ComplaintsMessage extends StatelessWidget {
  const _ComplaintsMessage({
    required this.message,
    this.actionLabel,
    this.onAction,
  });

  final String message;
  final String? actionLabel;
  final VoidCallback? onAction;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Text(message, textAlign: TextAlign.center),
            if (actionLabel != null && onAction != null) ...[
              const SizedBox(height: 16),
              OutlinedButton(onPressed: onAction, child: Text(actionLabel!)),
            ],
          ],
        ),
      ),
    );
  }
}

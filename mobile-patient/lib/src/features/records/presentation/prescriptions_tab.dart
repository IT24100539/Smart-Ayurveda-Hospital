import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/clinic_widgets.dart';
import '../application/records_provider.dart';
import '../domain/patient_records.dart';
import 'record_labels.dart';
import 'records_panel.dart';

abstract final class PrescriptionsTabKeys {
  static const retry = ValueKey('prescriptions-retry');
}

class PrescriptionsTab extends ConsumerWidget {
  const PrescriptionsTab({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final prescriptions = ref.watch(prescriptionsProvider);

    return RefreshIndicator(
      onRefresh: () async {
        ref.invalidate(prescriptionsProvider);
        await ref.read(prescriptionsProvider.future);
      },
      child: RecordsPanel(
        state: prescriptions,
        errorMessage: l10n.prescriptionsLoadError,
        emptyMessage: l10n.prescriptionsEmpty,
        retryKey: PrescriptionsTabKeys.retry,
        onRetry: () => ref.invalidate(prescriptionsProvider),
        itemBuilder: (prescription) => _PrescriptionCard(prescription: prescription),
      ),
    );
  }
}

class _PrescriptionCard extends StatelessWidget {
  const _PrescriptionCard({required this.prescription});

  final Prescription prescription;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final issued = prescription.issuedAt;

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
                    prescription.doctorName,
                    style: theme.textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
                _StatusChip(
                  label: prescriptionStatusLabel(l10n, prescription.status),
                ),
              ],
            ),
            if (issued != null) ...[
              const SizedBox(height: 6),
              Text(
                l10n.prescriptionIssuedOn(
                  DateFormat.yMMMd().format(issued.toLocal()),
                ),
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              ),
            ],
            if (prescription.revisionNumber > 1) ...[
              const SizedBox(height: 4),
              Text(
                l10n.prescriptionRevision(prescription.revisionNumber),
                style: theme.textTheme.bodySmall,
              ),
            ],
            const SizedBox(height: 12),
            for (final item in prescription.items) ...[
              Text(
                item.name,
                style: theme.textTheme.titleSmall?.copyWith(
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: 2),
              Text('${item.dosage} · ${item.frequency} · ${item.duration}'),
              if (item.instructions.isNotEmpty) ...[
                const SizedBox(height: 2),
                Text(
                  item.instructions,
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: theme.colorScheme.onSurfaceVariant,
                  ),
                ),
              ],
              const SizedBox(height: 10),
            ],
          ],
        ),
      ),
    );
  }
}

class _StatusChip extends StatelessWidget {
  const _StatusChip({required this.label});

  final String label;

  @override
  Widget build(BuildContext context) {
    return PillChip(label: label);
  }
}

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/empty_state.dart';
import '../../../shared/widgets/responsive_columns.dart';
import '../../../shared/widgets/unlinked_patient_card.dart';
import '../../feedback/presentation/feedback_messages.dart';

class RecordsPanel<T> extends StatelessWidget {
  const RecordsPanel({
    required this.state,
    required this.errorMessage,
    required this.emptyMessage,
    required this.onRetry,
    required this.itemBuilder,
    this.retryKey,
    this.emptyIcon = Icons.folder_open_outlined,
    super.key,
  });

  final AsyncValue<List<T>> state;
  final String errorMessage;
  final String emptyMessage;
  final VoidCallback onRetry;
  final Widget Function(T item) itemBuilder;
  final Key? retryKey;
  final IconData emptyIcon;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return state.when(
      loading: () => ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        children: const [
          SizedBox(height: 80),
          Center(child: CircularProgressIndicator()),
        ],
      ),
      error: (error, stackTrace) {
        if (isUnlinkedPatientError(error)) {
          return ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.all(20),
            children: [UnlinkedPatientCard(onRetry: onRetry)],
          );
        }
        return ErrorState(
          message: errorMessage,
          actionLabel: l10n.retry,
          actionKey: retryKey,
          onAction: onRetry,
          scrollable: true,
        );
      },
      data: (items) {
        if (items.isEmpty) {
          return EmptyState(
            message: emptyMessage,
            icon: emptyIcon,
            scrollable: true,
          );
        }
        return ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.fromLTRB(20, 12, 20, 24),
          children: [
            ResponsiveColumns(
              children: [for (final item in items) itemBuilder(item)],
            ),
          ],
        );
      },
    );
  }
}

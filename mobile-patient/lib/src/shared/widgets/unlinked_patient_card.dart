import 'package:flutter/material.dart';
import '../../l10n/app_localizations.dart';

/// A contrast-safe error card rendered when a patient user has no linked clinical record.
class UnlinkedPatientCard extends StatelessWidget {
  const UnlinkedPatientCard({
    this.onRetry,
    this.onHelp,
    this.customMessage,
    super.key,
  });

  final VoidCallback? onRetry;
  final VoidCallback? onHelp;
  final String? customMessage;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    final containerColor = isDark
        ? theme.colorScheme.errorContainer.withValues(alpha: 0.35)
        : theme.colorScheme.errorContainer;
    final textColor = theme.colorScheme.onErrorContainer;

    return Semantics(
      container: true,
      label: l10n.noPatientRecordLinked,
      child: Card(
        elevation: 0,
        color: containerColor,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
          side: BorderSide(
            color: theme.colorScheme.error.withValues(alpha: 0.4),
            width: 1,
          ),
        ),
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                crossAxisAlignment: CrossAxisAlignment.center,
                children: [
                  Container(
                    padding: const EdgeInsets.all(10),
                    decoration: BoxDecoration(
                      color: theme.colorScheme.error.withValues(alpha: 0.15),
                      shape: BoxShape.circle,
                    ),
                    child: Icon(
                      Icons.no_accounts_rounded,
                      color: theme.colorScheme.error,
                      size: 28,
                    ),
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Text(
                      l10n.noPatientRecordLinked,
                      style: theme.textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.bold,
                        color: textColor,
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              Text(
                customMessage ?? l10n.noPatientRecordLinkedHelp,
                style: theme.textTheme.bodyMedium?.copyWith(
                  color: textColor.withValues(alpha: 0.9),
                  height: 1.4,
                ),
              ),
              if (onRetry != null || onHelp != null) ...[
                const SizedBox(height: 16),
                Wrap(
                  alignment: WrapAlignment.end,
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    if (onHelp != null)
                      TextButton.icon(
                        onPressed: onHelp,
                        style: TextButton.styleFrom(
                          foregroundColor: textColor,
                          minimumSize: const Size(48, 48),
                        ),
                        icon: const Icon(Icons.support_agent_rounded, size: 18),
                        label: Text(l10n.unlinkedRecordHelpAction),
                      ),
                    if (onRetry != null)
                      OutlinedButton.icon(
                        onPressed: onRetry,
                        style: OutlinedButton.styleFrom(
                          foregroundColor: textColor,
                          side: BorderSide(
                            color: theme.colorScheme.error.withValues(
                              alpha: 0.5,
                            ),
                          ),
                          minimumSize: const Size(48, 48),
                        ),
                        icon: const Icon(Icons.refresh_rounded, size: 18),
                        label: Text(l10n.unlinkedRecordRetry),
                      ),
                  ],
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

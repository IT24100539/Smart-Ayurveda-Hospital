import 'package:flutter/material.dart';

import '../../theme/app_theme.dart';

/// Shared empty and error panels. Features should not build their own.
class EmptyState extends StatelessWidget {
  const EmptyState({
    required this.message,
    this.detail,
    this.icon = Icons.spa_outlined,
    this.actionLabel,
    this.onAction,
    this.useFilledAction = false,
    this.actionKey,
    this.scrollable = false,
    super.key,
  });

  final String message;
  final String? detail;
  final IconData icon;
  final String? actionLabel;
  final VoidCallback? onAction;
  final bool useFilledAction;
  final Key? actionKey;

  /// Keeps pull-to-refresh working when this panel is the only body child.
  final bool scrollable;

  @override
  Widget build(BuildContext context) {
    return _maybeScroll(
      scrollable,
      _StatusBody(
        icon: icon,
        iconColor: AyurvedaColors.sage,
        message: message,
        detail: detail,
        actionLabel: actionLabel,
        onAction: onAction,
        useFilledAction: useFilledAction,
        actionKey: actionKey,
      ),
    );
  }
}

class ErrorState extends StatelessWidget {
  const ErrorState({
    required this.message,
    this.actionLabel,
    this.onAction,
    this.scrollable = false,
    super.key,
  });

  final String message;
  final String? actionLabel;
  final VoidCallback? onAction;
  final bool scrollable;

  @override
  Widget build(BuildContext context) {
    return _maybeScroll(
      scrollable,
      _StatusBody(
        icon: Icons.cloud_off_outlined,
        iconColor: Theme.of(context).colorScheme.error,
        message: message,
        actionLabel: actionLabel,
        onAction: onAction,
        useFilledAction: false,
      ),
    );
  }
}

Widget _maybeScroll(bool scrollable, Widget child) {
  if (!scrollable) return child;
  return LayoutBuilder(
    builder: (context, constraints) {
      return ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        children: [SizedBox(height: constraints.maxHeight, child: child)],
      );
    },
  );
}

class _StatusBody extends StatelessWidget {
  const _StatusBody({
    required this.icon,
    required this.iconColor,
    required this.message,
    this.detail,
    this.actionLabel,
    this.onAction,
    required this.useFilledAction,
    this.actionKey,
  });

  final IconData icon;
  final Color iconColor;
  final String message;
  final String? detail;
  final String? actionLabel;
  final VoidCallback? onAction;
  final bool useFilledAction;
  final Key? actionKey;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon, size: 48, color: iconColor),
            const SizedBox(height: 16),
            Text(message, textAlign: TextAlign.center),
            if (detail != null) ...[
              const SizedBox(height: 8),
              Text(
                detail!,
                textAlign: TextAlign.center,
                style: theme.textTheme.bodyMedium?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              ),
            ],
            if (actionLabel != null && onAction != null) ...[
              const SizedBox(height: 16),
              if (useFilledAction)
                FilledButton(
                  key: actionKey,
                  onPressed: onAction,
                  child: Text(actionLabel!),
                )
              else
                OutlinedButton(
                  key: actionKey,
                  onPressed: onAction,
                  child: Text(actionLabel!),
                ),
            ],
          ],
        ),
      ),
    );
  }
}

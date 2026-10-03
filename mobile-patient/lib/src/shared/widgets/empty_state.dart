import 'package:flutter/material.dart';

import '../../theme/app_theme.dart';
import 'clinic_widgets.dart';

/// Shared empty and error panels. Features should not build their own.
class EmptyState extends StatelessWidget {
  const EmptyState({
    required this.message,
    this.detail,
    this.icon = Icons.spa_outlined,
    this.imageAsset,
    this.actionLabel,
    this.onAction,
    this.useFilledAction = false,
    this.actionKey,
    this.scrollable = false,
    this.compact = false,
    super.key,
  });

  final String message;
  final String? detail;
  final IconData icon;
  final String? imageAsset;
  final String? actionLabel;
  final VoidCallback? onAction;
  final bool useFilledAction;
  final Key? actionKey;

  /// Keeps pull-to-refresh working when this panel is the only body child.
  final bool scrollable;

  /// Renders inside a card, sized to its content, for use between other sections.
  final bool compact;

  @override
  Widget build(BuildContext context) {
    return _maybeScroll(
      scrollable,
      _StatusBody(
        icon: icon,
        tone: _StatusTone.calm,
        imageAsset: imageAsset,
        message: message,
        detail: detail,
        actionLabel: actionLabel,
        onAction: onAction,
        useFilledAction: useFilledAction,
        actionKey: actionKey,
        compact: compact,
      ),
    );
  }
}

class ErrorState extends StatelessWidget {
  const ErrorState({
    required this.message,
    this.actionLabel,
    this.onAction,
    this.actionKey,
    this.scrollable = false,
    this.compact = false,
    super.key,
  });

  final String message;
  final String? actionLabel;
  final VoidCallback? onAction;
  final Key? actionKey;
  final bool scrollable;

  /// Renders inside a card, sized to its content, for use between other sections.
  final bool compact;

  @override
  Widget build(BuildContext context) {
    return _maybeScroll(
      scrollable,
      _StatusBody(
        icon: Icons.cloud_off_outlined,
        tone: _StatusTone.error,
        message: message,
        actionLabel: actionLabel,
        onAction: onAction,
        useFilledAction: false,
        actionKey: actionKey,
        compact: compact,
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

enum _StatusTone { calm, error }

class _StatusBody extends StatelessWidget {
  const _StatusBody({
    required this.icon,
    required this.tone,
    this.imageAsset,
    required this.message,
    this.detail,
    this.actionLabel,
    this.onAction,
    required this.useFilledAction,
    this.actionKey,
    required this.compact,
  });

  final IconData icon;
  final _StatusTone tone;
  final String? imageAsset;
  final String message;
  final String? detail;
  final String? actionLabel;
  final VoidCallback? onAction;
  final bool useFilledAction;
  final Key? actionKey;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final brand = AyurvedaThemeExtension.of(context);
    final isError = tone == _StatusTone.error;
    // Error tint comes from the color scheme, so it is lighter in dark mode.
    final iconColor = isError ? theme.colorScheme.error : brand.teal;
    final badgeColor = isError
        ? theme.colorScheme.errorContainer
        : theme.colorScheme.primaryContainer;
    final badgeSize = compact ? 48.0 : 72.0;

    final column = Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        if (imageAsset != null && !compact)
          ClipRRect(
            borderRadius: BorderRadius.circular(brand.cardRadius),
            child: Image.asset(
              imageAsset!,
              height: 140,
              fit: BoxFit.contain,
              errorBuilder: (context, error, stackTrace) =>
                  _IconBadge(
                    icon: icon,
                    color: iconColor,
                    background: badgeColor,
                    size: badgeSize,
                  ),
            ),
          )
        else
          _IconBadge(
            icon: icon,
            color: iconColor,
            background: badgeColor,
            size: badgeSize,
          ),
        SizedBox(height: compact ? 12 : 16),
        Text(
          message,
          textAlign: TextAlign.center,
          style: theme.textTheme.titleMedium?.copyWith(
            color: isError ? theme.colorScheme.error : null,
          ),
        ),
        if (detail != null) ...[
          const SizedBox(height: 8),
          Text(
            detail!,
            textAlign: TextAlign.center,
            style: theme.textTheme.bodyMedium?.copyWith(
              color: theme.colorScheme.onSurfaceVariant,
              height: 1.4,
            ),
          ),
        ],
        if (actionLabel != null && onAction != null) ...[
          SizedBox(height: compact ? 12 : 20),
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
    );

    if (compact) {
      return ClinicCard(
        padding: const EdgeInsets.all(20),
        child: Center(child: column),
      );
    }
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 360),
          child: column,
        ),
      ),
    );
  }
}

class _IconBadge extends StatelessWidget {
  const _IconBadge({
    required this.icon,
    required this.color,
    required this.background,
    required this.size,
  });

  final IconData icon;
  final Color color;
  final Color background;
  final double size;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(color: background, shape: BoxShape.circle),
      child: Icon(icon, size: size * 0.46, color: color),
    );
  }
}

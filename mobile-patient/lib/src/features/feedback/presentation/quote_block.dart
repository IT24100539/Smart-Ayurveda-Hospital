import 'package:flutter/material.dart';

import '../../../theme/app_theme.dart';

/// A patient's note set apart inside a card: soft surface with a gold edge.
///
/// Colors come from the theme, so the block stays readable in dark mode.
class QuoteBlock extends StatelessWidget {
  const QuoteBlock({required this.text, super.key});

  final String text;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final brand = AyurvedaThemeExtension.of(context);
    return ClipRRect(
      borderRadius: BorderRadius.circular(12),
      child: DecoratedBox(
        decoration: BoxDecoration(color: theme.colorScheme.surface),
        child: IntrinsicHeight(
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Container(width: 3, color: brand.goldAccent),
              Expanded(
                child: Padding(
                  padding: const EdgeInsets.fromLTRB(14, 12, 14, 12),
                  child: Text(
                    text,
                    style: theme.textTheme.bodyLarge?.copyWith(height: 1.45),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

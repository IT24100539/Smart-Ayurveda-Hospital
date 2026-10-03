import 'package:flutter/material.dart';

import '../../../../l10n/feature_localizations.dart';
import '../../../../shared/widgets/clinic_widgets.dart';
import '../../../../theme/app_theme.dart';

class CharakaAiCard extends StatelessWidget {
  const CharakaAiCard({super.key, required this.onTap});

  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final copy = FeatureLocalizations.of(context);
    final theme = Theme.of(context);
    final brand = AyurvedaThemeExtension.of(context);

    return ClinicCard(
      padding: EdgeInsets.zero,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(brand.cardRadius),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Row(
            children: [
              Container(
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  gradient: LinearGradient(
                    colors: [brand.headerGradientStart, brand.headerGradientEnd],
                  ),
                  borderRadius: BorderRadius.circular(14),
                ),
                child: Icon(
                  Icons.psychology_outlined,
                  color: brand.onHeader,
                  size: 26,
                ),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Wraps when the card is half width and the title is long.
                    Wrap(
                      spacing: 6,
                      runSpacing: 4,
                      crossAxisAlignment: WrapCrossAlignment.center,
                      children: [
                        Text(
                          copy.charakaChatTitle,
                          style: theme.textTheme.titleMedium?.copyWith(
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        const PillChip(label: 'AI'),
                      ],
                    ),
                    const SizedBox(height: 3),
                    Text(
                      copy.text(
                        'Ask about therapies, fees, schedules or your UHID record.',
                        'ප්‍රතිකාර, ගාස්තු, කාලසටහන් හෝ ඔබගේ UHID තොරතුරු විමසන්න.',
                      ),
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                  ],
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 12,
                  vertical: 8,
                ),
                decoration: BoxDecoration(
                  color: theme.colorScheme.primaryContainer,
                  borderRadius: BorderRadius.circular(999),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      copy.text('Chat', 'අසන්න'),
                      style: TextStyle(
                        color: brand.teal,
                        fontWeight: FontWeight.bold,
                        fontSize: 13,
                      ),
                    ),
                    const SizedBox(width: 4),
                    Icon(Icons.arrow_forward, size: 14, color: brand.teal),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

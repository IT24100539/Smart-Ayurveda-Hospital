import 'package:flutter/material.dart';

import '../../../l10n/feature_localizations.dart';
import '../../../shared/widgets/clinic_widgets.dart';
import '../../../theme/app_theme.dart';
import '../../records/domain/patient_records.dart' show formatMoney;
import '../../treatments/domain/treatment_models.dart' show Treatment;

abstract final class TherapyCardKeys {
  static Key card(String treatmentId) => ValueKey('therapy-card-$treatmentId');
}

/// Currency is not part of the treatments API; the hospital prices in rupees.
const therapyCurrency = 'LKR';

/// `HerbalSteam` becomes `Herbal Steam`.
String therapyCategoryLabel(String category) =>
    category.replaceAllMapped(RegExp(r'(?<=[a-z])(?=[A-Z])'), (_) => ' ');

/// Selectable therapy: name, description, duration with a clock, price in teal serif, category pill.
class TherapyCard extends StatelessWidget {
  const TherapyCard({
    required this.treatment,
    required this.selected,
    required this.onTap,
    super.key,
  });

  final Treatment treatment;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final brand = AyurvedaThemeExtension.of(context);
    final copy = FeatureLocalizations.of(context);
    final sinhala = Localizations.localeOf(context).languageCode == 'si';
    final name = sinhala ? treatment.nameSinhala : treatment.nameEnglish;
    final category = treatment.category;
    final minutes = treatment.durationMinutes;
    final price = treatment.unitPrice;

    return Semantics(
      selected: selected,
      button: true,
      child: Material(
        key: TherapyCardKeys.card(treatment.id),
        color: selected
            ? theme.colorScheme.primaryContainer
            : theme.cardTheme.color ?? theme.colorScheme.surfaceContainerLow,
        clipBehavior: Clip.antiAlias,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(brand.cardRadius),
          side: BorderSide(
            color: selected ? brand.teal : brand.cardBorderColor,
            width: selected ? 2 : 1,
          ),
        ),
        child: InkWell(
          onTap: onTap,
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: Text(
                        name,
                        style: theme.textTheme.titleMedium?.copyWith(
                          fontFamily: AyurvedaFonts.serif,
                          fontFamilyFallback: AyurvedaFonts.fallback,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                    if (category != null) ...[
                      const SizedBox(width: 8),
                      PillChip(
                        key: const ValueKey('therapy-category'),
                        label: therapyCategoryLabel(category),
                      ),
                    ],
                  ],
                ),
                if (treatment.description.isNotEmpty) ...[
                  const SizedBox(height: 8),
                  Text(
                    treatment.description,
                    maxLines: 3,
                    overflow: TextOverflow.ellipsis,
                    style: theme.textTheme.bodyMedium?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                      height: 1.35,
                    ),
                  ),
                ],
                if (minutes != null || price != null) ...[
                  const SizedBox(height: 14),
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.center,
                    children: [
                      if (minutes != null) ...[
                        Icon(
                          Icons.schedule,
                          size: 16,
                          color: theme.colorScheme.onSurfaceVariant,
                        ),
                        const SizedBox(width: 4),
                        Flexible(
                          child: Text(
                            copy.duration(minutes),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: theme.textTheme.bodySmall?.copyWith(
                              color: theme.colorScheme.onSurfaceVariant,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ),
                      ],
                      const SizedBox(width: 12),
                      if (price != null)
                        Expanded(
                          child: Align(
                            alignment: AlignmentDirectional.centerEnd,
                            child: FittedBox(
                              fit: BoxFit.scaleDown,
                              child: Text(
                                formatMoney(therapyCurrency, price),
                                key: const ValueKey('therapy-price'),
                                style: AyurvedaType.price(context),
                              ),
                            ),
                          ),
                        ),
                    ],
                  ),
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }
}

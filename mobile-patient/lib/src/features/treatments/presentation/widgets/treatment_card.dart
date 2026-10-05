import 'package:flutter/material.dart';

import '../../../../theme/app_theme.dart';
import '../../application/treatments_provider.dart';
import '../../domain/treatment_models.dart';

String getTreatmentImageAsset(String name) {
  final lower = name.toLowerCase();
  if (lower.contains('panchakarma')) return 'assets/images/therapy-panchakarma.png';
  if (lower.contains('shirodhara')) return 'assets/images/therapy-shirodhara.png';
  if (lower.contains('abhyanga')) return 'assets/images/therapy-abhyanga.png';
  if (lower.contains('herbal') || lower.contains('medicine') || lower.contains('apothecary')) {
    return 'assets/images/therapy-herbal-medicine.png';
  }
  if (lower.contains('yoga') || lower.contains('meditation')) return 'assets/images/therapy-yoga.png';
  if (lower.contains('diet') || lower.contains('nutrition')) return 'assets/images/therapy-nutrition.png';
  return 'assets/images/panchakarma-room.png';
}

class TreatmentCard extends StatelessWidget {
  const TreatmentCard({
    super.key,
    required this.treatment,
    required this.onTap,
  });

  final Treatment treatment;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    // Compute "Available Today"
    final today = DateTime.now();
    final isAvailable = isAvailableToday(treatment.scheduleDays, today);

    return Card(
      clipBehavior: Clip.antiAlias,
      margin: const EdgeInsets.only(bottom: 14),
      elevation: 2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(20),
        side: BorderSide(
          color: theme.colorScheme.outline.withValues(alpha: 0.5),
        ),
      ),
      child: InkWell(
        onTap: onTap,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            SizedBox(
              height: 120,
              width: double.infinity,
              child: Stack(
                fit: StackFit.expand,
                children: [
                  Image.asset(
                    getTreatmentImageAsset(treatment.nameEnglish),
                    fit: BoxFit.cover,
                    errorBuilder: (context, error, stackTrace) => Container(
                      color: theme.colorScheme.primaryContainer,
                      child: Icon(
                        Icons.spa_outlined,
                        size: 40,
                        color: theme.colorScheme.primary,
                      ),
                    ),
                  ),
                  DecoratedBox(
                    decoration: BoxDecoration(
                      gradient: LinearGradient(
                        begin: Alignment.topCenter,
                        end: Alignment.bottomCenter,
                        colors: [
                          Colors.transparent,
                          AyurvedaColors.scrim.withValues(alpha: 0.35),
                        ],
                      ),
                    ),
                  ),
                  Positioned(
                    top: 10,
                    right: 10,
                    child: isAvailable
                        ? Container(
                            padding: const EdgeInsets.symmetric(
                              horizontal: 10,
                              vertical: 4,
                            ),
                            decoration: BoxDecoration(
                              color: theme.colorScheme.primary,
                              borderRadius: BorderRadius.circular(12),
                              boxShadow: [
                                BoxShadow(
                                  color: AyurvedaColors.scrim.withValues(alpha: 0.2),
                                  blurRadius: 4,
                                ),
                              ],
                            ),
                            child: const Text(
                              'Available Today',
                              style: TextStyle(
                                color: AyurvedaColors.onScrim,
                                fontSize: 11,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          )
                        : Container(
                            padding: const EdgeInsets.symmetric(
                              horizontal: 10,
                              vertical: 4,
                            ),
                            decoration: BoxDecoration(
                              color: AyurvedaColors.scrim.withValues(alpha: 0.54),
                              borderRadius: BorderRadius.circular(12),
                            ),
                            child: const Text(
                              'Not Today',
                              style: TextStyle(
                                color: AyurvedaColors.onScrim,
                                fontSize: 11,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ),
                  ),
                ],
              ),
            ),
            Padding(
              padding: const EdgeInsets.all(16.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    treatment.nameSinhala,
                    style: theme.textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.bold,
                      fontFamily: AyurvedaFonts.serif,
                      fontFamilyFallback: AyurvedaFonts.fallback,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    treatment.nameEnglish,
                    style: theme.textTheme.bodyMedium?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                  ),
                  const SizedBox(height: 12),
                  Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children: treatment.scheduleDays.map((day) {
                      return Chip(
                        label: Text(formatDayTag(day)),
                        visualDensity: VisualDensity.compact,
                        materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                        labelStyle: theme.textTheme.labelSmall,
                        backgroundColor: theme.colorScheme.surfaceContainerLow,
                        side: BorderSide(
                          color: theme.colorScheme.outline.withValues(alpha: 0.4),
                        ),
                      );
                    }).toList(),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

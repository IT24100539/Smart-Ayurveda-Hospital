import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../router/app_routes.dart';
import '../../../../theme/app_theme.dart';
import '../../../treatments/application/treatments_provider.dart';
import '../../../treatments/domain/treatment_models.dart';

/// Dynamic carousel of treatments fetched from treatmentsProvider
class FeaturedTherapiesCarousel extends StatelessWidget {
  const FeaturedTherapiesCarousel({
    super.key,
    required this.treatments,
    required this.isLoading,
  });

  final List<Treatment> treatments;
  final bool isLoading;

  static String _imageForTreatment(String name) {
    final lower = name.toLowerCase();
    if (lower.contains('shirodhara')) return 'assets/images/therapy-shirodhara.png';
    if (lower.contains('abhyanga')) return 'assets/images/therapy-abhyanga.png';
    if (lower.contains('panchakarma')) return 'assets/images/therapy-panchakarma.png';
    if (lower.contains('yoga')) return 'assets/images/therapy-yoga.png';
    if (lower.contains('diet') || lower.contains('nutrition') || lower.contains('ahara')) {
      return 'assets/images/therapy-nutrition.png';
    }
    return 'assets/images/therapy-herbal-medicine.png';
  }

  @override
  Widget build(BuildContext context) {
    if (isLoading && treatments.isEmpty) {
      return const SizedBox(
        height: 180,
        child: Center(
          child: CircularProgressIndicator(strokeWidth: 2),
        ),
      );
    }

    if (treatments.isEmpty) {
      return const SizedBox.shrink();
    }

    // Display top treatments from API
    final featured = treatments.take(6).toList();

    return SizedBox(
      height: 180,
      child: ListView.builder(
        scrollDirection: Axis.horizontal,
        clipBehavior: Clip.none,
        itemCount: featured.length,
        itemBuilder: (context, index) {
          final t = featured[index];
          final imgAsset = _imageForTreatment(t.nameEnglish);
          return Container(
            width: 155,
            margin: const EdgeInsets.only(right: 14),
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(18),
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withValues(alpha: 0.08),
                  blurRadius: 10,
                  offset: const Offset(0, 4),
                ),
              ],
            ),
            clipBehavior: Clip.antiAlias,
            child: InkWell(
              onTap: () => context.push('${AppRoutes.treatments}/${t.id}'),
              child: Stack(
                fit: StackFit.expand,
                children: [
                  Image.asset(
                    imgAsset,
                    fit: BoxFit.cover,
                    errorBuilder: (context, error, stackTrace) => Container(
                      color: AyurvedaColors.sageMuted,
                      child: const Icon(
                        Icons.spa_outlined,
                        color: AyurvedaColors.forest,
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
                          Colors.black.withValues(alpha: 0.82),
                        ],
                      ),
                    ),
                  ),
                  if (t.scheduleDays.isNotEmpty)
                    Positioned(
                      top: 10,
                      left: 10,
                      child: Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 8,
                          vertical: 3,
                        ),
                        decoration: BoxDecoration(
                          color: Colors.black54,
                          borderRadius: BorderRadius.circular(10),
                        ),
                        child: Text(
                          formatDayTag(t.scheduleDays.first),
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 10,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ),
                    ),
                  Positioned(
                    bottom: 12,
                    left: 12,
                    right: 12,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        if (t.nameSinhala.isNotEmpty)
                          Text(
                            t.nameSinhala,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(
                              color: Colors.white,
                              fontWeight: FontWeight.bold,
                              fontSize: 13,
                              fontFamily: 'serif',
                            ),
                          ),
                        Text(
                          t.nameEnglish,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(
                            color: Colors.white.withValues(alpha: 0.9),
                            fontSize: 11,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }
}

import 'dart:ui' show PointerDeviceKind;

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../l10n/feature_localizations.dart';
import '../../../../router/app_routes.dart';
import '../../../../shared/widgets/empty_state.dart';
import '../../../../shared/widgets/skeleton.dart';
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

  static const double _height = 180;

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
      return const _CarouselSkeleton(height: _height);
    }

    if (treatments.isEmpty) {
      final copy = FeatureLocalizations.of(context);
      return EmptyState(
        compact: true,
        icon: Icons.spa_outlined,
        message: copy.text(
          'No therapies are listed yet.',
          'ප්‍රතිකාර තවම ලැයිස්තුගත කර නැත.',
        ),
      );
    }

    final brand = AyurvedaThemeExtension.of(context);
    // Display top treatments from API
    final featured = treatments.take(6).toList();

    return SizedBox(
      height: _height,
      // Mouse drag scrolls the row on Flutter web and desktop.
      child: ScrollConfiguration(
        behavior: ScrollConfiguration.of(context).copyWith(
          dragDevices: {
            PointerDeviceKind.touch,
            PointerDeviceKind.mouse,
            PointerDeviceKind.trackpad,
            PointerDeviceKind.stylus,
          },
        ),
        child: ListView.builder(
          scrollDirection: Axis.horizontal,
          itemCount: featured.length,
          itemBuilder: (context, index) {
            final t = featured[index];
            final imgAsset = _imageForTreatment(t.nameEnglish);
            return Container(
              width: 168,
              margin: const EdgeInsets.only(right: 14),
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(brand.cardRadius),
                border: Border.all(color: brand.cardBorderColor),
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
                        color: Theme.of(context).colorScheme.primaryContainer,
                        child: Icon(Icons.spa_outlined, color: brand.teal),
                      ),
                    ),
                    DecoratedBox(
                      decoration: BoxDecoration(
                        gradient: LinearGradient(
                          begin: Alignment.topCenter,
                          end: Alignment.bottomCenter,
                          colors: [
                            Colors.transparent,
                            AyurvedaColors.scrim.withValues(alpha: 0.82),
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
                            color: AyurvedaColors.scrim.withValues(alpha: 0.65),
                            borderRadius: BorderRadius.circular(999),
                          ),
                          child: Text(
                            formatDayTag(t.scheduleDays.first),
                            style: const TextStyle(
                              color: AyurvedaColors.onScrim,
                              fontSize: 11,
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
                                color: AyurvedaColors.onScrim,
                                fontWeight: FontWeight.bold,
                                fontSize: 14,
                                fontFamily: AyurvedaFonts.serif,
                              ),
                            ),
                          Text(
                            t.nameEnglish,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(
                              color: AyurvedaColors.onScrim,
                              fontSize: 12,
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
      ),
    );
  }
}

class _CarouselSkeleton extends StatelessWidget {
  const _CarouselSkeleton({required this.height});

  final double height;

  @override
  Widget build(BuildContext context) {
    return SkeletonScope(
      child: SizedBox(
        height: height,
        child: ListView(
          scrollDirection: Axis.horizontal,
          physics: const NeverScrollableScrollPhysics(),
          children: [
            for (var index = 0; index < 4; index++)
              const Padding(
                padding: EdgeInsets.only(right: 14),
                child: SkeletonBone(height: 180, width: 168, radius: 22),
              ),
          ],
        ),
      ),
    );
  }
}

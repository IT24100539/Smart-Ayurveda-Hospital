import 'package:flutter/material.dart';

import '../../../../theme/app_theme.dart';

class HeroBanner extends StatelessWidget {
  const HeroBanner({
    super.key,
    required this.title,
    required this.subtitle,
    required this.ctaLabel,
    required this.onBookPressed,
  });

  final String title;
  final String subtitle;
  final String ctaLabel;
  final VoidCallback onBookPressed;

  @override
  Widget build(BuildContext context) {
    final brand = AyurvedaThemeExtension.of(context);
    return LayoutBuilder(
      builder: (context, constraints) {
        // Taller on tablets and web so the photo is not a thin strip.
        final height = constraints.maxWidth >= 600 ? 280.0 : 210.0;
        return Container(
          height: height,
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(brand.headerRadius),
            border: Border.all(color: brand.cardBorderColor),
          ),
          clipBehavior: Clip.antiAlias,
          child: Stack(
            fit: StackFit.expand,
            children: [
              Image.asset(
                'assets/images/hero-ayurveda-wellness.png',
                fit: BoxFit.cover,
                errorBuilder: (context, error, stackTrace) => Image.asset(
                  'assets/images/ayurveda-courtyard.png',
                  fit: BoxFit.cover,
                  errorBuilder: (context, error, stackTrace) =>
                      ColoredBox(color: brand.headerGradientStart),
                ),
              ),
              DecoratedBox(
                decoration: BoxDecoration(
                  gradient: LinearGradient(
                    begin: Alignment.topCenter,
                    end: Alignment.bottomCenter,
                    colors: [
                      AyurvedaColors.scrim.withValues(alpha: 0.2),
                      AyurvedaColors.scrim.withValues(alpha: 0.82),
                    ],
                  ),
                ),
              ),
              Positioned(
                bottom: 18,
                left: 20,
                right: 20,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      title,
                      style: const TextStyle(
                        color: AyurvedaColors.onScrim,
                        fontSize: 22,
                        height: 1.15,
                        fontWeight: FontWeight.w700,
                        fontFamily: AyurvedaFonts.serif,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      subtitle,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        color: AyurvedaColors.onScrim,
                        fontSize: 13,
                        height: 1.35,
                      ),
                    ),
                    const SizedBox(height: 14),
                    ElevatedButton.icon(
                      onPressed: onBookPressed,
                      icon: const Icon(Icons.calendar_today, size: 16),
                      label: Text(ctaLabel),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: brand.teal,
                        foregroundColor: brand.onTeal,
                        minimumSize: const Size(180, 44),
                        padding: const EdgeInsets.symmetric(horizontal: 20),
                        shape: const StadiumBorder(),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}

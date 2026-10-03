import 'package:flutter/material.dart';

import '../../../theme/app_theme.dart';

/// Teal header used at the top of patient feedback screens.
///
/// Same gradient, radius and small-caps kicker as `SectionBanner`, with an optional photo.
class FeedbackBanner extends StatelessWidget {
  const FeedbackBanner({
    required this.kicker,
    required this.title,
    required this.body,
    this.trailing,
    this.imageAsset,
    super.key,
  });

  final String kicker;
  final String title;
  final String body;
  final String? trailing;
  final String? imageAsset;

  @override
  Widget build(BuildContext context) {
    final brand = AyurvedaThemeExtension.of(context);
    return Container(
      width: double.infinity,
      clipBehavior: Clip.antiAlias,
      decoration: BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [brand.headerGradientStart, brand.headerGradientEnd],
        ),
        borderRadius: BorderRadius.circular(brand.headerRadius),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (imageAsset != null)
            Image.asset(
              imageAsset!,
              height: 150,
              width: double.infinity,
              fit: BoxFit.cover,
              errorBuilder: (context, error, stackTrace) =>
                  const SizedBox(height: 0),
            ),
          Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  kicker.toUpperCase(),
                  style: AyurvedaType.eyebrow(
                    context,
                    color: brand.avatarBackground,
                  ),
                ),
                const SizedBox(height: 8),
                Text(
                  title,
                  style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                    color: brand.onHeader,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 8),
                Text(body, style: TextStyle(color: brand.onHeader, height: 1.4)),
                if (trailing != null) ...[
                  const SizedBox(height: 14),
                  Text(
                    trailing!,
                    style: TextStyle(
                      color: brand.onHeader,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }
}

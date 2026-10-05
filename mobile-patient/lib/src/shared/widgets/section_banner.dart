import 'package:flutter/material.dart';

import '../../theme/app_theme.dart';

/// Forest summary used at the top of patient screens.
class SectionBanner extends StatelessWidget {
  const SectionBanner({
    required this.kicker,
    required this.title,
    required this.body,
    super.key,
  });

  final String kicker;
  final String title;
  final String body;

  @override
  Widget build(BuildContext context) {
    final brand = AyurvedaThemeExtension.of(context);
    return Container(
      width: double.infinity,
      margin: const EdgeInsets.only(bottom: 16),
      padding: const EdgeInsets.all(20),
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
          Text(
            kicker.toUpperCase(),
            style: AyurvedaType.eyebrow(context, color: brand.avatarBackground),
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
        ],
      ),
    );
  }
}

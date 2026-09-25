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
    return Container(
      width: double.infinity,
      margin: const EdgeInsets.only(bottom: 16),
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: AyurvedaColors.forest,
        borderRadius: BorderRadius.circular(20),
        border: const Border(
          bottom: BorderSide(color: AyurvedaColors.gold, width: 3),
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            kicker.toUpperCase(),
            style: const TextStyle(
              color: AyurvedaColors.sageMuted,
              fontSize: 11,
              fontWeight: FontWeight.w700,
              letterSpacing: 1.4,
            ),
          ),
          const SizedBox(height: 8),
          Text(
            title,
            style: Theme.of(context).textTheme.headlineSmall?.copyWith(
              color: Colors.white,
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 8),
          Text(
            body,
            style: const TextStyle(color: AyurvedaColors.sageMuted, height: 1.4),
          ),
        ],
      ),
    );
  }
}

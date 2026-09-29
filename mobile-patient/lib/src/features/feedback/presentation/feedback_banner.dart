import 'package:flutter/material.dart';

import '../../../theme/app_theme.dart';

/// Forest summary used at the top of patient feedback screens.
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
    return Container(
      width: double.infinity,
      clipBehavior: Clip.antiAlias,
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
          if (imageAsset != null)
            Image.asset(
              imageAsset!,
              height: 150,
              width: double.infinity,
              fit: BoxFit.cover,
            ),
          Padding(
            padding: const EdgeInsets.all(20),
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
            style: const TextStyle(
              color: AyurvedaColors.sageMuted,
              height: 1.4,
            ),
          ),
          if (trailing != null) ...[
            const SizedBox(height: 14),
            Text(
              trailing!,
              style: const TextStyle(
                color: Colors.white,
                fontWeight: FontWeight.w600,
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

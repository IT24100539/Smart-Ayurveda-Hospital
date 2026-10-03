import 'package:flutter/material.dart';

import '../../../theme/app_theme.dart';
import 'feedback_keys.dart';

/// Five-star score. [value] is 0 when nothing is selected, otherwise 1-5.
class StarRating extends StatelessWidget {
  const StarRating({required this.value, required this.onChanged, super.key});

  final int value;
  final ValueChanged<int>? onChanged;

  @override
  Widget build(BuildContext context) {
    final gold = AyurvedaThemeExtension.of(context).goldAccent;
    return Row(
      children: [
        for (var star = 1; star <= 5; star++)
          IconButton(
            key: FeedbackKeys.star(star),
            tooltip: '$star',
            onPressed: onChanged == null ? null : () => onChanged!(star),
            icon: Icon(
              star <= value ? Icons.star_rounded : Icons.star_outline_rounded,
              color: gold,
            ),
          ),
      ],
    );
  }
}

/// Read-only stars for a saved rating. Gold turns lighter in dark mode.
class StarDisplay extends StatelessWidget {
  const StarDisplay({required this.rating, this.size = 16, super.key});

  final int rating;
  final double size;

  @override
  Widget build(BuildContext context) {
    final gold = AyurvedaThemeExtension.of(context).goldAccent;
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        for (var star = 1; star <= 5; star++)
          Icon(
            star <= rating ? Icons.star_rounded : Icons.star_outline_rounded,
            size: size,
            color: gold,
          ),
      ],
    );
  }
}

import 'package:flutter/material.dart';

import '../../../theme/app_theme.dart';
import 'feedback_keys.dart';

/// Five-star score. [value] is 0 when nothing is selected, otherwise 1–5.
class StarRating extends StatelessWidget {
  const StarRating({required this.value, required this.onChanged, super.key});

  final int value;
  final ValueChanged<int> onChanged;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        for (var star = 1; star <= 5; star++)
          IconButton(
            key: FeedbackKeys.star(star),
            tooltip: '$star',
            onPressed: () => onChanged(star),
            icon: Icon(
              star <= value ? Icons.star : Icons.star_border,
              color: AyurvedaColors.gold,
            ),
          ),
      ],
    );
  }
}

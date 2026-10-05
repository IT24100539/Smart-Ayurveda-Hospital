import 'package:flutter/material.dart';

import 'clinic_widgets.dart';
import 'page_layout.dart';
import 'responsive_columns.dart';

/// Drives one shared pulse for every [SkeletonBone] below it.
///
/// With system animations off the bones stay still.
class SkeletonScope extends StatefulWidget {
  const SkeletonScope({required this.child, super.key});

  final Widget child;

  @override
  State<SkeletonScope> createState() => _SkeletonScopeState();
}

class _SkeletonScopeState extends State<SkeletonScope>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 1100),
  );

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    final still = MediaQuery.maybeDisableAnimationsOf(context) ?? false;
    if (still) {
      _controller.stop();
      _controller.value = 0.5;
    } else if (!_controller.isAnimating) {
      _controller.repeat(reverse: true);
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ExcludeSemantics(
      child: _PulseScope(
        notifier: _controller,
        child: widget.child,
      ),
    );
  }
}

class _PulseScope extends InheritedNotifier<AnimationController> {
  const _PulseScope({required super.notifier, required super.child});
}

/// Grey placeholder block. Reads the colors from the theme, so it works in light and dark.
class SkeletonBone extends StatelessWidget {
  const SkeletonBone({
    this.height = 14,
    this.width,
    this.radius = 8,
    this.circle = false,
    super.key,
  });

  final double height;
  final double? width;
  final double radius;
  final bool circle;

  @override
  Widget build(BuildContext context) {
    final scope = context
        .dependOnInheritedWidgetOfExactType<_PulseScope>()
        ?.notifier;
    final onSurface = Theme.of(context).colorScheme.onSurface;
    final t = scope?.value ?? 0.5;
    final color = onSurface.withValues(alpha: 0.06 + 0.07 * t);
    return Container(
      height: height,
      width: circle ? height : width,
      decoration: BoxDecoration(
        color: color,
        shape: circle ? BoxShape.circle : BoxShape.rectangle,
        borderRadius: circle ? null : BorderRadius.circular(radius),
      ),
    );
  }
}

/// Placeholder for a [SectionBanner] or hero header.
class SkeletonBanner extends StatelessWidget {
  const SkeletonBanner({this.height = 120, super.key});

  final double height;

  @override
  Widget build(BuildContext context) {
    return SkeletonBone(height: height, radius: 28);
  }
}

/// Placeholder for one list card: a title line, a status pill, and body lines.
class SkeletonCard extends StatelessWidget {
  const SkeletonCard({this.lines = 2, this.leading = false, super.key});

  final int lines;

  /// Adds a round icon or avatar slot before the title.
  final bool leading;

  @override
  Widget build(BuildContext context) {
    return ClinicCard(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (leading) ...[
            const SkeletonBone(height: 40, circle: true),
            const SizedBox(width: 14),
          ],
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    const Expanded(
                      child: Align(
                        alignment: Alignment.centerLeft,
                        child: FractionallySizedBox(
                          widthFactor: 0.6,
                          child: SkeletonBone(height: 16),
                        ),
                      ),
                    ),
                    const SizedBox(width: 12),
                    const SkeletonBone(height: 22, width: 64, radius: 999),
                  ],
                ),
                for (var line = 0; line < lines; line++) ...[
                  const SizedBox(height: 10),
                  FractionallySizedBox(
                    widthFactor: line == lines - 1 ? 0.45 : 0.9,
                    child: const SkeletonBone(height: 12),
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

/// Loading body for a card list. Mirrors the loaded layout: an optional
/// banner, then cards that split over two columns on wide screens.
class SkeletonList extends StatelessWidget {
  const SkeletonList({
    this.itemCount = 4,
    this.withBanner = true,
    this.twoColumns = false,
    this.lines = 2,
    this.leading = false,
    this.listKey,
    super.key,
  });

  final int itemCount;
  final bool withBanner;

  /// Match screens whose loaded list uses [ResponsiveColumns].
  final bool twoColumns;
  final int lines;
  final bool leading;
  final Key? listKey;

  @override
  Widget build(BuildContext context) {
    final cards = [
      for (var index = 0; index < itemCount; index++)
        SkeletonCard(lines: lines, leading: leading),
    ];
    return SkeletonScope(
      child: PageListView(
        key: listKey,
        maxWidth: twoColumns ? wideContentWidth : contentWidth,
        physics: const NeverScrollableScrollPhysics(),
        children: [
          if (withBanner) ...[
            const SkeletonBanner(),
            const SizedBox(height: 16),
          ],
          if (twoColumns)
            ResponsiveColumns(children: cards)
          else
            for (var index = 0; index < cards.length; index++) ...[
              if (index > 0) const SizedBox(height: 12),
              cards[index],
            ],
        ],
      ),
    );
  }
}

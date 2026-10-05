import 'package:flutter/material.dart';

/// Width at which card lists switch from one column to two.
const double twoColumnBreakpoint = 840;

/// Stacks [children] on phones and splits them over two columns on wide screens.
///
/// Items alternate between the columns, so reading order runs left to right.
class ResponsiveColumns extends StatelessWidget {
  const ResponsiveColumns({
    required this.children,
    this.spacing = 12,
    this.breakpoint = twoColumnBreakpoint,
    super.key,
  });

  final List<Widget> children;
  final double spacing;
  final double breakpoint;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final twoColumns =
            constraints.maxWidth >= breakpoint && children.length > 1;
        if (!twoColumns) {
          return Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: _spaced(children),
          );
        }
        final left = <Widget>[];
        final right = <Widget>[];
        for (var index = 0; index < children.length; index++) {
          (index.isEven ? left : right).add(children[index]);
        }
        return Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(
              child: Column(
                key: const ValueKey('responsive-column-start'),
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: _spaced(left),
              ),
            ),
            SizedBox(width: spacing),
            Expanded(
              child: Column(
                key: const ValueKey('responsive-column-end'),
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: _spaced(right),
              ),
            ),
          ],
        );
      },
    );
  }

  List<Widget> _spaced(List<Widget> items) => [
    for (var index = 0; index < items.length; index++) ...[
      if (index > 0) SizedBox(height: spacing),
      items[index],
    ],
  ];
}

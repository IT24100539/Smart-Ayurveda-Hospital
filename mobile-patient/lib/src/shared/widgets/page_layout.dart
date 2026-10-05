import 'dart:math' as math;

import 'package:flutter/material.dart';

/// Readable column width for lists, forms and feeds on tablets and Flutter web.
const double contentWidth = 720;

/// Width for card grids that split over two columns (see `ResponsiveColumns`).
const double wideContentWidth = 1040;

/// Extra side padding that centres content of [maxWidth] inside [viewportWidth].
double pageGutter(double viewportWidth, double maxWidth, double basePadding) {
  return math.max(0, (viewportWidth - basePadding * 2 - maxWidth) / 2);
}

/// A [ListView] whose content stays at most [maxWidth] wide and centred.
///
/// The list itself spans the full width, so the scrollbar and wheel scrolling
/// work across the whole page on web. Phones keep the plain 20px edge padding.
class PageListView extends StatelessWidget {
  const PageListView({
    required this.children,
    this.maxWidth = contentWidth,
    this.padding = const EdgeInsets.fromLTRB(20, 8, 20, 24),
    this.physics = const AlwaysScrollableScrollPhysics(),
    this.controller,
    super.key,
  })  : itemCount = null,
        itemBuilder = null,
        spacing = 0;

  /// Lazily built items with [spacing] between them.
  const PageListView.builder({
    required int this.itemCount,
    required IndexedWidgetBuilder this.itemBuilder,
    this.spacing = 12,
    this.maxWidth = contentWidth,
    this.padding = const EdgeInsets.fromLTRB(20, 8, 20, 24),
    this.physics = const AlwaysScrollableScrollPhysics(),
    this.controller,
    super.key,
  }) : children = const [];

  final List<Widget> children;
  final int? itemCount;
  final IndexedWidgetBuilder? itemBuilder;
  final double spacing;
  final double maxWidth;
  final EdgeInsets padding;
  final ScrollPhysics physics;
  final ScrollController? controller;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final gutter = pageGutter(constraints.maxWidth, maxWidth, padding.left);
        final resolved = padding.copyWith(
          left: padding.left + gutter,
          right: padding.right + gutter,
        );
        final builder = itemBuilder;
        if (builder == null) {
          return ListView(
            controller: controller,
            physics: physics,
            padding: resolved,
            children: children,
          );
        }
        return ListView.separated(
          controller: controller,
          physics: physics,
          padding: resolved,
          itemCount: itemCount!,
          itemBuilder: builder,
          separatorBuilder: (context, index) => SizedBox(height: spacing),
        );
      },
    );
  }
}

/// Eager scroll view (every child is built) with the same centred width as [PageListView].
class PageScrollView extends StatelessWidget {
  const PageScrollView({
    required this.children,
    this.maxWidth = wideContentWidth,
    this.padding = const EdgeInsets.fromLTRB(20, 8, 20, 32),
    super.key,
  });

  final List<Widget> children;
  final double maxWidth;
  final EdgeInsets padding;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final gutter = pageGutter(constraints.maxWidth, maxWidth, padding.left);
        return SingleChildScrollView(
          padding: padding.copyWith(
            left: padding.left + gutter,
            right: padding.right + gutter,
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: children,
          ),
        );
      },
    );
  }
}

/// Centres a non-scrolling [child] at most [maxWidth] wide.
class CenteredContent extends StatelessWidget {
  const CenteredContent({
    required this.child,
    this.maxWidth = contentWidth,
    super.key,
  });

  final Widget child;
  final double maxWidth;

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: Alignment.topCenter,
      child: ConstrainedBox(
        constraints: BoxConstraints(maxWidth: maxWidth),
        child: child,
      ),
    );
  }
}

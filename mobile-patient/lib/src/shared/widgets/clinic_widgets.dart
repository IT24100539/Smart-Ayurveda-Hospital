import 'package:flutter/material.dart';

import '../../theme/app_theme.dart';

/// First and last initials, uppercased. A single name uses its first letter.
String patientInitials(String name) {
  final parts = name
      .trim()
      .split(RegExp(r'\s+'))
      .where((part) => part.isNotEmpty)
      .toList();
  if (parts.isEmpty) return '?';
  String mark(String part) => part.characters.first.toUpperCase();
  if (parts.length == 1) return mark(parts.first);
  return '${mark(parts.first)}${mark(parts.last)}';
}

/// Flat card: 24px radius, 1px border, no shadow.
class ClinicCard extends StatelessWidget {
  const ClinicCard({
    required this.child,
    this.padding = const EdgeInsets.all(16),
    this.margin = EdgeInsets.zero,
    super.key,
  });

  final Widget child;
  final EdgeInsetsGeometry padding;
  final EdgeInsetsGeometry margin;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final brand = AyurvedaThemeExtension.of(context);
    return Padding(
      padding: margin,
      child: Material(
        color: theme.cardTheme.color ?? theme.colorScheme.surfaceContainerLow,
        elevation: 0,
        shadowColor: Colors.transparent,
        clipBehavior: Clip.antiAlias,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(brand.cardRadius),
          side: BorderSide(color: brand.cardBorderColor),
        ),
        child: Padding(padding: padding, child: child),
      ),
    );
  }
}

/// Large rounded surface (28px radius, 1px border) that holds a whole flow or conversation.
class RoundedPanel extends StatelessWidget {
  const RoundedPanel({
    required this.child,
    this.padding = EdgeInsets.zero,
    super.key,
  });

  final Widget child;
  final EdgeInsetsGeometry padding;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final brand = AyurvedaThemeExtension.of(context);
    return Material(
      color: theme.cardTheme.color ?? theme.colorScheme.surfaceContainerLow,
      elevation: 0,
      shadowColor: Colors.transparent,
      clipBehavior: Clip.antiAlias,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(brand.headerRadius),
        side: BorderSide(color: brand.cardBorderColor),
      ),
      child: Padding(padding: padding, child: child),
    );
  }
}

/// Gold or cream status chip. Pass [background] for approved and pending.
class PillChip extends StatelessWidget {
  const PillChip({
    required this.label,
    this.kicker,
    this.icon,
    this.background,
    this.foreground,
    super.key,
  });

  final String label;
  final String? kicker;
  final IconData? icon;
  final Color? background;
  final Color? foreground;

  @override
  Widget build(BuildContext context) {
    final brand = AyurvedaThemeExtension.of(context);
    final bg = background ?? brand.pillBackground;
    final fg = foreground ?? brand.pillForeground;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(999),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[
            Icon(icon, size: 14, color: fg),
            const SizedBox(width: 4),
          ],
          if (kicker != null) ...[
            Text(
              kicker!.toUpperCase(),
              style: AyurvedaType.eyebrow(context, color: fg),
            ),
            const SizedBox(width: 6),
          ],
          Text(
            label,
            style: TextStyle(
              color: fg,
              fontSize: 12,
              fontWeight: FontWeight.w700,
            ),
          ),
        ],
      ),
    );
  }
}

/// Error copy with a lighter tint in dark mode, always paired with an icon.
class ErrorLine extends StatelessWidget {
  const ErrorLine({required this.message, super.key});

  final String message;

  @override
  Widget build(BuildContext context) {
    final color = Theme.of(context).colorScheme.error;
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(Icons.error_outline, color: color, size: 18),
        const SizedBox(width: 8),
        Expanded(
          child: Text(
            message,
            style: TextStyle(
              color: color,
              fontSize: 14,
              height: 1.4,
              fontWeight: FontWeight.w600,
            ),
          ),
        ),
      ],
    );
  }
}

class UnderlineTabItem {
  const UnderlineTabItem({required this.label, this.count, this.tabKey});

  final String label;
  final int? count;
  final Key? tabKey;
}

/// Underline tabs. [count] shows a badge when it is greater than zero.
class UnderlineTabBar extends StatelessWidget {
  const UnderlineTabBar({required this.tabs, this.controller, super.key});

  final List<UnderlineTabItem> tabs;

  /// Uses the nearest [DefaultTabController] when null.
  final TabController? controller;

  @override
  Widget build(BuildContext context) {
    final controller = this.controller ?? DefaultTabController.of(context);
    return AnimatedBuilder(
      animation: controller,
      builder: (context, _) {
        return ScrollConfiguration(
          behavior: ScrollConfiguration.of(context).copyWith(scrollbars: false),
          child: SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: 12),
            child: Row(
              children: [
                for (var index = 0; index < tabs.length; index++)
                  _UnderlineTab(
                    item: tabs[index],
                    selected: controller.index == index,
                    onTap: () => controller.animateTo(index),
                  ),
              ],
            ),
          ),
        );
      },
    );
  }
}

class _UnderlineTab extends StatefulWidget {
  const _UnderlineTab({
    required this.item,
    required this.selected,
    required this.onTap,
  });

  final UnderlineTabItem item;
  final bool selected;
  final VoidCallback onTap;

  @override
  State<_UnderlineTab> createState() => _UnderlineTabState();
}

class _UnderlineTabState extends State<_UnderlineTab> {
  @override
  void initState() {
    super.initState();
    if (widget.selected) _revealAfterLayout();
  }

  @override
  void didUpdateWidget(_UnderlineTab oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.selected && !oldWidget.selected) _revealAfterLayout();
  }

  /// Keeps the selected tab on screen when the bar scrolls, for example after a swipe.
  void _revealAfterLayout() {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      Scrollable.ensureVisible(
        context,
        duration: const Duration(milliseconds: 200),
        curve: Curves.easeOut,
        alignment: 0.5,
      );
    });
  }

  @override
  Widget build(BuildContext context) {
    final item = widget.item;
    final selected = widget.selected;
    final onTap = widget.onTap;
    final theme = Theme.of(context);
    final brand = AyurvedaThemeExtension.of(context);
    final color = selected ? brand.teal : theme.colorScheme.onSurfaceVariant;
    final showCount = item.count != null && item.count! > 0;
    return InkWell(
      key: item.tabKey,
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.fromLTRB(12, 12, 12, 10),
        decoration: BoxDecoration(
          border: Border(
            bottom: BorderSide(
              color: selected ? brand.teal : Colors.transparent,
              width: 2,
            ),
          ),
        ),
        child: Row(
          children: [
            Text(
              item.label,
              style: TextStyle(
                color: color,
                fontWeight: selected ? FontWeight.w700 : FontWeight.w600,
              ),
            ),
            if (showCount) ...[
              const SizedBox(width: 6),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 1),
                decoration: BoxDecoration(
                  color: selected ? brand.teal : brand.neutralBackground,
                  borderRadius: BorderRadius.circular(999),
                ),
                child: Text(
                  '${item.count}',
                  style: TextStyle(
                    color: selected ? brand.onTeal : brand.neutralForeground,
                    fontSize: 11,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

/// Three-step (or more) numbered progress. Completed steps show a check.
class NumberedStepper extends StatelessWidget {
  const NumberedStepper({
    required this.labels,
    required this.current,
    super.key,
  });

  final List<String> labels;
  final int current;

  @override
  Widget build(BuildContext context) {
    final brand = AyurvedaThemeExtension.of(context);
    return Padding(
      padding: const EdgeInsets.fromLTRB(8, 4, 8, 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          for (var index = 0; index < labels.length; index++)
            Expanded(
              child: _StepColumn(
                index: index,
                label: labels[index],
                current: current,
                last: index == labels.length - 1,
                brand: brand,
              ),
            ),
        ],
      ),
    );
  }
}

class _StepColumn extends StatelessWidget {
  const _StepColumn({
    required this.index,
    required this.label,
    required this.current,
    required this.last,
    required this.brand,
  });

  final int index;
  final String label;
  final int current;
  final bool last;
  final AyurvedaThemeExtension brand;

  @override
  Widget build(BuildContext context) {
    final done = index < current;
    final active = index == current;
    final line = done || active ? brand.teal : brand.cardBorderColor;
    final fill = done || active
        ? brand.teal
        : Theme.of(context).colorScheme.surfaceContainerHigh;
    final digit = done || active
        ? brand.onTeal
        : Theme.of(context).colorScheme.onSurfaceVariant;
    return Column(
      children: [
        Row(
          children: [
            Expanded(
              child: Container(
                height: 1,
                color: index == 0
                    ? Colors.transparent
                    : (index <= current ? brand.teal : brand.cardBorderColor),
              ),
            ),
            Container(
              width: AyurvedaType.stepSize,
              height: AyurvedaType.stepSize,
              alignment: Alignment.center,
              decoration: BoxDecoration(
                color: fill,
                shape: BoxShape.circle,
                border: Border.all(color: done || active ? brand.teal : line),
              ),
              child: done
                  ? Icon(Icons.check, size: 22, color: brand.onTeal)
                  : Text(
                      '${index + 1}',
                      style: TextStyle(
                        color: digit,
                        fontWeight: FontWeight.w700,
                        fontSize: 16,
                      ),
                    ),
            ),
            Expanded(
              child: Container(
                height: 1,
                color: last
                    ? Colors.transparent
                    : (index < current ? brand.teal : brand.cardBorderColor),
              ),
            ),
          ],
        ),
        const SizedBox(height: 6),
        Text(
          label.toUpperCase(),
          textAlign: TextAlign.center,
          style: AyurvedaType.eyebrow(
            context,
            color: active
                ? brand.teal
                : Theme.of(context).colorScheme.onSurfaceVariant,
          ),
        ),
      ],
    );
  }
}

/// Horizontal prompts. The scrollbar stays hidden.
class SuggestionChips extends StatelessWidget {
  const SuggestionChips({
    required this.labels,
    required this.onSelected,
    this.enabled = true,
    super.key,
  });

  final List<String> labels;
  final ValueChanged<String> onSelected;
  final bool enabled;

  @override
  Widget build(BuildContext context) {
    final brand = AyurvedaThemeExtension.of(context);
    return SizedBox(
      height: 48,
      child: ScrollConfiguration(
        behavior: ScrollConfiguration.of(context).copyWith(scrollbars: false),
        child: ListView.separated(
          scrollDirection: Axis.horizontal,
          padding: const EdgeInsets.symmetric(horizontal: 16),
          itemCount: labels.length,
          separatorBuilder: (context, index) => const SizedBox(width: 8),
          itemBuilder: (context, index) {
            final label = labels[index];
            return ActionChip(
              label: Text(label, style: const TextStyle(fontSize: 12)),
              backgroundColor: brand.pillBackground,
              labelStyle: TextStyle(
                color: brand.pillForeground,
                fontWeight: FontWeight.w600,
              ),
              side: BorderSide.none,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(999),
              ),
              onPressed: enabled ? () => onSelected(label) : null,
            );
          },
        ),
      ),
    );
  }
}

/// Rounded field with a pill Send button.
class PillSendField extends StatelessWidget {
  const PillSendField({
    required this.controller,
    required this.onSend,
    required this.sendLabel,
    this.hint,
    this.enabled = true,
    this.busy = false,
    this.fieldKey,
    this.sendKey,
    super.key,
  });

  final TextEditingController controller;
  final VoidCallback onSend;
  final String sendLabel;
  final String? hint;
  final bool enabled;
  final bool busy;
  final Key? fieldKey;
  final Key? sendKey;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final brand = AyurvedaThemeExtension.of(context);
    final radius = BorderRadius.circular(28);
    final canSend = enabled && !busy;
    return Row(
      children: [
        Expanded(
          child: TextField(
            key: fieldKey,
            controller: controller,
            enabled: enabled,
            textInputAction: TextInputAction.send,
            onSubmitted: canSend ? (_) => onSend() : null,
            decoration: InputDecoration(
              hintText: hint,
              filled: true,
              fillColor: theme.colorScheme.surfaceContainerLow,
              contentPadding: const EdgeInsets.symmetric(
                horizontal: 18,
                vertical: 14,
              ),
              border: OutlineInputBorder(
                borderRadius: radius,
                borderSide: BorderSide(color: brand.cardBorderColor),
              ),
              enabledBorder: OutlineInputBorder(
                borderRadius: radius,
                borderSide: BorderSide(color: brand.cardBorderColor),
              ),
              focusedBorder: OutlineInputBorder(
                borderRadius: radius,
                borderSide: BorderSide(color: brand.teal, width: 1.5),
              ),
              disabledBorder: OutlineInputBorder(
                borderRadius: radius,
                borderSide: BorderSide(color: brand.cardBorderColor),
              ),
            ),
          ),
        ),
        const SizedBox(width: 8),
        FilledButton(
          key: sendKey,
          onPressed: canSend ? onSend : null,
          style: FilledButton.styleFrom(
            backgroundColor: brand.teal,
            foregroundColor: brand.onTeal,
            minimumSize: const Size(72, 48),
            padding: const EdgeInsets.symmetric(horizontal: 18),
            shape: const StadiumBorder(),
          ),
          child: busy
              ? SizedBox(
                  width: 18,
                  height: 18,
                  child: CircularProgressIndicator(
                    strokeWidth: 2,
                    color: brand.onTeal,
                  ),
                )
              : Text(sendLabel),
        ),
      ],
    );
  }
}

/// Teal identity card: initials, name, and a UHID chip.
class PatientHeaderCard extends StatelessWidget {
  const PatientHeaderCard({
    required this.name,
    required this.uhid,
    this.uhidLabel = 'UHID',
    this.subtitle,
    this.action,
    super.key,
  });

  final String name;
  final String uhid;
  final String uhidLabel;
  final String? subtitle;
  final Widget? action;

  @override
  Widget build(BuildContext context) {
    final brand = AyurvedaThemeExtension.of(context);
    final avatar = Container(
      width: AyurvedaType.avatarSize,
      height: AyurvedaType.avatarSize,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: brand.avatarBackground,
        borderRadius: BorderRadius.circular(AyurvedaType.avatarRadius),
        border: Border.all(color: brand.onHeader.withValues(alpha: 0.55)),
      ),
      child: Text(
        patientInitials(name),
        style: TextStyle(
          fontFamily: AyurvedaFonts.serif,
          fontFamilyFallback: AyurvedaFonts.fallback,
          fontWeight: FontWeight.w700,
          fontSize: 24,
          color: brand.avatarForeground,
        ),
      ),
    );
    final identity = Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Wrap(
          crossAxisAlignment: WrapCrossAlignment.center,
          spacing: 8,
          runSpacing: 8,
          children: [
            Text(
              name,
              style: TextStyle(
                fontFamily: AyurvedaFonts.serif,
                fontFamilyFallback: AyurvedaFonts.fallback,
                fontWeight: FontWeight.w700,
                fontSize: 32,
                height: 1.15,
                color: brand.onHeader,
              ),
            ),
            PillChip(kicker: uhidLabel, label: uhid),
          ],
        ),
        if (subtitle != null) ...[
          const SizedBox(height: 8),
          Text(
            subtitle!,
            style: TextStyle(color: brand.onHeader, height: 1.35, fontSize: 15),
          ),
        ],
      ],
    );

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(24),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.centerLeft,
          end: Alignment.centerRight,
          colors: [brand.headerGradientStart, brand.headerGradientEnd],
        ),
        borderRadius: BorderRadius.circular(brand.headerRadius),
        border: Border.all(color: brand.teal.withValues(alpha: 0.35)),
      ),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final width = constraints.maxWidth;
          final stacked = !width.isFinite || width < 600;
          // A row gives non-flex children an unbounded width. The action is a
          // button, which cannot take a tight infinite width.
          final actionSlot = action == null
              ? null
              : LimitedBox(maxWidth: 360, child: action!);
          if (stacked) {
            return Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                avatar,
                const SizedBox(height: 16),
                identity,
                if (actionSlot != null) ...[
                  const SizedBox(height: 16),
                  actionSlot,
                ],
              ],
            );
          }
          return Row(
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              avatar,
              const SizedBox(width: 16),
              Expanded(child: identity),
              if (actionSlot != null) ...[
                const SizedBox(width: 16),
                actionSlot,
              ],
            ],
          );
        },
      ),
    );
  }
}

typedef AppCard = ClinicCard;
typedef HeaderCard = PatientHeaderCard;
typedef StatusPill = PillChip;
typedef CategoryPill = PillChip;
typedef SuggestionChipRow = SuggestionChips;
typedef PillTextField = PillSendField;

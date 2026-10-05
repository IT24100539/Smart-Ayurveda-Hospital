import 'package:flutter/material.dart';

import '../../theme/app_theme.dart';
import 'clinic_widgets.dart';

class SectionRow extends StatelessWidget {
  const SectionRow({required this.title, this.actionLabel, this.onAction, super.key});

  final String title;
  final String? actionLabel;
  final VoidCallback? onAction;

  @override
  Widget build(BuildContext context) {
    final brand = AyurvedaThemeExtension.of(context);
    return Row(
      crossAxisAlignment: CrossAxisAlignment.end,
      children: [
        Expanded(child: Text(title, style: Theme.of(context).textTheme.titleLarge)),
        if (actionLabel != null)
          TextButton(
            onPressed: onAction,
            child: Text('$actionLabel →', style: TextStyle(color: brand.teal)),
          ),
      ],
    );
  }
}

class InfoCard extends StatelessWidget {
  const InfoCard({
    required this.kicker,
    required this.title,
    required this.status,
    required this.facts,
    this.action,
    super.key,
  });

  final String kicker;
  final String title;
  final String status;
  final List<(String, String)> facts;
  final Widget? action;

  @override
  Widget build(BuildContext context) {
    return ClinicCard(
      padding: const EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(child: Text(kicker.toUpperCase(), style: AyurvedaType.eyebrow(context))),
              StatusMark(status: status),
            ],
          ),
          const SizedBox(height: 12),
          Text(title, style: Theme.of(context).textTheme.titleMedium),
          const Divider(height: 24),
          for (final fact in facts) ...[
            Text(fact.$1.toUpperCase(), style: AyurvedaType.eyebrow(context)),
            const SizedBox(height: 4),
            Text(fact.$2, style: Theme.of(context).textTheme.bodyMedium),
            const SizedBox(height: 12),
          ],
          if (action != null) Align(alignment: Alignment.centerRight, child: action!),
        ],
      ),
    );
  }
}

class StatusMark extends StatelessWidget {
  const StatusMark({required this.status, super.key});

  final String status;

  @override
  Widget build(BuildContext context) {
    final brand = AyurvedaThemeExtension.of(context);
    final (IconData icon, Color background, Color foreground) = switch (status) {
      'Approved' => (Icons.check, brand.approvedBackground, brand.approvedForeground),
      'Pending' => (Icons.schedule, brand.pendingBackground, brand.pendingForeground),
      'Completed' => (Icons.check, brand.completedBackground, brand.completedForeground),
      'Rejected' || 'Cancelled' => (
          Icons.close,
          brand.terracottaBackground,
          brand.terracottaAccent,
        ),
      _ => (Icons.circle, brand.neutralBackground, brand.neutralForeground),
    };
    return PillChip(label: status, icon: icon, background: background, foreground: foreground);
  }
}

class BookingOptionCard extends StatelessWidget {
  const BookingOptionCard({
    required this.name,
    required this.price,
    required this.description,
    required this.duration,
    required this.category,
    this.selected = false,
    super.key,
  });

  final String name;
  final String price;
  final String description;
  final String duration;
  final String category;
  final bool selected;

  @override
  Widget build(BuildContext context) {
    final brand = AyurvedaThemeExtension.of(context);
    return DecoratedBox(
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(brand.cardRadius),
        border: Border.all(color: selected ? brand.teal : brand.cardBorderColor, width: selected ? 2 : 1),
      ),
      child: ClinicCard(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(child: Text(name, style: Theme.of(context).textTheme.titleMedium)),
                Text(price, style: AyurvedaType.price(context)),
              ],
            ),
            const SizedBox(height: 8),
            Text(description, style: Theme.of(context).textTheme.bodyMedium),
            const SizedBox(height: 12),
            Row(
              children: [
                Icon(Icons.schedule, size: 16, color: Theme.of(context).colorScheme.onSurfaceVariant),
                const SizedBox(width: 6),
                Expanded(child: Text(duration, style: Theme.of(context).textTheme.bodySmall)),
                PillChip(label: category),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class KpiCard extends StatelessWidget {
  const KpiCard({required this.label, required this.value, super.key});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return ClinicCard(
      padding: const EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label.toUpperCase(), style: AyurvedaType.eyebrow(context)),
          const SizedBox(height: 8),
          Text(value, style: Theme.of(context).textTheme.displayMedium),
        ],
      ),
    );
  }
}

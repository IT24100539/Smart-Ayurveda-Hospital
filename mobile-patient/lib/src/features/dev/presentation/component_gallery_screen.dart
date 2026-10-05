import 'package:flutter/material.dart';

import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/clinic_widgets.dart';
import '../../../shared/widgets/design_recipes.dart';
import '../../../shared/widgets/empty_state.dart';
import '../../../theme/app_theme.dart';

/// Debug-only catalogue. The route is registered only when [kDebugMode] is true.
class ComponentGalleryScreen extends StatelessWidget {
  const ComponentGalleryScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final brand = AyurvedaThemeExtension.of(context);
    return Scaffold(
      appBar: AppBar(title: Text(l10n.devGalleryTitle)),
      body: ListView(
        padding: const EdgeInsets.all(24),
        children: [
          Text(l10n.devGalleryHint),
          const SizedBox(height: 24),
          PatientHeaderCard(
            name: l10n.devGallerySampleName,
            uhid: l10n.devGallerySampleUhid,
            subtitle: l10n.devGalleryHint,
          ),
          const SizedBox(height: 24),
          SectionRow(title: l10n.devGallerySection, actionLabel: l10n.devGalleryOpen),
          const SizedBox(height: 12),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: const [
              StatusMark(status: 'Approved'),
              StatusMark(status: 'Pending'),
              StatusMark(status: 'Rejected'),
              StatusMark(status: 'Cancelled'),
              StatusMark(status: 'Completed'),
            ],
          ),
          const SizedBox(height: 24),
          NumberedStepper(
            labels: [
              l10n.devGalleryStepTherapy,
              l10n.devGalleryStepDate,
              l10n.devGalleryStepTime,
              l10n.devGalleryStepConfirm,
            ],
            current: 1,
          ),
          const SizedBox(height: 24),
          InfoCard(
            kicker: l10n.devGalleryKicker,
            title: l10n.devGallerySampleName,
            status: 'Pending',
            facts: [(l10n.devGalleryFact, l10n.devGalleryFactValue)],
          ),
          const SizedBox(height: 16),
          BookingOptionCard(
            name: l10n.devGallerySampleName,
            price: l10n.devGalleryPrice,
            description: l10n.devGalleryHint,
            duration: l10n.devGalleryDuration,
            category: l10n.devGalleryCategory,
            selected: true,
          ),
          const SizedBox(height: 16),
          KpiCard(label: l10n.devGalleryKpi, value: '—'),
          const SizedBox(height: 16),
          SuggestionChips(labels: [l10n.devGallerySampleName], onSelected: (_) {}),
          const SizedBox(height: 16),
          EmptyState(message: l10n.devGalleryEmpty, detail: l10n.devGalleryHint, compact: true),
          const SizedBox(height: 16),
          ErrorLine(message: l10n.devGalleryError),
          const SizedBox(height: 8),
          Text('Aa', style: TextStyle(color: brand.teal, fontFamily: AyurvedaFonts.serif)),
        ],
      ),
    );
  }
}

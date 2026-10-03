import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../l10n/app_localizations.dart';
import '../../../l10n/feature_localizations.dart';
import '../../../shared/widgets/clinic_widgets.dart';
import '../../../shared/widgets/empty_state.dart';
import '../../../shared/widgets/page_layout.dart';
import '../../../shared/widgets/responsive_columns.dart';
import '../../../shared/widgets/skeleton.dart';
import '../../../theme/app_theme.dart';
import '../../feedback/application/communication_providers.dart';
import '../../feedback/domain/communication_models.dart';
import '../../feedback/presentation/star_rating.dart';

abstract final class ContactLocationKeys {
  static const hospitalName = ValueKey('contact-hospital-name');
  static const hospitalAddress = ValueKey('contact-hospital-address');
  static const hospitalPhone = ValueKey('contact-hospital-phone');
  static const hospitalHours = ValueKey('contact-hospital-hours');
  static const testimonialsSection = ValueKey('contact-testimonials-section');
  static const testimonialsRetry = ValueKey('contact-testimonials-retry');
}

class ContactLocationScreen extends ConsumerWidget {
  const ContactLocationScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final copy = FeatureLocalizations.of(context);
    final theme = Theme.of(context);
    final brand = AyurvedaThemeExtension.of(context);
    final publicFeedbackAsync = ref.watch(publicFeedProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(copy.text('Contact & Location', 'සම්බන්ධතා සහ ස්ථානය')),
      ),
      body: PageScrollView(
        children: [
          _FacilityHeader(
            kicker: copy.text('Ayurvedic Center', 'ආයුර්වේද මධ්‍යස්ථානය'),
            name: l10n.homeHospitalName,
          ),
          const SizedBox(height: 24),
          Text(
            copy.text('Hospital Information', 'රෝහල් තොරතුරු'),
            style: theme.textTheme.titleMedium,
          ),
          const SizedBox(height: 14),
          ResponsiveColumns(
            spacing: 10,
            children: [
              _InfoTile(
                icon: Icons.location_on_outlined,
                label: copy.text('Address', 'ලිපිනය'),
                value: l10n.homeHospitalAddress,
                key: ContactLocationKeys.hospitalAddress,
              ),
              _InfoTile(
                icon: Icons.phone_outlined,
                label: copy.text('Phone', 'දුරකථන අංකය'),
                value: l10n.homeHospitalPhone,
                key: ContactLocationKeys.hospitalPhone,
              ),
              _InfoTile(
                icon: Icons.access_time_outlined,
                label: copy.text('Hours', 'වේලාවන්'),
                value: l10n.homeHospitalHours,
                key: ContactLocationKeys.hospitalHours,
              ),
            ],
          ),
          const SizedBox(height: 28),

          // Testimonials Section
          Row(
            children: [
              Icon(Icons.rate_review_outlined, size: 20, color: brand.teal),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  l10n.publicFeedTitle,
                  style: theme.textTheme.titleMedium,
                ),
              ),
            ],
          ),
          const SizedBox(height: 6),
          Text(
            copy.text(
              'Approved feedback from hospital patients.',
              'රෝහල් රෝගීන්ගෙන් ලැබුණු අනුමත ප්‍රතිචාර.',
            ),
            style: theme.textTheme.bodySmall?.copyWith(
              color: theme.colorScheme.onSurfaceVariant,
            ),
          ),
          const SizedBox(height: 14),

          // Testimonials Content from GET /api/feedback
          publicFeedbackAsync.when(
            data: (feedbackList) {
              if (feedbackList.isEmpty) {
                return KeyedSubtree(
                  key: ContactLocationKeys.testimonialsSection,
                  child: EmptyState(
                    compact: true,
                    message: l10n.publicFeedEmpty,
                    detail: copy.text(
                      'Patient reviews submitted through the app are published here once approved.',
                      'යෙදුම හරහා යොමු කරන ප්‍රතිචාර අනුමත වූ පසු මෙහි දැක්වේ.',
                    ),
                    icon: Icons.chat_bubble_outline,
                  ),
                );
              }

              return KeyedSubtree(
                key: ContactLocationKeys.testimonialsSection,
                child: ResponsiveColumns(
                  children: [
                    for (final fb in feedbackList) _TestimonialCard(feedback: fb),
                  ],
                ),
              );
            },
            loading: () => KeyedSubtree(
              key: ContactLocationKeys.testimonialsSection,
              child: const SkeletonScope(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    SkeletonCard(lines: 2),
                    SizedBox(height: 12),
                    SkeletonCard(lines: 2),
                  ],
                ),
              ),
            ),
            error: (error, stackTrace) => KeyedSubtree(
              key: ContactLocationKeys.testimonialsSection,
              child: ErrorState(
                compact: true,
                message: copy.text(
                  'Feedback is currently unavailable.',
                  'ප්‍රතිචාර මේ අවස්ථාවේ ලබාගත නොහැක.',
                ),
                actionLabel: l10n.retry,
                actionKey: ContactLocationKeys.testimonialsRetry,
                onAction: () => ref.invalidate(publicFeedProvider),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

/// Photo header with the hospital name. Rounded like the other banners.
class _FacilityHeader extends StatelessWidget {
  const _FacilityHeader({required this.kicker, required this.name});

  final String kicker;
  final String name;

  @override
  Widget build(BuildContext context) {
    final brand = AyurvedaThemeExtension.of(context);
    return LayoutBuilder(
      builder: (context, constraints) {
        final height = constraints.maxWidth >= 600 ? 260.0 : 200.0;
        return Container(
          height: height,
          clipBehavior: Clip.antiAlias,
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(brand.headerRadius),
            border: Border.all(color: brand.cardBorderColor),
          ),
          child: Stack(
            fit: StackFit.expand,
            children: [
              Image.asset(
                'assets/images/herbal-garden.png',
                fit: BoxFit.cover,
                errorBuilder: (context, error, stackTrace) => Container(
                  color: Theme.of(context).colorScheme.primaryContainer,
                  child: Center(
                    child: Icon(
                      Icons.local_hospital_outlined,
                      size: 64,
                      color: brand.teal,
                    ),
                  ),
                ),
              ),
              DecoratedBox(
                decoration: BoxDecoration(
                  gradient: LinearGradient(
                    begin: Alignment.topCenter,
                    end: Alignment.bottomCenter,
                    colors: [
                      AyurvedaColors.scrim.withValues(alpha: 0.1),
                      AyurvedaColors.scrim.withValues(alpha: 0.78),
                    ],
                  ),
                ),
              ),
              Positioned(
                bottom: 18,
                left: 20,
                right: 20,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    PillChip(
                      label: kicker,
                      background: brand.headerGradientStart,
                      foreground: brand.onHeader,
                    ),
                    const SizedBox(height: 8),
                    Text(
                      name,
                      key: ContactLocationKeys.hospitalName,
                      style: Theme.of(context).textTheme.titleLarge?.copyWith(
                        color: AyurvedaColors.onScrim,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}

class _InfoTile extends StatelessWidget {
  const _InfoTile({
    required this.icon,
    required this.label,
    required this.value,
    super.key,
  });

  final IconData icon;
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final brand = AyurvedaThemeExtension.of(context);
    return ClinicCard(
      padding: const EdgeInsets.all(14),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: theme.colorScheme.primaryContainer,
              shape: BoxShape.circle,
            ),
            child: Icon(icon, size: 20, color: brand.teal),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(label.toUpperCase(), style: AyurvedaType.eyebrow(context)),
                const SizedBox(height: 3),
                Text(
                  value,
                  style: theme.textTheme.bodyMedium?.copyWith(
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _TestimonialCard extends StatelessWidget {
  const _TestimonialCard({required this.feedback});

  final PublicFeedback feedback;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final copy = FeatureLocalizations.of(context);

    return ClinicCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              StarDisplay(rating: feedback.rating, size: 18),
              const SizedBox(width: 12),
              Expanded(
                child: Text(
                  feedback.isAnonymous || feedback.patientName.isEmpty
                      ? copy.text('Anonymous Patient', 'නිර්නාමික රෝගියා')
                      : feedback.patientName,
                  textAlign: TextAlign.end,
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.bold,
                    color: theme.colorScheme.onSurfaceVariant,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          Text(
            feedback.comment,
            style: theme.textTheme.bodyMedium?.copyWith(height: 1.4),
          ),
        ],
      ),
    );
  }
}

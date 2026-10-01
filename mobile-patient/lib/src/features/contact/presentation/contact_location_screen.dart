import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../l10n/app_localizations.dart';
import '../../../l10n/feature_localizations.dart';
import '../../../shared/widgets/empty_state.dart';
import '../../../theme/app_theme.dart';
import '../../feedback/application/communication_providers.dart';
import '../../feedback/domain/communication_models.dart';

abstract final class ContactLocationKeys {
  static const hospitalName = ValueKey('contact-hospital-name');
  static const hospitalAddress = ValueKey('contact-hospital-address');
  static const hospitalPhone = ValueKey('contact-hospital-phone');
  static const hospitalHours = ValueKey('contact-hospital-hours');
  static const testimonialsSection = ValueKey('contact-testimonials-section');
}

class ContactLocationScreen extends ConsumerWidget {
  const ContactLocationScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final copy = FeatureLocalizations.of(context);
    final theme = Theme.of(context);
    final publicFeedbackAsync = ref.watch(publicFeedProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(copy.text('Contact & Location', 'සම්බන්ධතා සහ ස්ථානය')),
      ),
      body: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Facility Hero Header Image
            Stack(
              children: [
                SizedBox(
                  height: 200,
                  width: double.infinity,
                  child: Image.asset(
                    'assets/images/herbal-garden.png',
                    fit: BoxFit.cover,
                    errorBuilder: (context, error, stackTrace) => Container(
                      color: AyurvedaColors.sageMuted,
                      child: const Center(
                        child: Icon(
                          Icons.local_hospital_outlined,
                          size: 64,
                          color: AyurvedaColors.forest,
                        ),
                      ),
                    ),
                  ),
                ),
                Container(
                  height: 200,
                  decoration: BoxDecoration(
                    gradient: LinearGradient(
                      begin: Alignment.topCenter,
                      end: Alignment.bottomCenter,
                      colors: [
                        Colors.black.withValues(alpha: 0.1),
                        Colors.black.withValues(alpha: 0.75),
                      ],
                    ),
                  ),
                ),
                Positioned(
                  bottom: 16,
                  left: 20,
                  right: 20,
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 10,
                          vertical: 4,
                        ),
                        decoration: BoxDecoration(
                          color: AyurvedaColors.forest,
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: Text(
                          copy.text('Ayurvedic Center', 'ආයුර්වේද මධ්‍යස්ථානය'),
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 11,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                      const SizedBox(height: 6),
                      Text(
                        l10n.homeHospitalName,
                        key: ContactLocationKeys.hospitalName,
                        style: theme.textTheme.titleLarge?.copyWith(
                          color: Colors.white,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),

            Padding(
              padding: const EdgeInsets.all(20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    copy.text('Hospital Information', 'රෝහල් තොරතුරු'),
                    style: theme.textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.bold,
                      color: AyurvedaColors.forest,
                    ),
                  ),
                  const SizedBox(height: 14),

                  // Address Card
                  _InfoTile(
                    icon: Icons.location_on_outlined,
                    label: copy.text('Address', 'ලිපිනය'),
                    value: l10n.homeHospitalAddress,
                    key: ContactLocationKeys.hospitalAddress,
                  ),
                  const SizedBox(height: 10),

                  // Phone Card
                  _InfoTile(
                    icon: Icons.phone_outlined,
                    label: copy.text('Phone', 'දුරකථන අංකය'),
                    value: l10n.homeHospitalPhone,
                    key: ContactLocationKeys.hospitalPhone,
                  ),
                  const SizedBox(height: 10),

                  // Hours Card
                  _InfoTile(
                    icon: Icons.access_time_outlined,
                    label: copy.text('Hours', 'වේලාවන්'),
                    value: l10n.homeHospitalHours,
                    key: ContactLocationKeys.hospitalHours,
                  ),
                  const SizedBox(height: 28),

                  // Testimonials Section
                  Row(
                    children: [
                      const Icon(
                        Icons.rate_review_outlined,
                        size: 20,
                        color: AyurvedaColors.forest,
                      ),
                      const SizedBox(width: 8),
                      Text(
                        l10n.publicFeedTitle,
                        style: theme.textTheme.titleMedium?.copyWith(
                          fontWeight: FontWeight.bold,
                          color: AyurvedaColors.forest,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 6),
                  Text(
                    copy.text(
                      'Real approved feedback from hospital patients.',
                      'රෝහල් රෝගීන්ගෙන් ලැබුණු අනුමත සැබෑ ප්‍රතිචාර.',
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
                        return Container(
                          key: ContactLocationKeys.testimonialsSection,
                          padding: const EdgeInsets.symmetric(
                            horizontal: 16,
                            vertical: 24,
                          ),
                          decoration: BoxDecoration(
                            color: theme.colorScheme.surfaceContainerLow,
                            borderRadius: BorderRadius.circular(16),
                            border: Border.all(
                              color: theme.colorScheme.outlineVariant.withValues(
                                alpha: 0.4,
                              ),
                            ),
                          ),
                          child: EmptyState(
                            message: l10n.publicFeedEmpty,
                            detail: copy.text(
                              'Patient reviews submitted through the app are published here once approved.',
                              'යෙදුම හරහා යොමු කරන ප්‍රතිචාර අනුමත වූ පසු මෙහි දැක්වේ.',
                            ),
                            icon: Icons.chat_bubble_outline,
                          ),
                        );
                      }

                      return Column(
                        key: ContactLocationKeys.testimonialsSection,
                        children: feedbackList
                            .map((fb) => _TestimonialCard(feedback: fb))
                            .toList(),
                      );
                    },
                    loading: () => const Center(
                      child: Padding(
                        padding: EdgeInsets.all(24),
                        child: CircularProgressIndicator(strokeWidth: 2),
                      ),
                    ),
                    error: (error, stackTrace) => Container(
                      key: ContactLocationKeys.testimonialsSection,
                      padding: const EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        color: theme.colorScheme.surfaceContainerLow,
                        borderRadius: BorderRadius.circular(16),
                      ),
                      child: Text(
                        copy.text(
                          'Feedback is currently unavailable.',
                          'ප්‍රතිචාර මේ අවස්ථාවේ ලබාගත නොහැක.',
                        ),
                        style: TextStyle(
                          color: theme.colorScheme.onSurfaceVariant,
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
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
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: theme.colorScheme.surfaceContainerLow,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(
          color: theme.colorScheme.outlineVariant.withValues(alpha: 0.4),
        ),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: AyurvedaColors.forest.withValues(alpha: 0.1),
              shape: BoxShape.circle,
            ),
            child: Icon(icon, size: 20, color: AyurvedaColors.forest),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: TextStyle(
                    fontSize: 12,
                    color: theme.colorScheme.onSurfaceVariant,
                    fontWeight: FontWeight.w500,
                  ),
                ),
                const SizedBox(height: 2),
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

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      elevation: 0,
      color: theme.colorScheme.surfaceContainerLow,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(14),
        side: BorderSide(
          color: theme.colorScheme.outlineVariant.withValues(alpha: 0.4),
        ),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Row(
                  children: List.generate(
                    5,
                    (index) => Icon(
                      index < feedback.rating
                          ? Icons.star_rounded
                          : Icons.star_outline_rounded,
                      size: 18,
                      color: AyurvedaColors.gold,
                    ),
                  ),
                ),
                Text(
                  feedback.isAnonymous || feedback.patientName.isEmpty
                      ? copy.text('Anonymous Patient', 'නිර්නාමික රෝගියා')
                      : feedback.patientName,
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.bold,
                    color: theme.colorScheme.onSurfaceVariant,
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
      ),
    );
  }
}

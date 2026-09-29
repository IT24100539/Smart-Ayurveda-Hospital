import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../l10n/app_localizations.dart';
import '../../../l10n/feature_localizations.dart';
import '../../../shared/widgets/empty_state.dart';
import '../../../shared/widgets/section_banner.dart';
import '../../auth/application/auth_controller.dart';
import '../../treatments/application/treatments_provider.dart';
import '../../../router/app_routes.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final featureCopy = FeatureLocalizations.of(context);
    final user = ref.watch(authControllerProvider).user;
    final firstName = user?.firstName ?? '';
    final treatments = ref.watch(treatmentsProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.appTitle)),
      body: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
          if (treatments.isLoading)
            const Padding(
              padding: EdgeInsets.only(bottom: 12),
              child: LinearProgressIndicator(),
            ),
          if (treatments.hasError)
            Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: ErrorState(
                message: l10n.homeLoadError,
                actionLabel: l10n.retry,
                onAction: () => ref.invalidate(treatmentsProvider),
              ),
            ),
            ClipRRect(
              borderRadius: BorderRadius.circular(20),
              child: Image.asset(
                'assets/images/ayurveda-courtyard.png',
                height: 160,
                width: double.infinity,
                fit: BoxFit.cover,
                errorBuilder: (context, _, _) => Container(
                  height: 160,
                  color: Theme.of(context).colorScheme.surfaceContainerHighest,
                  alignment: Alignment.center,
                  child: Icon(
                    Icons.spa_outlined,
                    size: 48,
                    color: Theme.of(context).colorScheme.primary,
                  ),
                ),
              ),
            ),
            const SizedBox(height: 16),
            SectionBanner(
              kicker: featureCopy.text('Hospital', 'රෝහල'),
              title: firstName.isEmpty
                  ? l10n.homeGreetingGeneric
                  : l10n.homeGreeting(firstName),
              body: l10n.homeSubtitle,
            ),
            _HomeCard(
              icon: Icons.spa_outlined,
              title: l10n.navTreatments,
              body: l10n.treatmentsPlaceholder,
              onTap: () => context.go(AppRoutes.treatments),
            ),
            const SizedBox(height: 12),
            _HomeCard(
              icon: Icons.event_available_outlined,
              title: featureCopy.bookAppointment,
              body: l10n.appointmentsPlaceholder,
              onTap: () => context.go(AppRoutes.treatments),
            ),
            const SizedBox(height: 12),
            _HomeCard(
              icon: Icons.hotel_outlined,
              title: featureCopy.wardAvailability,
              body: featureCopy.text(
                'View occupancy and request admission for staff review.',
                'වාට්ටු භාවිතය බලා කාර්ය මණ්ඩල සමාලෝචනය සඳහා ඇතුළත් වීමක් ඉල්ලන්න.',
              ),
              onTap: () => context.push(AppRoutes.wards),
            ),
            const SizedBox(height: 12),
            _HomeCard(
              icon: Icons.reviews_outlined,
              title: l10n.navFeedback,
              body: l10n.publicFeedTitle,
              onTap: () => context.go(AppRoutes.feedback),
            ),
            const SizedBox(height: 16),
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      l10n.homeHospitalName,
                      style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Text(l10n.homeHospitalAddress),
                    const SizedBox(height: 4),
                    Text(l10n.homeHospitalPhone),
                    const SizedBox(height: 4),
                    Text(l10n.homeHospitalHours),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _HomeCard extends StatelessWidget {
  const _HomeCard({
    required this.icon,
    required this.title,
    required this.body,
    required this.onTap,
  });

  final IconData icon;
  final String title;
  final String body;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Card(
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(16),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(icon, color: theme.colorScheme.primary),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      title,
                      style: theme.textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(height: 6),
                    Text(
                      body,
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

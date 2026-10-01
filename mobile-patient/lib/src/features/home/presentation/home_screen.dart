import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../l10n/app_localizations.dart';
import '../../../l10n/feature_localizations.dart';
import '../../../router/app_routes.dart';
import '../../../shared/widgets/empty_state.dart';
import '../../../shared/widgets/section_banner.dart';
import '../../../theme/app_theme.dart';
import '../../appointments/domain/appointment_models.dart';
import '../../appointments/presentation/appointments_screen.dart';
import '../../auth/application/auth_controller.dart';
import '../../treatments/application/treatments_provider.dart';
import 'widgets/charaka_ai_card.dart';
import 'widgets/featured_therapies_carousel.dart';
import 'widgets/hero_banner.dart';
import 'widgets/hospital_info_card.dart';
import 'widgets/quick_action_tile.dart';
import 'widgets/upcoming_appointment_card.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final featureCopy = FeatureLocalizations.of(context);
    final user = ref.watch(authControllerProvider).user;
    final firstName = user?.firstName ?? '';
    final treatments = ref.watch(treatmentsProvider);
    final appointments = ref.watch(myAppointmentsProvider);
    final theme = Theme.of(context);

    // Feature 1: Upcoming appointment calculation (lowest future date/time with status Pending or Approved)
    final now = DateTime.now();
    final todayStart = DateTime(now.year, now.month, now.day);
    final upcomingList = appointments.valueOrNull
        ?.where((a) =>
            (a.status == AppointmentStatus.pending ||
                a.status == AppointmentStatus.approved) &&
            !a.requestedDate.isBefore(todayStart))
        .toList();
    if (upcomingList != null && upcomingList.length > 1) {
      upcomingList.sort((a, b) => a.requestedDate.compareTo(b.requestedDate));
    }
    final nextAppointment = upcomingList?.firstOrNull;

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.appTitle),
        actions: [
          IconButton(
            icon: const Icon(Icons.notifications_none_outlined),
            tooltip: 'Notifications',
            onPressed: () => context.push('${AppRoutes.feedback}/notifications'),
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 32),
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

            // Hero Sanctuary Card
            HeroBanner(
              onBookPressed: () => context.go(AppRoutes.treatments),
              title: l10n.homeHospitalName,
              subtitle: featureCopy.text(
                'Ayurvedic care and therapies',
                'ආයුර්වේද සත්කාර සහ ප්‍රතිකාර',
              ),
              ctaLabel: featureCopy.bookAppointment,
            ),
            const SizedBox(height: 16),

            // Feature 1: Pinned Upcoming Appointment Card
            UpcomingAppointmentCard(
              appointment: nextAppointment,
              isLoading: appointments.isLoading,
              onTap: () => context.go(AppRoutes.appointments),
              onBookConsultation: () => context.go(AppRoutes.treatments),
            ),
            const SizedBox(height: 16),

            SectionBanner(
              kicker: featureCopy.text('Hospital', 'රෝහල'),
              title: firstName.isEmpty
                  ? l10n.homeGreetingGeneric
                  : l10n.homeGreeting(firstName),
              body: l10n.homeSubtitle,
            ),
            const SizedBox(height: 14),

            // Quick Service Grid (Treatments, Book Visit, Wards & Beds, Feedback)
            GridView.count(
              crossAxisCount: 2,
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              mainAxisSpacing: 12,
              crossAxisSpacing: 12,
              childAspectRatio: 1.45,
              children: [
                QuickActionTile(
                  icon: Icons.spa_outlined,
                  title: l10n.navTreatments,
                  subtitle: l10n.treatmentsPlaceholder,
                  color: AyurvedaColors.forest,
                  onTap: () => context.go(AppRoutes.treatments),
                ),
                QuickActionTile(
                  icon: Icons.event_available_outlined,
                  title: featureCopy.bookAppointment,
                  subtitle: l10n.appointmentsPlaceholder,
                  color: AyurvedaColors.gold,
                  onTap: () => context.go(AppRoutes.treatments),
                ),
                QuickActionTile(
                  icon: Icons.hotel_outlined,
                  title: featureCopy.wardAvailability,
                  subtitle: featureCopy.text('Check beds', 'ඇඳන් විමසන්න'),
                  color: AyurvedaColors.sage,
                  onTap: () => context.push(AppRoutes.wards),
                ),
                QuickActionTile(
                  icon: Icons.reviews_outlined,
                  title: l10n.navFeedback,
                  subtitle: l10n.publicFeedTitle,
                  color: AyurvedaColors.terracotta,
                  onTap: () => context.go(AppRoutes.feedback),
                ),
              ],
            ),
            const SizedBox(height: 14),
            CharakaAiCard(
              onTap: () => context.push(AppRoutes.charakaChat),
            ),
            const SizedBox(height: 24),

            // Featured Holistic Therapies Section (Dynamic from treatmentsProvider)
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  featureCopy.text('Featured Therapies', 'විශේෂිත ප්‍රතිකාර'),
                  style: theme.textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.bold,
                    fontFamily: 'serif',
                  ),
                ),
                TextButton(
                  onPressed: () => context.go(AppRoutes.treatments),
                  child: Text(featureCopy.text('View All', 'සියල්ල බලන්න')),
                ),
              ],
            ),
            const SizedBox(height: 10),
            FeaturedTherapiesCarousel(
              treatments: treatments.valueOrNull ?? const [],
              isLoading: treatments.isLoading,
            ),
            const SizedBox(height: 24),

            // Hospital Location & Hours (From existing l10n strings at HEAD)
            const HospitalInfoCard(),
          ],
        ),
      ),
    );
  }
}

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../l10n/app_localizations.dart';
import '../../../l10n/feature_localizations.dart';
import '../../../router/app_routes.dart';
import '../../../shared/widgets/clinic_widgets.dart';
import '../../../shared/widgets/empty_state.dart';
import '../../../shared/widgets/page_layout.dart';
import '../../../shared/widgets/responsive_columns.dart';
import '../../../shared/widgets/section_banner.dart';
import '../../../theme/app_theme.dart';
import '../../appointments/domain/appointment_models.dart';
import '../../appointments/presentation/appointments_screen.dart';
import '../../auth/application/auth_controller.dart';
import '../../feedback/presentation/feedback_keys.dart';
import '../../feedback/presentation/unread_count_badge.dart';
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
    final brand = AyurvedaThemeExtension.of(context);

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
            tooltip: l10n.notificationsTitle,
            onPressed: () => context.push(AppRoutes.notifications),
            icon: const UnreadCountBadge(
              badgeKey: FeedbackKeys.homeUnreadBadge,
              child: Icon(Icons.notifications_none_outlined),
            ),
          ),
        ],
      ),
      body: PageScrollView(
        children: [
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
            hasError: appointments.hasError,
            onRetry: () => ref.invalidate(myAppointmentsProvider),
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
          const SizedBox(height: 2),

          // Quick Service Grid (Treatments, Book Visit, Wards & Beds, Feedback)
          _QuickActions(
            tiles: [
              QuickActionTile(
                icon: Icons.spa_outlined,
                title: l10n.navTreatments,
                subtitle: l10n.treatmentsPlaceholder,
                color: brand.teal,
                onTap: () => context.go(AppRoutes.treatments),
              ),
              QuickActionTile(
                icon: Icons.event_available_outlined,
                title: featureCopy.bookAppointment,
                subtitle: l10n.appointmentsPlaceholder,
                color: brand.goldAccent,
                onTap: () => context.go(AppRoutes.treatments),
              ),
              QuickActionTile(
                icon: Icons.hotel_outlined,
                title: featureCopy.wardAvailability,
                subtitle: featureCopy.text('Check beds', 'ඇඳන් විමසන්න'),
                color: brand.approvedForeground,
                onTap: () => context.push(AppRoutes.wards),
              ),
              QuickActionTile(
                icon: Icons.reviews_outlined,
                title: l10n.navFeedback,
                subtitle: l10n.publicFeedTitle,
                color: brand.terracottaAccent,
                onTap: () => context.go(AppRoutes.feedback),
              ),
            ],
          ),
          const SizedBox(height: 14),
          ResponsiveColumns(
            children: [
              ClinicCard(
                padding: EdgeInsets.zero,
                child: ListTile(
                  contentPadding: const EdgeInsets.symmetric(
                    horizontal: 16,
                    vertical: 6,
                  ),
                  leading: Container(
                    padding: const EdgeInsets.all(10),
                    decoration: BoxDecoration(
                      color: theme.colorScheme.primaryContainer,
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Icon(Icons.groups_outlined, color: brand.teal),
                  ),
                  title: Text(l10n.doctorsTitle, style: theme.textTheme.titleSmall),
                  subtitle: Text(l10n.doctorsNavSubtitle),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => context.push(AppRoutes.doctors),
                ),
              ),
              CharakaAiCard(onTap: () => context.push(AppRoutes.charakaChat)),
            ],
          ),
          const SizedBox(height: 24),

          // Therapies from treatmentsProvider
          Row(
            children: [
              Expanded(
                child: Text(
                  featureCopy.text('Featured Therapies', 'විශේෂිත ප්‍රතිකාර'),
                  style: theme.textTheme.titleMedium,
                ),
              ),
              TextButton(
                onPressed: () => context.go(AppRoutes.treatments),
                child: Text(featureCopy.text('View All', 'සියල්ල බලන්න')),
              ),
            ],
          ),
          const SizedBox(height: 10),
          if (treatments.hasError && !treatments.hasValue)
            ErrorState(
              compact: true,
              message: l10n.homeLoadError,
              actionLabel: l10n.retry,
              onAction: () => ref.invalidate(treatmentsProvider),
            )
          else
            FeaturedTherapiesCarousel(
              treatments: treatments.valueOrNull ?? const [],
              isLoading: treatments.isLoading,
            ),
          const SizedBox(height: 24),

          // Hospital Location & Hours (From existing l10n strings at HEAD)
          const HospitalInfoCard(),
        ],
      ),
    );
  }
}

/// Two columns on phones, four on tablets and wide web windows.
class _QuickActions extends StatelessWidget {
  const _QuickActions({required this.tiles});

  final List<Widget> tiles;

  @override
  Widget build(BuildContext context) {
    final textScale = MediaQuery.textScalerOf(
      context,
    ).scale(1).clamp(1.0, 1.6).toDouble();
    return LayoutBuilder(
      builder: (context, constraints) {
        final columns = constraints.maxWidth >= twoColumnBreakpoint ? 4 : 2;
        return GridView(
          shrinkWrap: true,
          padding: EdgeInsets.zero,
          physics: const NeverScrollableScrollPhysics(),
          gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
            crossAxisCount: columns,
            mainAxisSpacing: 12,
            crossAxisSpacing: 12,
            mainAxisExtent: 112 * textScale,
          ),
          children: tiles,
        );
      },
    );
  }
}

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../core/network/api_exception.dart';
import '../../../router/app_routes.dart';
import '../../../shared/widgets/empty_state.dart';
import '../../../theme/app_theme.dart';
import '../../appointments/domain/appointment_models.dart';
import '../../auth/application/auth_controller.dart';
import '../application/availability_provider.dart';
import '../application/treatments_provider.dart';
import '../domain/treatment_models.dart';

final selectedDateProvider = StateProvider.autoDispose<DateTime?>(
  (ref) => null,
);

class TreatmentDetailScreen extends ConsumerWidget {
  const TreatmentDetailScreen({super.key, required this.treatmentId});

  final String treatmentId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final treatmentsState = ref.watch(treatmentsProvider);

    return treatmentsState.when(
      data: (treatments) {
        final treatment = treatments
            .where((t) => t.id == treatmentId)
            .firstOrNull;
        if (treatment == null) {
          return Scaffold(
            appBar: AppBar(title: const Text('Treatment Details')),
            body: const EmptyState(message: 'Treatment not found.'),
          );
        }
        return _TreatmentDetailView(treatment: treatment);
      },
      loading: () => Scaffold(
        appBar: AppBar(title: const Text('Loading...')),
        body: const Center(child: CircularProgressIndicator()),
      ),
      error: (error, stack) => Scaffold(
        appBar: AppBar(title: const Text('Error')),
        body: ErrorState(message: 'Error loading treatment: $error'),
      ),
    );
  }
}

class _TreatmentDetailView extends ConsumerWidget {
  const _TreatmentDetailView({required this.treatment});

  final Treatment treatment;

  void _handleRequestAppointment(BuildContext context, WidgetRef ref) {
    final authState = ref.read(authControllerProvider);
    if (authState.isAuthenticated) {
      context.push(
        AppRoutes.bookingFor(treatment.id),
        extra: TreatmentBooking(
          id: treatment.id,
          name: treatment.nameEnglish,
        ),
      );
    } else {
      context.push(
        AppRoutes.loginWithReturn(AppRoutes.bookingFor(treatment.id)),
      );
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final selectedDate = ref.watch(selectedDateProvider);

    return Scaffold(
      appBar: AppBar(title: Text(treatment.nameEnglish)),
      body: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            ClipRRect(
              borderRadius: BorderRadius.circular(20),
              child: SizedBox(
                height: 220,
                width: double.infinity,
                child: Stack(
                  fit: StackFit.expand,
                  children: [
                    Image.asset(
                      _treatmentImageAsset(treatment.nameEnglish),
                      fit: BoxFit.cover,
                      errorBuilder: (context, _, _) => Container(
                        color: theme.colorScheme.primaryContainer,
                        child: Icon(
                          Icons.spa_outlined,
                          size: 64,
                          color: theme.colorScheme.primary,
                        ),
                      ),
                    ),
                    DecoratedBox(
                      decoration: BoxDecoration(
                        gradient: LinearGradient(
                          begin: Alignment.topCenter,
                          end: Alignment.bottomCenter,
                          colors: [
                            Colors.transparent,
                            AyurvedaColors.scrim.withValues(alpha: 0.45),
                          ],
                        ),
                      ),
                    ),

                  ],
                ),
              ),
            ),
            const SizedBox(height: 24),

            Text(
              treatment.nameSinhala,
              style: theme.textTheme.headlineMedium?.copyWith(
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              treatment.nameEnglish,
              style: theme.textTheme.titleLarge?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
            const SizedBox(height: 16),

            if (treatment.therapistName != null)
              Text(
                'Therapist: ${treatment.therapistName}',
                style: theme.textTheme.titleMedium,
              ),

            Text(
              'Description',
              style: theme.textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 8),
            Text(treatment.description, style: theme.textTheme.bodyMedium),
            const SizedBox(height: 24),

            Text(
              'Available Days',
              style: theme.textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 8),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: treatment.scheduleDays.map((day) {
                return Chip(label: Text(formatDayTag(day)));
              }).toList(),
            ),
            const SizedBox(height: 32),

            const Divider(),
            const SizedBox(height: 16),

            Text(
              'Check Availability',
              style: theme.textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 16),
            OutlinedButton.icon(
              onPressed: () async {
                final date = await showDatePicker(
                  context: context,
                  initialDate: DateTime.now(),
                  firstDate: DateTime.now(),
                  lastDate: DateTime.now().add(const Duration(days: 90)),
                );
                if (date != null) {
                  ref.read(selectedDateProvider.notifier).state = date;
                }
              },
              icon: const Icon(Icons.calendar_today),
              label: Text(
                selectedDate == null
                    ? 'Select Date'
                    : DateFormat('yyyy-MM-dd').format(selectedDate),
              ),
            ),

            const SizedBox(height: 16),

            if (selectedDate != null)
              Consumer(
                builder: (context, ref, child) {
                  final dateStr = DateFormat('yyyy-MM-dd').format(selectedDate);
                  final availabilityAsync = ref.watch(
                    availabilityProvider((id: treatment.id, date: dateStr)),
                  );

                  return availabilityAsync.when(
                    data: (availability) {
                      if (availability.isAvailable) {
                        return Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            Container(
                              padding: const EdgeInsets.all(12),
                              decoration: BoxDecoration(
                                color: AyurvedaColors.sageMuted,
                                borderRadius: BorderRadius.circular(14),
                              ),
                              child: Row(
                                children: [
                                  const Icon(
                                    Icons.check_circle,
                                    color: AyurvedaColors.forest,
                                  ),
                                  const SizedBox(width: 8),
                                  Expanded(
                                    child: Text(
                                      availability.message ??
                                          'Slots are available on this date.',
                                      style: const TextStyle(
                                        color: AyurvedaColors.forest,
                                        fontWeight: FontWeight.bold,
                                      ),
                                    ),
                                  ),
                                ],
                              ),
                            ),
                            const SizedBox(height: 16),
                            FilledButton(
                              onPressed: () =>
                                  _handleRequestAppointment(context, ref),
                              child: const Text('Request Appointment'),
                            ),
                          ],
                        );
                      } else {
                        return Container(
                          padding: const EdgeInsets.all(12),
                          decoration: BoxDecoration(
                            color: theme.colorScheme.errorContainer,
                            borderRadius: BorderRadius.circular(14),
                          ),
                          child: Row(
                            children: [
                              Icon(
                                Icons.error_outline,
                                color: theme.colorScheme.error,
                              ),
                              const SizedBox(width: 8),
                              Expanded(
                                child: Text(
                                  availability.message ??
                                      'Not available on this date.',
                                  style: TextStyle(
                                    color: theme.colorScheme.onErrorContainer,
                                    fontWeight: FontWeight.bold,
                                  ),
                                ),
                              ),
                            ],
                          ),
                        );
                      }
                    },
                    loading: () =>
                        const Center(child: CircularProgressIndicator()),
                    error: (error, stack) => ErrorState(
                      message: error is ApiException
                          ? (error.message ?? 'Failed to check availability.')
                          : 'Failed to check availability: $error',
                    ),
                  );
                },
              ),
          ],
        ),
      ),
    );
  }
}

String _treatmentImageAsset(String name) {
  final lower = name.toLowerCase();
  if (lower.contains('panchakarma')) return 'assets/images/therapy-panchakarma.png';
  if (lower.contains('shirodhara')) return 'assets/images/therapy-shirodhara.png';
  if (lower.contains('abhyanga')) return 'assets/images/therapy-abhyanga.png';
  if (lower.contains('herbal') || lower.contains('medicine') || lower.contains('apothecary')) {
    return 'assets/images/therapy-herbal-medicine.png';
  }
  if (lower.contains('yoga') || lower.contains('meditation')) return 'assets/images/therapy-yoga.png';
  if (lower.contains('diet') || lower.contains('nutrition')) return 'assets/images/therapy-nutrition.png';
  return 'assets/images/panchakarma-room.png';
}

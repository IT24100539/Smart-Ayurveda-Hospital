import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../theme/app_theme.dart';
import '../../auth/application/auth_controller.dart';
import '../application/availability_provider.dart';
import '../application/treatments_provider.dart';
import '../domain/treatment_models.dart';

final selectedDateProvider = StateProvider.autoDispose<DateTime?>((ref) => null);

class TreatmentDetailScreen extends ConsumerWidget {
  const TreatmentDetailScreen({
    super.key,
    required this.treatmentId,
  });

  final String treatmentId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final treatmentsState = ref.watch(treatmentsProvider);

    return treatmentsState.when(
      data: (treatments) {
        final treatment = treatments.where((t) => t.id == treatmentId).firstOrNull;
        if (treatment == null) {
          return Scaffold(
            appBar: AppBar(title: const Text('Treatment Details')),
            body: const Center(child: Text('Treatment not found.')),
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
        body: Center(child: Text('Error loading treatment: $error')),
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
      context.push('/appointments/book/${treatment.id}');
    } else {
      context.push('/login?returnPath=/treatments/${treatment.id}');
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final selectedDate = ref.watch(selectedDateProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(treatment.nameEnglish),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Photo placeholder
            Container(
              height: 200,
              decoration: BoxDecoration(
                color: theme.colorScheme.surfaceContainerHighest,
                borderRadius: BorderRadius.circular(16),
              ),
              child: Center(
                child: Icon(
                  Icons.image_outlined,
                  size: 64,
                  color: theme.colorScheme.onSurfaceVariant,
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
            Text(
              treatment.description,
              style: theme.textTheme.bodyMedium,
            ),
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
                return Chip(
                  label: Text(formatDayTag(day)),
                );
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
                                color: Colors.green.withOpacity(0.1),
                                borderRadius: BorderRadius.circular(8),
                              ),
                              child: Row(
                                children: [
                                  const Icon(Icons.check_circle, color: Colors.green),
                                  const SizedBox(width: 8),
                                  Expanded(
                                    child: Text(
                                      availability.message ?? 'Available for booking!',
                                      style: const TextStyle(color: Colors.green, fontWeight: FontWeight.bold),
                                    ),
                                  ),
                                ],
                              ),
                            ),
                            const SizedBox(height: 16),
                            FilledButton(
                              onPressed: () => _handleRequestAppointment(context, ref),
                              child: const Text('Request Appointment'),
                            ),
                          ],
                        );
                      } else {
                        return Container(
                          padding: const EdgeInsets.all(12),
                          decoration: BoxDecoration(
                            color: theme.colorScheme.errorContainer,
                            borderRadius: BorderRadius.circular(8),
                          ),
                          child: Row(
                            children: [
                              Icon(Icons.error_outline, color: theme.colorScheme.error),
                              const SizedBox(width: 8),
                              Expanded(
                                child: Text(
                                  availability.message ?? 'Not available on this date.',
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
                    loading: () => const Center(child: CircularProgressIndicator()),
                    error: (error, stack) => Text('Failed to check availability: $error'),
                  );
                },
              ),
          ],
        ),
      ),
    );
  }
}

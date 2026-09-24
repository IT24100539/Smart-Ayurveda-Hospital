import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../theme/app_theme.dart';
import '../../../l10n/feature_localizations.dart';
import '../../../router/app_routes.dart';
import '../data/appointment_repository.dart';
import '../domain/appointment_models.dart';

final myAppointmentsProvider = FutureProvider.autoDispose<List<Appointment>>(
  (ref) => ref.watch(appointmentRepositoryProvider).mine(),
);

abstract final class AppointmentStatusColors {
  static Color background(AppointmentStatus status) => switch (status) {
    AppointmentStatus.pending => const Color(0xFFFFE7B3),
    AppointmentStatus.approved => const Color(0xFFD9EAD3),
    AppointmentStatus.rejected => const Color(0xFFF4D6D2),
    AppointmentStatus.completed => const Color(0xFFD8E7F3),
    AppointmentStatus.cancelled => const Color(0xFFE3E3E3),
  };

  static Color foreground(AppointmentStatus status) => switch (status) {
    AppointmentStatus.pending => const Color(0xFF7A5100),
    AppointmentStatus.approved => AyurvedaColors.forest,
    AppointmentStatus.rejected => AyurvedaColors.danger,
    AppointmentStatus.completed => const Color(0xFF205477),
    AppointmentStatus.cancelled => const Color(0xFF555555),
  };
}

class AppointmentStatusChip extends StatelessWidget {
  const AppointmentStatusChip({required this.status, super.key});
  final AppointmentStatus status;

  @override
  Widget build(BuildContext context) => Chip(
    key: ValueKey('status-chip-${status.name}'),
    label: Text(FeatureLocalizations.of(context).status(status.name)),
    backgroundColor: AppointmentStatusColors.background(status),
    labelStyle: TextStyle(
      color: AppointmentStatusColors.foreground(status),
      fontWeight: FontWeight.w700,
    ),
    side: BorderSide.none,
    visualDensity: VisualDensity.compact,
  );
}

class MyAppointmentsScreen extends ConsumerWidget {
  const MyAppointmentsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final appointments = ref.watch(myAppointmentsProvider);
    final copy = FeatureLocalizations.of(context);
    return Scaffold(
      appBar: AppBar(title: Text(copy.myAppointments)),
      body: RefreshIndicator(
        onRefresh: () => ref.refresh(myAppointmentsProvider.future),
        child: appointments.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (error, _) => ListView(
            children: [
              const SizedBox(height: 120),
              const Icon(Icons.cloud_off_outlined, size: 48),
              Padding(
                padding: const EdgeInsets.all(20),
                child: Text(
                  copy.loadAppointmentsError(error),
                  textAlign: TextAlign.center,
                ),
              ),
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 40),
                child: FilledButton(
                  onPressed: () => ref.invalidate(myAppointmentsProvider),
                  child: Text(copy.tryAgain),
                ),
              ),
            ],
          ),
          data: (items) => items.isEmpty
              ? ListView(
                  padding: const EdgeInsets.symmetric(horizontal: 32),
                  children: [
                    const SizedBox(height: 130),
                    const Icon(Icons.event_note_outlined, size: 56),
                    const SizedBox(height: 12),
                    Text(copy.noAppointments, textAlign: TextAlign.center),
                    const SizedBox(height: 8),
                    Text(
                      copy.chooseTreatmentFirst,
                      textAlign: TextAlign.center,
                    ),
                    const SizedBox(height: 20),
                    FilledButton.icon(
                      key: const ValueKey('empty-book-appointment'),
                      onPressed: () => context.go(AppRoutes.treatments),
                      icon: const Icon(Icons.add),
                      label: Text(copy.bookNewAppointment),
                    ),
                  ],
                )
              : ListView.separated(
                  padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
                  itemCount: items.length,
                  separatorBuilder: (_, _) => const SizedBox(height: 12),
                  itemBuilder: (context, index) => _AppointmentCard(
                    appointment: items[index],
                    onTap: () => _showDetails(context, ref, items[index]),
                  ),
                ),
        ),
      ),
    );
  }

  void _showDetails(
    BuildContext context,
    WidgetRef ref,
    Appointment appointment,
  ) {
    final copy = FeatureLocalizations.of(context);
    showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      builder: (sheetContext) => Padding(
        padding: const EdgeInsets.fromLTRB(24, 4, 24, 28),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    appointment.treatmentName,
                    style: Theme.of(context).textTheme.titleLarge?.copyWith(
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
                AppointmentStatusChip(status: appointment.status),
              ],
            ),
            const SizedBox(height: 18),
            ListTile(
              contentPadding: EdgeInsets.zero,
              leading: const Icon(Icons.calendar_today_outlined),
              title: Text(copy.dateLabel),
              subtitle: Text(
                copy.date('EEEE, d MMMM yyyy', appointment.requestedDate),
              ),
            ),
            ListTile(
              contentPadding: EdgeInsets.zero,
              leading: const Icon(Icons.schedule),
              title: Text(copy.timeLabel),
              subtitle: Text(appointment.requestedTimeSlot),
            ),
            if (_canCancel(appointment.status)) ...[
              const SizedBox(height: 10),
              OutlinedButton.icon(
                style: OutlinedButton.styleFrom(
                  foregroundColor: Theme.of(context).colorScheme.error,
                  side: BorderSide(color: Theme.of(context).colorScheme.error),
                ),
                onPressed: () async {
                  Navigator.pop(sheetContext);
                  await _confirmCancel(context, ref, appointment);
                },
                icon: const Icon(Icons.cancel_outlined),
                label: Text(copy.cancelAppointment),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Future<void> _confirmCancel(
    BuildContext context,
    WidgetRef ref,
    Appointment appointment,
  ) async {
    final copy = FeatureLocalizations.of(context);
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(copy.cancelQuestion),
        content: Text(copy.cancelExplanation(appointment.treatmentName)),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialogContext, false),
            child: Text(copy.keep),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(dialogContext, true),
            child: Text(copy.cancelAppointment),
          ),
        ],
      ),
    );
    if (confirmed != true || !context.mounted) return;
    try {
      await ref.read(appointmentRepositoryProvider).cancel(appointment.id);
      ref.invalidate(myAppointmentsProvider);
      if (context.mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(copy.appointmentCancelled)));
      }
    } catch (error) {
      if (context.mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(copy.cancelError(error))));
      }
    }
  }

  static bool _canCancel(AppointmentStatus status) =>
      status == AppointmentStatus.pending ||
      status == AppointmentStatus.approved;
}

class _AppointmentCard extends StatelessWidget {
  const _AppointmentCard({required this.appointment, required this.onTap});
  final Appointment appointment;
  final VoidCallback onTap;
  @override
  Widget build(BuildContext context) {
    final copy = FeatureLocalizations.of(context);
    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(16),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: Text(
                      appointment.treatmentName,
                      style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                  AppointmentStatusChip(status: appointment.status),
                ],
              ),
              const SizedBox(height: 10),
              Text(
                '${copy.date('EEE, d MMM yyyy', appointment.requestedDate)}  •  ${appointment.requestedTimeSlot}',
              ),
              const SizedBox(height: 8),
              Text(
                copy.tapForDetails,
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../shared/widgets/empty_state.dart';
import '../../../shared/widgets/section_banner.dart';
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
    AppointmentStatus.pending => AyurvedaColors.goldMuted,
    AppointmentStatus.approved => AyurvedaColors.sageMuted,
    AppointmentStatus.rejected => AyurvedaColors.dangerMuted,
    AppointmentStatus.completed => AyurvedaColors.infoMuted,
    AppointmentStatus.cancelled => AyurvedaColors.neutralChip,
  };

  static Color foreground(AppointmentStatus status) => switch (status) {
    AppointmentStatus.pending => AyurvedaColors.forestDark,
    AppointmentStatus.approved => AyurvedaColors.forest,
    AppointmentStatus.rejected => AyurvedaColors.danger,
    AppointmentStatus.completed => AyurvedaColors.info,
    AppointmentStatus.cancelled => AyurvedaColors.neutralChipInk,
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
          error: (error, _) => ErrorState(
            message: copy.loadAppointmentsError(error),
            actionLabel: copy.tryAgain,
            onAction: () => ref.invalidate(myAppointmentsProvider),
            scrollable: true,
          ),
          data: (items) => items.isEmpty
              ? EmptyState(
                  message: copy.noAppointments,
                  detail: copy.chooseTreatmentFirst,
                  icon: Icons.event_note_outlined,
                  actionLabel: copy.bookNewAppointment,
                  onAction: () => context.go(AppRoutes.treatments),
                  useFilledAction: true,
                  actionKey: const ValueKey('empty-book-appointment'),
                  scrollable: true,
                )
              : ListView.separated(
                  padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
                  itemCount: items.length + 1,
                  separatorBuilder: (_, _) => const SizedBox(height: 12),
                  itemBuilder: (context, index) {
                    if (index == 0) {
                      return Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          ClipRRect(
                            borderRadius: BorderRadius.circular(20),
                            child: Image.asset(
                              'assets/images/consultation-desk.png',
                              height: 140,
                              width: double.infinity,
                              fit: BoxFit.cover,
                            ),
                          ),
                          const SizedBox(height: 12),
                          SectionBanner(
                            kicker: copy.text('Care schedule', 'සත්කාර කාලසටහන'),
                            title: copy.text(
                              'Requested visits',
                              'ඉල්ලා ඇති හමුවීම්',
                            ),
                            body: copy.text(
                              'Pending requests stay here until the hospital confirms the slot.',
                              'රෝහල වේලාව තහවුරු කරන තුරු ඉල්ලීම් මෙහි පවතී.',
                            ),
                          ),
                        ],
                      );
                    }
                    return _AppointmentCard(
                      appointment: items[index - 1],
                      onTap: () => _showDetails(context, ref, items[index - 1]),
                    );
                  },
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

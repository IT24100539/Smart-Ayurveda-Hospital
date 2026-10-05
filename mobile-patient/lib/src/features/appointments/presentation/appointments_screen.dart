import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/network/api_exception.dart';
import '../../../shared/widgets/clinic_widgets.dart';
import '../../../shared/widgets/empty_state.dart';
import '../../../shared/widgets/page_layout.dart';
import '../../../shared/widgets/responsive_columns.dart';
import '../../../shared/widgets/safe_asset_image.dart';
import '../../../shared/widgets/section_banner.dart';
import '../../../shared/widgets/skeleton.dart';
import '../../../shared/widgets/unlinked_patient_card.dart';
import '../../../theme/app_theme.dart';
import '../../../l10n/feature_localizations.dart';
import '../../../router/app_routes.dart';
import '../../feedback/presentation/feedback_messages.dart';
import '../../treatments/application/treatments_provider.dart';
import '../data/appointment_repository.dart';
import '../domain/appointment_models.dart';

abstract final class AppointmentsScreenKeys {
  static const skeleton = ValueKey('appointments-skeleton');
  static const retry = ValueKey('appointments-retry');
}

final myAppointmentsProvider = FutureProvider.autoDispose<List<Appointment>>(
  (ref) => ref.watch(appointmentRepositoryProvider).mine(),
);

class AppointmentStatusChip extends StatelessWidget {
  const AppointmentStatusChip({required this.status, super.key});
  final AppointmentStatus status;

  @override
  Widget build(BuildContext context) {
    final brand = AyurvedaThemeExtension.of(context);
    final scheme = Theme.of(context).colorScheme;
    final (background, foreground, icon) = switch (status) {
      AppointmentStatus.pending => (
        brand.pendingBackground,
        brand.pendingForeground,
        Icons.schedule,
      ),
      AppointmentStatus.approved => (
        brand.approvedBackground,
        brand.approvedForeground,
        Icons.check,
      ),
      AppointmentStatus.rejected => (
        scheme.errorContainer,
        scheme.error,
        Icons.close,
      ),
      AppointmentStatus.completed => (
        brand.completedBackground,
        brand.completedForeground,
        Icons.check_circle_outline,
      ),
      AppointmentStatus.cancelled => (
        brand.neutralBackground,
        brand.neutralForeground,
        null,
      ),
    };
    return PillChip(
      key: ValueKey('status-chip-${status.name}'),
      label: FeatureLocalizations.of(context).status(status.name),
      icon: icon,
      background: background,
      foreground: foreground,
    );
  }
}

class MyAppointmentsScreen extends ConsumerWidget {
  const MyAppointmentsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final appointments = ref.watch(myAppointmentsProvider);
    final copy = FeatureLocalizations.of(context);
    return Scaffold(
      appBar: AppBar(title: Text(copy.myAppointments)),
      floatingActionButton: appointments.maybeWhen(
        data: (items) => items.isEmpty
            ? null
            : FloatingActionButton.extended(
                key: const ValueKey('book-appointment-fab'),
                onPressed: () => context.push(AppRoutes.chooseTherapyToBook),
                icon: const Icon(Icons.add),
                label: Text(copy.bookNewAppointment),
              ),
        orElse: () => null,
      ),
      body: RefreshIndicator(
        onRefresh: () => ref.refresh(myAppointmentsProvider.future),
        child: appointments.when(
          loading: () => const SkeletonList(
            twoColumns: true,
            listKey: AppointmentsScreenKeys.skeleton,
          ),
          error: (error, _) {
            if (isUnlinkedPatientError(error)) {
              return ListView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.all(20),
                children: [
                  UnlinkedPatientCard(
                    onRetry: () => ref.invalidate(myAppointmentsProvider),
                    onHelp: () => context.push(AppRoutes.contact),
                  ),
                ],
              );
            }
            return ErrorState(
              message: copy.loadAppointmentsError(
                describeAppointmentLoadError(error),
              ),
              actionLabel: copy.tryAgain,
              actionKey: AppointmentsScreenKeys.retry,
              onAction: () => ref.invalidate(myAppointmentsProvider),
              scrollable: true,
            );
          },
          data: (items) => items.isEmpty
              ? EmptyState(
                  message: copy.noAppointments,
                  detail: copy.chooseTreatmentFirst,
                  icon: Icons.event_note_outlined,
                  imageAsset: 'assets/images/empty-appointments.png',
                  actionLabel: copy.bookNewAppointment,
                  onAction: () => context.push(AppRoutes.chooseTherapyToBook),
                  useFilledAction: true,
                  actionKey: const ValueKey('empty-book-appointment'),
                  scrollable: true,
                )
              : PageListView(
                  maxWidth: wideContentWidth,
                  children: [
                    ClipRRect(
                      borderRadius: BorderRadius.circular(
                        AyurvedaThemeExtension.of(context).cardRadius,
                      ),
                      child: SafeAssetImage(
                        asset: 'assets/images/consultation-desk.png',
                        height: 140,
                        width: double.infinity,
                        fallbackIcon: Icons.event_note_outlined,
                      ),
                    ),
                    const SizedBox(height: 12),
                    SectionBanner(
                      kicker: copy.text('Care schedule', 'සත්කාර කාලසටහන'),
                      title: copy.text('Requested visits', 'ඉල්ලා ඇති හමුවීම්'),
                      body: copy.text(
                        'Pending requests stay here until the hospital confirms the slot.',
                        'රෝහල වේලාව තහවුරු කරන තුරු ඉල්ලීම් මෙහි පවතී.',
                      ),
                    ),
                    ResponsiveColumns(
                      children: [
                        for (final appointment in items)
                          _AppointmentCard(
                            appointment: appointment,
                            onTap: () => _showDetails(context, ref, appointment),
                          ),
                      ],
                    ),
                  ],
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
      isScrollControlled: true,
      // Keeps the sheet a readable width on tablets and Flutter web.
      constraints: const BoxConstraints(maxWidth: 560),
      builder: (sheetContext) => SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.fromLTRB(24, 4, 24, 28),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
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
              if (_canRescheduleOrCancel(appointment.status)) ...[
                const SizedBox(height: 12),
                FilledButton.icon(
                  key: const ValueKey('reschedule-appointment-button'),
                  onPressed: () {
                    Navigator.pop(sheetContext);
                    final treatments =
                        ref.read(treatmentsProvider).valueOrNull ?? [];
                    final matching = treatments
                        .where(
                          (t) =>
                              t.nameEnglish == appointment.treatmentName ||
                              t.nameSinhala == appointment.treatmentName,
                        )
                        .firstOrNull;
                    final effectiveTreatmentId =
                        appointment.treatmentId ?? matching?.id ?? '';
                    context.push(
                      '/appointments/${appointment.id}/reschedule?treatmentId=$effectiveTreatmentId&name=${Uri.encodeComponent(appointment.treatmentName)}',
                    );
                  },
                  icon: const Icon(Icons.event_repeat, size: 18),
                  label: Text(
                    copy.text(
                      'Reschedule appointment',
                      'හමුවීම වෙනත් දිනකට මාරු කරන්න',
                    ),
                  ),
                ),
                const SizedBox(height: 10),
                OutlinedButton.icon(
                  key: const ValueKey('cancel-appointment-button'),
                  style: OutlinedButton.styleFrom(
                    foregroundColor: Theme.of(context).colorScheme.error,
                    side: BorderSide(
                      color: Theme.of(context).colorScheme.error,
                    ),
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
            key: const ValueKey('confirm-cancel-button'),
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

  /// Only appointments in 'pending' or 'approved' status can be rescheduled or cancelled.
  /// Terminal/closed statuses ('cancelled', 'completed', 'rejected') hide modification controls.
  static bool _canRescheduleOrCancel(AppointmentStatus status) =>
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
    final theme = Theme.of(context);
    final brand = AyurvedaThemeExtension.of(context);
    return ClinicCard(
      padding: EdgeInsets.zero,
      child: InkWell(
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
                      style: theme.textTheme.titleMedium,
                    ),
                  ),
                  const SizedBox(width: 8),
                  AppointmentStatusChip(status: appointment.status),
                ],
              ),
              const SizedBox(height: 12),
              Wrap(
                spacing: 16,
                runSpacing: 6,
                children: [
                  _CardMeta(
                    icon: Icons.calendar_today_outlined,
                    text: copy.date('EEE, d MMM yyyy', appointment.requestedDate),
                  ),
                  if (appointment.requestedTimeSlot.isNotEmpty)
                    _CardMeta(
                      icon: Icons.schedule,
                      text: appointment.requestedTimeSlot,
                    ),
                ],
              ),
              const SizedBox(height: 10),
              Row(
                children: [
                  Expanded(
                    child: Text(
                      copy.tapForDetails,
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                  ),
                  Icon(Icons.chevron_right, size: 20, color: brand.teal),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

String describeAppointmentLoadError(Object error) {
  if (error is ApiException) {
    if (error.isNetworkError) {
      return 'Check your connection and try again.';
    }
    return error.message ?? 'The hospital could not load your visits.';
  }
  return error.toString();
}

class _CardMeta extends StatelessWidget {
  const _CardMeta({required this.icon, required this.text});

  final IconData icon;
  final String text;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(
          icon,
          size: 16,
          color: Theme.of(context).colorScheme.onSurfaceVariant,
        ),
        const SizedBox(width: 6),
        Text(
          text,
          style: Theme.of(
            context,
          ).textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w600),
        ),
      ],
    );
  }
}

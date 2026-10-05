import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../../../l10n/feature_localizations.dart';
import '../../../../shared/widgets/clinic_widgets.dart';
import '../../../../shared/widgets/skeleton.dart';
import '../../../../theme/app_theme.dart';
import '../../../appointments/domain/appointment_models.dart';
import '../../../appointments/presentation/appointments_screen.dart';

class UpcomingAppointmentCard extends StatelessWidget {
  const UpcomingAppointmentCard({
    super.key,
    required this.appointment,
    required this.isLoading,
    required this.onTap,
    required this.onBookConsultation,
    this.hasError = false,
    this.onRetry,
  });

  final Appointment? appointment;
  final bool isLoading;
  final VoidCallback onTap;
  final VoidCallback onBookConsultation;

  /// The appointment list failed to load. Shows a retry row, not the empty state.
  final bool hasError;
  final VoidCallback? onRetry;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final brand = AyurvedaThemeExtension.of(context);
    final copy = FeatureLocalizations.of(context);

    if (isLoading) return const _UpcomingSkeleton();

    if (hasError && appointment == null) {
      return ClinicCard(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        child: Row(
          children: [
            Icon(Icons.cloud_off_outlined, color: theme.colorScheme.error),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                copy.text(
                  'Could not load your appointments.',
                  'ඔබගේ හමුවීම් පූරණය කළ නොහැකි විය.',
                ),
                style: TextStyle(
                  color: theme.colorScheme.error,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
            if (onRetry != null)
              TextButton(onPressed: onRetry, child: Text(copy.tryAgain)),
          ],
        ),
      );
    }

    if (appointment == null) {
      return ClinicCard(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        child: Row(
          children: [
            _IconTile(icon: Icons.calendar_month_outlined, brand: brand),
            const SizedBox(width: 14),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    copy.text(
                      'No Upcoming Appointments',
                      'ඉදිරි වෙන්කිරීම් නොමැත',
                    ),
                    style: theme.textTheme.titleSmall,
                  ),
                  const SizedBox(height: 2),
                  Text(
                    copy.text(
                      'Request a consultation or therapy visit',
                      'උපදේශනයක් හෝ ප්‍රතිකාර හමුවක් ඉල්ලන්න',
                    ),
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                  ),
                ],
              ),
            ),
            TextButton(
              onPressed: onBookConsultation,
              style: TextButton.styleFrom(visualDensity: VisualDensity.compact),
              child: Text(copy.bookAppointment),
            ),
          ],
        ),
      );
    }

    final appt = appointment!;
    final now = DateTime.now();
    final todayStart = DateTime(now.year, now.month, now.day);
    final apptDay = DateTime(
      appt.requestedDate.year,
      appt.requestedDate.month,
      appt.requestedDate.day,
    );
    final diffDays = apptDay.difference(todayStart).inDays;

    final countdownBadge = switch (diffDays) {
      0 => copy.text('Today', 'අද'),
      1 => copy.text('Tomorrow', 'හෙට'),
      _ => copy.text('In $diffDays days', 'දින $diffDays කින්'),
    };

    final dateFormatted = DateFormat('EEE, MMM d').format(appt.requestedDate);

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
                children: [
                  Icon(Icons.alarm_outlined, color: brand.teal, size: 18),
                  const SizedBox(width: 6),
                  Expanded(
                    child: Text(
                      copy
                          .text('Upcoming Appointment', 'ඉදිරි හමුවීම')
                          .toUpperCase(),
                      style: AyurvedaType.eyebrow(context, color: brand.teal),
                    ),
                  ),
                  PillChip(label: countdownBadge),
                ],
              ),
              const Divider(height: 24),
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  _IconTile(icon: Icons.spa_outlined, brand: brand),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          appt.treatmentName,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                          style: theme.textTheme.titleMedium,
                        ),
                        const SizedBox(height: 6),
                        Wrap(
                          spacing: 14,
                          runSpacing: 4,
                          children: [
                            _MetaItem(
                              icon: Icons.calendar_today,
                              text: dateFormatted,
                              strong: true,
                            ),
                            if (appt.requestedTimeSlot.isNotEmpty)
                              _MetaItem(
                                icon: Icons.access_time,
                                text: appt.requestedTimeSlot,
                              ),
                            AppointmentStatusChip(status: appt.status),
                          ],
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _IconTile extends StatelessWidget {
  const _IconTile({required this.icon, required this.brand});

  final IconData icon;
  final AyurvedaThemeExtension brand;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.primaryContainer,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Icon(icon, color: brand.teal, size: 22),
    );
  }
}

class _MetaItem extends StatelessWidget {
  const _MetaItem({required this.icon, required this.text, this.strong = false});

  final IconData icon;
  final String text;
  final bool strong;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final muted = theme.colorScheme.onSurfaceVariant;
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 13, color: muted),
        const SizedBox(width: 4),
        Flexible(
          child: Text(
            text,
            overflow: TextOverflow.ellipsis,
            style: theme.textTheme.bodySmall?.copyWith(
              fontWeight: strong ? FontWeight.w600 : FontWeight.w400,
              color: strong ? null : muted,
            ),
          ),
        ),
      ],
    );
  }
}

class _UpcomingSkeleton extends StatelessWidget {
  const _UpcomingSkeleton();

  @override
  Widget build(BuildContext context) {
    return const SkeletonScope(
      child: ClinicCard(
        child: Row(
          children: [
            SkeletonBone(height: 44, circle: true),
            SizedBox(width: 14),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  SkeletonBone(height: 16, width: 160),
                  SizedBox(height: 10),
                  SkeletonBone(height: 12, width: 220),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

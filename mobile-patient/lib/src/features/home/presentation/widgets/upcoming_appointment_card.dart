import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../../../l10n/feature_localizations.dart';
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
  });

  final Appointment? appointment;
  final bool isLoading;
  final VoidCallback onTap;
  final VoidCallback onBookConsultation;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final featureCopy = FeatureLocalizations.of(context);

    if (isLoading) {
      return Card(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(18)),
        child: const Padding(
          padding: EdgeInsets.all(20),
          child: Center(
            child: SizedBox(
              height: 24,
              width: 24,
              child: CircularProgressIndicator(strokeWidth: 2),
            ),
          ),
        ),
      );
    }

    if (appointment == null) {
      return Card(
        elevation: 1,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(18),
          side: BorderSide(
            color: theme.colorScheme.outline.withValues(alpha: 0.35),
          ),
        ),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
          child: Row(
            children: [
              Container(
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: AyurvedaColors.sageMuted,
                  borderRadius: BorderRadius.circular(12),
                ),
                child: const Icon(
                  Icons.calendar_month_outlined,
                  color: AyurvedaColors.forest,
                  size: 22,
                ),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      featureCopy.text('No Upcoming Appointments', 'ඉදිරි වෙන්කිරීම් නොමැත'),
                      style: theme.textTheme.titleSmall?.copyWith(
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      featureCopy.text(
                        'Schedule your consultation or wellness session',
                        'ඔබේ උපදේශනය හෝ ප්‍රතිකාරය වෙන්කරවා ගන්න',
                      ),
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: theme.colorScheme.onSurfaceVariant,
                        fontSize: 11,
                      ),
                    ),
                  ],
                ),
              ),
              TextButton(
                onPressed: onBookConsultation,
                style: TextButton.styleFrom(
                  visualDensity: VisualDensity.compact,
                ),
                child: Text(featureCopy.bookAppointment),
              ),
            ],
          ),
        ),
      );
    }

    final appt = appointment!;
    final now = DateTime.now();
    final todayStart = DateTime(now.year, now.month, now.day);
    final apptDay = DateTime(appt.requestedDate.year, appt.requestedDate.month, appt.requestedDate.day);
    final diffDays = apptDay.difference(todayStart).inDays;

    final countdownBadge = switch (diffDays) {
      0 => featureCopy.text('Today', 'අද'),
      1 => featureCopy.text('Tomorrow', 'හෙට'),
      _ => featureCopy.text('In $diffDays days', 'දින $diffDays කින්'),
    };

    final dateFormatted = DateFormat('EEE, MMM d').format(appt.requestedDate);

    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(20),
        side: const BorderSide(color: AyurvedaColors.forest, width: 1.2),
      ),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(20),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Row(
                    children: [
                      const Icon(
                        Icons.alarm_outlined,
                        color: AyurvedaColors.forest,
                        size: 18,
                      ),
                      const SizedBox(width: 6),
                      Text(
                        featureCopy.text('Upcoming Appointment', 'ඉදිරි හමුවීම'),
                        style: theme.textTheme.labelMedium?.copyWith(
                          fontWeight: FontWeight.bold,
                          color: AyurvedaColors.forestDark,
                        ),
                      ),
                    ],
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 3),
                    decoration: BoxDecoration(
                      color: AyurvedaColors.goldMuted,
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: Text(
                      countdownBadge,
                      style: const TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.bold,
                        color: AyurvedaColors.forestDark,
                      ),
                    ),
                  ),
                ],
              ),
              const Divider(height: 18),
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Container(
                    padding: const EdgeInsets.all(10),
                    decoration: BoxDecoration(
                      color: AyurvedaColors.forest.withValues(alpha: 0.1),
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: const Icon(
                      Icons.spa_outlined,
                      color: AyurvedaColors.forest,
                      size: 24,
                    ),
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          appt.treatmentName,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: theme.textTheme.titleMedium?.copyWith(
                            fontWeight: FontWeight.bold,
                            fontFamily: 'serif',
                          ),
                        ),
                        const SizedBox(height: 4),
                        Row(
                          children: [
                            const Icon(
                              Icons.calendar_today,
                              size: 13,
                              color: AyurvedaColors.inkMuted,
                            ),
                            const SizedBox(width: 4),
                            Text(
                              dateFormatted,
                              style: theme.textTheme.bodySmall?.copyWith(
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                            if (appt.requestedTimeSlot.isNotEmpty) ...[
                              const SizedBox(width: 10),
                              const Icon(
                                Icons.access_time,
                                size: 13,
                                color: AyurvedaColors.inkMuted,
                              ),
                              const SizedBox(width: 4),
                              Text(
                                appt.requestedTimeSlot,
                                style: theme.textTheme.bodySmall?.copyWith(
                                  color: theme.colorScheme.onSurfaceVariant,
                                ),
                              ),
                            ],
                          ],
                        ),
                      ],
                    ),
                  ),
                  AppointmentStatusChip(status: appt.status),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

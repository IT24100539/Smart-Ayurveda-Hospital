import 'package:flutter/material.dart';

import '../../../l10n/app_localizations.dart';
import '../../../l10n/feature_localizations.dart';
import '../../../shared/widgets/clinic_widgets.dart';
import '../../../theme/app_theme.dart';
import '../../appointments/domain/appointment_models.dart';
import '../../appointments/presentation/appointments_screen.dart';
import '../domain/health_hub_models.dart';

/// Small uppercase label above a value.
class _FieldLabel extends StatelessWidget {
  const _FieldLabel(this.text);

  final String text;

  @override
  Widget build(BuildContext context) {
    return Text(text.toUpperCase(), style: AyurvedaType.eyebrow(context));
  }
}

class _LabeledValue extends StatelessWidget {
  const _LabeledValue({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _FieldLabel(label),
        const SizedBox(height: 2),
        Text(
          value,
          style: theme.textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w600),
        ),
      ],
    );
  }
}

/// Appointment card: treatment, status pill, then date and time under small caps labels.
class HealthHubAppointmentCard extends StatelessWidget {
  const HealthHubAppointmentCard({required this.appointment, super.key});

  final Appointment appointment;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final copy = FeatureLocalizations.of(context);
    return ClinicCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    _FieldLabel(copy.treatment),
                    const SizedBox(height: 2),
                    Text(
                      appointment.treatmentName,
                      style: theme.textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 8),
              AppointmentStatusChip(status: appointment.status),
            ],
          ),
          const SizedBox(height: 14),
          Wrap(
            spacing: 28,
            runSpacing: 10,
            children: [
              _LabeledValue(
                label: copy.dateLabel,
                value: copy.date('EEE, d MMM yyyy', appointment.requestedDate),
              ),
              if (appointment.requestedTimeSlot.isNotEmpty)
                _LabeledValue(
                  label: copy.timeLabel,
                  value: appointment.requestedTimeSlot,
                ),
            ],
          ),
        ],
      ),
    );
  }
}

/// Therapy plan card: treatment, session count, next session, then each session with its status pill.
class HealthHubTherapyCard extends StatelessWidget {
  const HealthHubTherapyCard({required this.plan, super.key});

  final TreatmentPlan plan;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final l10n = AppLocalizations.of(context);
    final copy = FeatureLocalizations.of(context);
    final brand = AyurvedaThemeExtension.of(context);
    final next = plan.nextDate;

    return ClinicCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: theme.colorScheme.primaryContainer,
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Icon(Icons.spa_outlined, color: brand.teal, size: 22),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Text(
                  plan.treatmentName,
                  style: theme.textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
              const SizedBox(width: 8),
              PillChip(label: l10n.sessionsCount(plan.sessionCount)),
            ],
          ),
          if (next != null) ...[
            const SizedBox(height: 12),
            _LabeledValue(
              label: l10n.nextSessionLabel,
              value: copy.date('EEE, MMM d, yyyy', next),
            ),
          ],
          if (plan.sessions.isNotEmpty) ...[
            const Divider(height: 24),
            for (final session in plan.sessions)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 5),
                child: Row(
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            copy.date('MMM d, yyyy', session.date),
                            style: theme.textTheme.bodyMedium?.copyWith(
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                          if (session.timeSlot.isNotEmpty)
                            Text(
                              session.timeSlot,
                              style: theme.textTheme.bodySmall?.copyWith(
                                color: theme.colorScheme.onSurfaceVariant,
                              ),
                            ),
                        ],
                      ),
                    ),
                    const SizedBox(width: 8),
                    AppointmentStatusChip(status: session.status),
                  ],
                ),
              ),
          ],
        ],
      ),
    );
  }
}

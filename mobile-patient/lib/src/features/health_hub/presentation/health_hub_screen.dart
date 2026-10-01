import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/empty_state.dart';
import '../../../shared/widgets/section_banner.dart';
import '../../../theme/app_theme.dart';
import '../../appointments/presentation/appointments_screen.dart';
import '../data/health_hub_repository.dart';
import '../domain/health_hub_models.dart';

class HealthHubScreen extends ConsumerWidget {
  const HealthHubScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final plansAsync = ref.watch(treatmentPlansProvider);
    final summaryAsync = ref.watch(registrationSummaryProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.myHealthHubTitle),
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(treatmentPlansProvider);
          ref.invalidate(registrationSummaryProvider);
          await Future.wait([
            ref.read(treatmentPlansProvider.future),
            ref.read(registrationSummaryProvider.future),
          ]);
        },
        child: SingleChildScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.fromLTRB(20, 8, 20, 32),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              SectionBanner(
                kicker: l10n.appTitle,
                title: l10n.myHealthHubTitle,
                body: l10n.myHealthHubSubtitle,
              ),
              const SizedBox(height: 20),

              // Section 1: My therapy sessions
              Text(
                l10n.myTherapySessionsTitle,
                style: theme.textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.bold,
                  fontFamily: 'serif',
                ),
              ),
              const SizedBox(height: 10),
              plansAsync.when(
                loading: () => const Padding(
                  padding: EdgeInsets.symmetric(vertical: 24),
                  child: Center(child: CircularProgressIndicator()),
                ),
                error: (error, _) => ErrorState(
                  message: l10n.healthHubError,
                  actionLabel: l10n.retry,
                  onAction: () => ref.invalidate(treatmentPlansProvider),
                ),
                data: (plans) {
                  if (plans.isEmpty) {
                    return Card(
                      child: Padding(
                        padding: const EdgeInsets.symmetric(vertical: 20, horizontal: 16),
                        child: EmptyState(
                          message: l10n.noTherapySessionsFound,
                        ),
                      ),
                    );
                  }

                  return Column(
                    children: plans.map((plan) => _TreatmentPlanCard(plan: plan)).toList(),
                  );
                },
              ),
              const SizedBox(height: 28),

              // Section 2: My registration summary
              Text(
                l10n.myRegistrationSummaryTitle,
                style: theme.textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.bold,
                  fontFamily: 'serif',
                ),
              ),
              const SizedBox(height: 10),
              summaryAsync.when(
                loading: () => const Padding(
                  padding: EdgeInsets.symmetric(vertical: 24),
                  child: Center(child: CircularProgressIndicator()),
                ),
                error: (error, _) => ErrorState(
                  message: l10n.healthHubError,
                  actionLabel: l10n.retry,
                  onAction: () => ref.invalidate(registrationSummaryProvider),
                ),
                data: (summary) => _RegistrationSummaryCard(summary: summary),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _TreatmentPlanCard extends StatelessWidget {
  const _TreatmentPlanCard({required this.plan});

  final TreatmentPlan plan;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final l10n = AppLocalizations.of(context);

    return Card(
      margin: const EdgeInsets.only(bottom: 14),
      elevation: 2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(18),
        side: BorderSide(
          color: theme.colorScheme.outline.withValues(alpha: 0.5),
        ),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.center,
              children: [
                Container(
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    color: AyurvedaColors.forest.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: const Icon(
                    Icons.spa_outlined,
                    color: AyurvedaColors.forest,
                    size: 22,
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Text(
                    plan.treatmentName,
                    style: theme.textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.bold,
                      fontFamily: 'serif',
                    ),
                  ),
                ),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                  decoration: BoxDecoration(
                    color: AyurvedaColors.sageMuted,
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Text(
                    l10n.sessionsCount(plan.sessionCount),
                    style: const TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.bold,
                      color: AyurvedaColors.forestDark,
                    ),
                  ),
                ),
              ],
            ),
            if (plan.nextDate != null) ...[
              const SizedBox(height: 10),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                decoration: BoxDecoration(
                  color: AyurvedaColors.goldMuted.withValues(alpha: 0.5),
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(
                    color: AyurvedaColors.gold.withValues(alpha: 0.4),
                  ),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const Icon(
                      Icons.event_outlined,
                      size: 15,
                      color: AyurvedaColors.forestDark,
                    ),
                    const SizedBox(width: 6),
                    Text(
                      '${l10n.nextSessionLabel}: ${DateFormat('EEE, MMM d, yyyy').format(plan.nextDate!)}',
                      style: const TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w600,
                        color: AyurvedaColors.forestDark,
                      ),
                    ),
                  ],
                ),
              ),
            ],
            const Divider(height: 22),
            ...plan.sessions.map((session) {
              final formattedDate = DateFormat('MMM d, yyyy').format(session.date);
              return Padding(
                padding: const EdgeInsets.symmetric(vertical: 4),
                child: Row(
                  children: [
                    const Icon(
                      Icons.circle,
                      size: 8,
                      color: AyurvedaColors.forest,
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        formattedDate,
                        style: theme.textTheme.bodyMedium?.copyWith(
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                    if (session.timeSlot.isNotEmpty) ...[
                      Text(
                        session.timeSlot,
                        style: theme.textTheme.bodySmall?.copyWith(
                          color: theme.colorScheme.onSurfaceVariant,
                        ),
                      ),
                      const SizedBox(width: 10),
                    ],
                    AppointmentStatusChip(status: session.status),
                  ],
                ),
              );
            }),
          ],
        ),
      ),
    );
  }
}

class _RegistrationSummaryCard extends StatelessWidget {
  const _RegistrationSummaryCard({required this.summary});

  final RegistrationSummary summary;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final l10n = AppLocalizations.of(context);

    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(18),
        side: BorderSide(
          color: theme.colorScheme.outline.withValues(alpha: 0.5),
        ),
      ),
      child: Padding(
        padding: const EdgeInsets.all(18),
        child: Column(
          children: [
            _SummaryRow(
              label: l10n.uhidLabel,
              value: summary.uhid,
              isHighlight: true,
            ),
            const Divider(height: 16),
            _SummaryRow(
              label: l10n.fullNameLabel,
              value: summary.fullName,
            ),
            const Divider(height: 16),
            _SummaryRow(
              label: l10n.phoneNumberLabel,
              value: summary.phone,
            ),
            const Divider(height: 16),
            _SummaryRow(
              label: l10n.emailLabel,
              value: summary.email?.isNotEmpty == true ? summary.email! : l10n.notRecorded,
            ),
            const Divider(height: 16),
            _SummaryRow(
              label: l10n.dateOfBirthLabel,
              value: DateFormat('yyyy-MM-dd').format(summary.dateOfBirth),
            ),
            const Divider(height: 16),
            _SummaryRow(
              label: l10n.genderLabel,
              value: summary.gender,
            ),
            const Divider(height: 16),
            _SummaryRow(
              label: l10n.prakritiLabel,
              value: summary.prakriti.isNotEmpty ? summary.prakriti : l10n.notRecorded,
            ),
            const Divider(height: 16),
            _SummaryRow(
              label: l10n.vikritiLabel,
              value: summary.vikriti.isNotEmpty ? summary.vikriti : l10n.notRecorded,
            ),
            const Divider(height: 16),
            _SummaryRow(
              label: l10n.allergiesLabel,
              value: summary.allergies?.isNotEmpty == true ? summary.allergies! : l10n.notRecorded,
            ),
            const Divider(height: 16),
            _SummaryRow(
              label: l10n.bloodGroupLabel,
              value: summary.bloodGroup?.isNotEmpty == true ? summary.bloodGroup! : l10n.notRecorded,
            ),
          ],
        ),
      ),
    );
  }
}

class _SummaryRow extends StatelessWidget {
  const _SummaryRow({
    required this.label,
    required this.value,
    this.isHighlight = false,
  });

  final String label;
  final String value;
  final bool isHighlight;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(
          width: 120,
          child: Text(
            label,
            style: theme.textTheme.bodySmall?.copyWith(
              color: theme.colorScheme.onSurfaceVariant,
              fontWeight: FontWeight.w500,
            ),
          ),
        ),
        const SizedBox(width: 8),
        Expanded(
          child: isHighlight
              ? Text(
                  value,
                  style: theme.textTheme.bodyMedium?.copyWith(
                    fontWeight: FontWeight.bold,
                    color: AyurvedaColors.forest,
                    letterSpacing: 0.5,
                  ),
                )
              : Text(
                  value,
                  style: theme.textTheme.bodyMedium?.copyWith(
                    fontWeight: FontWeight.w600,
                  ),
                ),
        ),
      ],
    );
  }
}

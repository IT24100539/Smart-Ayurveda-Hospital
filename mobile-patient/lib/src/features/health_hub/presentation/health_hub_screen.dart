import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import 'package:go_router/go_router.dart';

import '../../../l10n/app_localizations.dart';
import '../../../l10n/feature_localizations.dart';
import '../../../router/app_routes.dart';
import '../../../shared/widgets/clinic_widgets.dart';
import '../../../shared/widgets/empty_state.dart';
import '../../../shared/widgets/responsive_columns.dart';
import '../../../theme/app_theme.dart';
import '../../appointments/presentation/appointments_screen.dart';
import '../../records/application/records_provider.dart';
import '../../records/presentation/documents_tab.dart';
import '../../records/presentation/invoices_tab.dart';
import '../../records/presentation/prescriptions_tab.dart';
import '../../records/presentation/records_panel.dart';
import '../application/upcoming_appointments.dart';
import '../data/health_hub_repository.dart';
import '../domain/health_hub_models.dart';
import 'health_hub_cards.dart';

abstract final class HealthHubTabKeys {
  static const upcoming = ValueKey('health-hub-tab-upcoming');
  static const therapy = ValueKey('health-hub-tab-therapy');
  static const registration = ValueKey('health-hub-tab-registration');
  static const prescriptions = ValueKey('health-hub-tab-prescriptions');
  static const invoices = ValueKey('health-hub-tab-invoices');
  static const documents = ValueKey('health-hub-tab-documents');
}

const _tabCount = 6;

/// Upcoming is 0, therapy 1, registration 2, prescriptions 3, invoices 4,
/// documents 5. Anything else opens upcoming.
int healthHubTabIndex(String? tab) => switch (tab) {
  'therapy' => 1,
  'registration' => 2,
  'prescriptions' => 3,
  'invoices' => 4,
  'documents' => 5,
  _ => 0,
};

int? _loadedCount<T>(AsyncValue<List<T>> value) {
  return value.maybeWhen(data: (items) => items.length, orElse: () => null);
}

/// Registered details only: gender and date of birth when they are on file.
String _registeredLine(RegistrationSummary record) {
  final parts = <String>[
    if (record.gender.isNotEmpty) record.gender,
    DateFormat.yMMMd().format(record.dateOfBirth),
  ];
  return parts.join(' · ');
}

class HealthHubScreen extends ConsumerWidget {
  const HealthHubScreen({this.initialTab = 0, super.key});

  final int initialTab;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final copy = FeatureLocalizations.of(context);
    final brand = AyurvedaThemeExtension.of(context);
    final index = initialTab.clamp(0, _tabCount - 1).toInt();
    final summary = ref.watch(registrationSummaryProvider);

    return DefaultTabController(
      length: _tabCount,
      initialIndex: index,
      child: Scaffold(
        appBar: AppBar(title: Text(l10n.myHealthHubTitle)),
        body: Column(
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 4),
              child: summary.maybeWhen(
                data: (record) => PatientHeaderCard(
                  name: record.fullName,
                  uhid: record.uhid,
                  uhidLabel: l10n.uhidLabel,
                  subtitle: _registeredLine(record),
                  action: FilledButton(
                    onPressed: () =>
                        context.push(AppRoutes.chooseTherapyToBook),
                    style: FilledButton.styleFrom(
                      backgroundColor: brand.avatarBackground,
                      foregroundColor: brand.avatarForeground,
                    ),
                    child: Text(copy.bookNewAppointment),
                  ),
                ),
                orElse: () => const SizedBox(height: 8),
              ),
            ),
            UnderlineTabBar(
              tabs: [
                UnderlineTabItem(
                  label: l10n.healthHubUpcomingTab,
                  count: _loadedCount(ref.watch(upcomingAppointmentsProvider)),
                  tabKey: HealthHubTabKeys.upcoming,
                ),
                UnderlineTabItem(
                  label: l10n.healthHubTherapyTab,
                  count: _loadedCount(ref.watch(treatmentPlansProvider)),
                  tabKey: HealthHubTabKeys.therapy,
                ),
                UnderlineTabItem(
                  label: l10n.healthHubRegistrationTab,
                  tabKey: HealthHubTabKeys.registration,
                ),
                UnderlineTabItem(
                  label: l10n.healthHubPrescriptionsTab,
                  count: _loadedCount(ref.watch(prescriptionsProvider)),
                  tabKey: HealthHubTabKeys.prescriptions,
                ),
                UnderlineTabItem(
                  label: l10n.healthHubInvoicesTab,
                  count: _loadedCount(ref.watch(invoicesProvider)),
                  tabKey: HealthHubTabKeys.invoices,
                ),
                UnderlineTabItem(
                  label: l10n.healthHubDocumentsTab,
                  count: _loadedCount(ref.watch(documentsProvider)),
                  tabKey: HealthHubTabKeys.documents,
                ),
              ],
            ),
            const Expanded(
              child: TabBarView(
                children: [
                  _UpcomingTab(),
                  _TherapyTab(),
                  _RegistrationTab(),
                  PrescriptionsTab(),
                  InvoicesTab(),
                  DocumentsTab(),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _UpcomingTab extends ConsumerWidget {
  const _UpcomingTab();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    return RefreshIndicator(
      onRefresh: () async {
        ref.invalidate(myAppointmentsProvider);
        await ref.read(myAppointmentsProvider.future);
      },
      child: RecordsPanel(
        state: ref.watch(upcomingAppointmentsProvider),
        errorMessage: l10n.healthHubError,
        emptyMessage: l10n.healthHubUpcomingEmpty,
        emptyIcon: Icons.event_note_outlined,
        onRetry: () => ref.invalidate(myAppointmentsProvider),
        itemBuilder: (appointment) =>
            HealthHubAppointmentCard(appointment: appointment),
      ),
    );
  }
}

class _TherapyTab extends ConsumerWidget {
  const _TherapyTab();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    return RefreshIndicator(
      onRefresh: () async {
        ref.invalidate(treatmentPlansProvider);
        await ref.read(treatmentPlansProvider.future);
      },
      child: RecordsPanel(
        state: ref.watch(treatmentPlansProvider),
        errorMessage: l10n.healthHubError,
        emptyMessage: l10n.noTherapySessionsFound,
        emptyIcon: Icons.spa_outlined,
        onRetry: () => ref.invalidate(treatmentPlansProvider),
        itemBuilder: (plan) => HealthHubTherapyCard(plan: plan),
      ),
    );
  }
}

class _RegistrationTab extends ConsumerWidget {
  const _RegistrationTab();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final summaryAsync = ref.watch(registrationSummaryProvider);

    return RefreshIndicator(
      onRefresh: () async {
        ref.invalidate(registrationSummaryProvider);
        await ref.read(registrationSummaryProvider.future);
      },
      child: summaryAsync.when(
        loading: () => ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          children: const [
            SizedBox(height: 80),
            Center(child: CircularProgressIndicator()),
          ],
        ),
        error: (error, _) => ErrorState(
          message: l10n.healthHubError,
          actionLabel: l10n.retry,
          onAction: () => ref.invalidate(registrationSummaryProvider),
          scrollable: true,
        ),
        data: (summary) => ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.fromLTRB(20, 12, 20, 24),
          children: [_RegistrationSummary(summary: summary)],
        ),
      ),
    );
  }
}

class _RegistrationSummary extends StatelessWidget {
  const _RegistrationSummary({required this.summary});

  final RegistrationSummary summary;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    String recorded(String? value) =>
        value != null && value.isNotEmpty ? value : l10n.notRecorded;

    final identity = <_SummaryRow>[
      _SummaryRow(label: l10n.uhidLabel, value: summary.uhid, isHighlight: true),
      _SummaryRow(label: l10n.fullNameLabel, value: summary.fullName),
      _SummaryRow(label: l10n.phoneNumberLabel, value: summary.phone),
      _SummaryRow(label: l10n.emailLabel, value: recorded(summary.email)),
      _SummaryRow(
        label: l10n.dateOfBirthLabel,
        value: DateFormat('yyyy-MM-dd').format(summary.dateOfBirth),
      ),
      _SummaryRow(label: l10n.genderLabel, value: summary.gender),
    ];
    final constitution = <_SummaryRow>[
      _SummaryRow(label: l10n.prakritiLabel, value: recorded(summary.prakriti)),
      _SummaryRow(label: l10n.vikritiLabel, value: recorded(summary.vikriti)),
      _SummaryRow(label: l10n.allergiesLabel, value: recorded(summary.allergies)),
      _SummaryRow(label: l10n.bloodGroupLabel, value: recorded(summary.bloodGroup)),
    ];

    return ResponsiveColumns(
      children: [
        _SummaryCard(rows: identity),
        _SummaryCard(rows: constitution),
      ],
    );
  }
}

class _SummaryCard extends StatelessWidget {
  const _SummaryCard({required this.rows});

  final List<_SummaryRow> rows;

  @override
  Widget build(BuildContext context) {
    return ClinicCard(
      padding: const EdgeInsets.all(18),
      child: Column(
        children: [
          for (var index = 0; index < rows.length; index++) ...[
            if (index > 0) const Divider(height: 16),
            rows[index],
          ],
        ],
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
          width: 128,
          child: Padding(
            padding: const EdgeInsets.only(top: 3),
            child: Text(label.toUpperCase(), style: AyurvedaType.eyebrow(context)),
          ),
        ),
        const SizedBox(width: 8),
        Expanded(
          child: isHighlight
              ? Text(
                  value,
                  style: AyurvedaType.price(context).copyWith(fontSize: 16),
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

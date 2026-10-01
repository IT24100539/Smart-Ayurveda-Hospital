import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/features/appointments/domain/appointment_models.dart';
import 'package:patient_app/src/features/health_hub/data/health_hub_repository.dart';
import 'package:patient_app/src/features/health_hub/domain/health_hub_models.dart';
import 'package:patient_app/src/features/health_hub/presentation/health_hub_screen.dart';
import 'package:patient_app/src/l10n/app_localizations.dart';
import 'package:patient_app/src/theme/app_theme.dart';

Widget _buildHealthHubTestHarness({
  required AsyncValue<List<TreatmentPlan>> plansState,
  required AsyncValue<RegistrationSummary> summaryState,
}) {
  return ProviderScope(
    overrides: [
      treatmentPlansProvider.overrideWith((ref) => plansState.when(
            data: (d) => Future.value(d),
            error: (e, s) => Future.error(e, s),
            loading: () => Future.value(<TreatmentPlan>[]),
          )),
      registrationSummaryProvider.overrideWith((ref) => summaryState.when(
            data: (d) => Future.value(d),
            error: (e, s) => Future.error(e, s),
            loading: () => Future.value(RegistrationSummary(
              uhid: 'SAH-DEFAULT',
              fullName: 'Default',
              phone: '0000000000',
              dateOfBirth: DateTime(2000, 1, 1),
              gender: 'Other',
              prakriti: 'Vata',
              vikriti: 'Pitta',
            )),
          )),
    ],
    child: MaterialApp(
      theme: AppTheme.light,
      localizationsDelegates: const [
        AppLocalizations.delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      supportedLocales: AppLocalizations.supportedLocales,
      home: const HealthHubScreen(),
    ),
  );
}

void main() {
  testWidgets('renders empty state when patient has no therapy sessions', (tester) async {
    final summary = RegistrationSummary(
      uhid: 'SAH-2026-0001',
      fullName: 'Sunil Shantha',
      phone: '0771234567',
      email: 'sunil@test.local',
      dateOfBirth: DateTime(1985, 4, 12),
      gender: 'Male',
      prakriti: 'Kapha',
      vikriti: 'Vata-Pitta',
      allergies: 'None',
      bloodGroup: 'O+',
    );

    await tester.pumpWidget(_buildHealthHubTestHarness(
      plansState: const AsyncValue.data([]),
      summaryState: AsyncValue.data(summary),
    ));
    await tester.pumpAndSettle();

    expect(find.text('My Health Hub'), findsWidgets);
    expect(find.text('My therapy sessions'), findsOneWidget);
    expect(find.text('No therapy sessions recorded yet.'), findsOneWidget);

    expect(find.text('My registration summary'), findsOneWidget);
    expect(find.text('SAH-2026-0001'), findsOneWidget);
    expect(find.text('Sunil Shantha'), findsOneWidget);
    expect(find.text('Kapha'), findsOneWidget);
  });

  testWidgets('renders treatment plans with session dates, slots, and status chips', (tester) async {
    final plan = TreatmentPlan(
      treatmentId: 't-101',
      treatmentName: 'Shirodhara Therapy',
      sessionCount: 2,
      lastStatus: AppointmentStatus.approved,
      nextDate: DateTime(2026, 10, 20),
      sessions: [
        TreatmentSession(
          appointmentId: 'a-1',
          date: DateTime(2026, 10, 10),
          timeSlot: '09:00 - 10:00',
          status: AppointmentStatus.completed,
        ),
        TreatmentSession(
          appointmentId: 'a-2',
          date: DateTime(2026, 10, 20),
          timeSlot: '11:00 - 12:00',
          status: AppointmentStatus.approved,
        ),
      ],
    );

    final summary = RegistrationSummary(
      uhid: 'SAH-2026-0002',
      fullName: 'Kamani Perera',
      phone: '0712345678',
      email: null,
      dateOfBirth: DateTime(1990, 8, 25),
      gender: 'Female',
      prakriti: 'Pitta',
      vikriti: 'Kapha',
      allergies: null,
      bloodGroup: null,
    );

    await tester.pumpWidget(_buildHealthHubTestHarness(
      plansState: AsyncValue.data([plan]),
      summaryState: AsyncValue.data(summary),
    ));
    await tester.pumpAndSettle();

    expect(find.text('Shirodhara Therapy'), findsOneWidget);
    expect(find.text('Next Session: Tue, Oct 20, 2026'), findsOneWidget);
    expect(find.text('09:00 - 10:00'), findsOneWidget);
    expect(find.text('11:00 - 12:00'), findsOneWidget);
    expect(find.text('SAH-2026-0002'), findsOneWidget);
    expect(find.text('Kamani Perera'), findsOneWidget);
    expect(find.text('Not recorded'), findsWidgets);
  });

  testWidgets('renders error state with retry when treatment plans fail to load', (tester) async {
    final summary = RegistrationSummary(
      uhid: 'SAH-2026-0003',
      fullName: 'Anura Kumara',
      phone: '0781234567',
      dateOfBirth: DateTime(1975, 1, 1),
      gender: 'Male',
      prakriti: 'Vata',
      vikriti: 'Vata',
    );

    await tester.pumpWidget(_buildHealthHubTestHarness(
      plansState: AsyncValue.error('Network Error', StackTrace.empty),
      summaryState: AsyncValue.data(summary),
    ));
    await tester.pumpAndSettle();

    expect(find.text('Could not load health records.'), findsOneWidget);
    expect(find.text('Try again'), findsOneWidget);
  });
}

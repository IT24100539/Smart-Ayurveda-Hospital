import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/features/appointments/domain/appointment_models.dart';
import 'package:patient_app/src/features/appointments/presentation/appointments_screen.dart';
import 'package:patient_app/src/features/health_hub/application/upcoming_appointments.dart';
import 'package:patient_app/src/features/health_hub/data/health_hub_repository.dart';
import 'package:patient_app/src/features/health_hub/domain/health_hub_models.dart';
import 'package:patient_app/src/features/health_hub/presentation/health_hub_screen.dart';
import 'package:patient_app/src/features/records/application/records_provider.dart';
import 'package:patient_app/src/features/records/domain/patient_records.dart';
import 'package:patient_app/src/l10n/app_localizations.dart';
import 'package:patient_app/src/theme/app_theme.dart';

final _summary = RegistrationSummary(
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

Appointment _appointment(
  String id,
  String treatment,
  DateTime date,
  AppointmentStatus status, {
  String slot = '09:00 - 10:00',
}) {
  return Appointment(
    id: id,
    treatmentName: treatment,
    requestedDate: date,
    requestedTimeSlot: slot,
    status: status,
  );
}

TreatmentPlan _plan() {
  return TreatmentPlan(
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
}

Widget _harness({
  List<TreatmentPlan>? plans,
  RegistrationSummary? summary,
  List<Appointment> appointments = const [],
  Object? appointmentsError,
  Object? plansError,
  ThemeData? theme,
  int initialTab = 0,
}) {
  return ProviderScope(
    key: UniqueKey(),
    overrides: [
      treatmentPlansProvider.overrideWith((ref) async {
        if (plansError != null) throw plansError;
        return plans ?? const <TreatmentPlan>[];
      }),
      registrationSummaryProvider.overrideWith(
        (ref) async => summary ?? _summary,
      ),
      myAppointmentsProvider.overrideWith((ref) async {
        if (appointmentsError != null) throw appointmentsError;
        return appointments;
      }),
      prescriptionsProvider.overrideWith((ref) async => const <Prescription>[]),
      invoicesProvider.overrideWith((ref) async => const <Invoice>[]),
      documentsProvider.overrideWith((ref) async => const <MedicalDocument>[]),
    ],
    child: MaterialApp(
      theme: theme ?? AppTheme.light,
      locale: const Locale('en'),
      localizationsDelegates: const [
        AppLocalizations.delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      supportedLocales: AppLocalizations.supportedLocales,
      home: HealthHubScreen(initialTab: initialTab),
    ),
  );
}

void _setSize(WidgetTester tester, Size size) {
  tester.view.physicalSize = size;
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
}

final _soon = DateTime(2099, 3, 14);
final _later = DateTime(2099, 4, 2);
final _past = DateTime(2000, 1, 5);

void main() {
  group('upcoming appointments', () {
    test('keeps open future appointments, soonest first', () {
      final now = DateTime(2026, 10, 2, 22, 30);
      final result = upcomingAppointments([
        _appointment('late', 'Abhyanga', DateTime(2026, 11, 1), AppointmentStatus.pending),
        _appointment('today', 'Basti', DateTime(2026, 10, 2), AppointmentStatus.approved),
        _appointment('yesterday', 'Nasya', DateTime(2026, 10, 1), AppointmentStatus.approved),
        _appointment('done', 'Vamana', DateTime(2026, 10, 5), AppointmentStatus.completed),
        _appointment('cancelled', 'Virechana', DateTime(2026, 10, 6), AppointmentStatus.cancelled),
        _appointment('rejected', 'Pizhichil', DateTime(2026, 10, 7), AppointmentStatus.rejected),
        _appointment('soon', 'Shirodhara', DateTime(2026, 10, 9), AppointmentStatus.pending),
      ], now);

      expect(result.map((a) => a.id), ['today', 'soon', 'late']);
    });

    test('tab index maps deep-link names', () {
      expect(healthHubTabIndex('therapy'), 1);
      expect(healthHubTabIndex('registration'), 2);
      expect(healthHubTabIndex('prescriptions'), 3);
      expect(healthHubTabIndex('invoices'), 4);
      expect(healthHubTabIndex('documents'), 5);
      expect(healthHubTabIndex('unknown'), 0);
      expect(healthHubTabIndex(null), 0);
    });
  });

  testWidgets('header shows name, UHID and registered details only', (tester) async {
    await tester.pumpWidget(_harness());
    await tester.pumpAndSettle();

    expect(find.text('My Health Hub'), findsOneWidget);
    expect(find.text('Sunil Shantha'), findsOneWidget);
    expect(find.text('SS'), findsOneWidget);
    expect(find.text('SAH-2026-0001'), findsOneWidget);
    expect(find.text('Male · Apr 12, 1985'), findsOneWidget);
    expect(
      find.text('Therapy sessions, prescriptions, invoices, and documents.'),
      findsNothing,
    );
  });

  testWidgets('lists only the tabs backed by data, with count badges', (tester) async {
    await tester.pumpWidget(_harness(
      plans: [_plan()],
      appointments: [
        _appointment('1', 'Abhyanga', _soon, AppointmentStatus.approved),
        _appointment('2', 'Basti', _later, AppointmentStatus.pending),
        _appointment('3', 'Nasya', _past, AppointmentStatus.approved),
        _appointment('4', 'Vamana', _soon, AppointmentStatus.cancelled),
      ],
    ));
    await tester.pumpAndSettle();

    for (final label in [
      'Upcoming',
      'Therapy',
      'Registration',
      'Prescriptions',
      'Invoices',
      'Documents',
    ]) {
      expect(find.text(label), findsOneWidget, reason: label);
    }
    expect(find.text('Care'), findsNothing);

    Finder badge(Key tab, String count) =>
        find.descendant(of: find.byKey(tab), matching: find.text(count));
    expect(badge(HealthHubTabKeys.upcoming, '2'), findsOneWidget);
    expect(badge(HealthHubTabKeys.therapy, '1'), findsOneWidget);
    // Empty lists show no badge.
    expect(badge(HealthHubTabKeys.prescriptions, '0'), findsNothing);
  });

  testWidgets('upcoming appointment cards use status pills and uppercase labels', (tester) async {
    await tester.pumpWidget(_harness(
      appointments: [
        _appointment('1', 'Abhyanga', _soon, AppointmentStatus.approved, slot: '10:00 - 11:00'),
        _appointment('2', 'Basti', _later, AppointmentStatus.pending),
        _appointment('3', 'Nasya', _past, AppointmentStatus.approved),
      ],
    ));
    await tester.pumpAndSettle();

    expect(find.text('Abhyanga'), findsOneWidget);
    expect(find.text('Basti'), findsOneWidget);
    expect(find.text('Nasya'), findsNothing);
    expect(find.text('TREATMENT'), findsNWidgets(2));
    expect(find.text('DATE'), findsNWidgets(2));
    expect(find.text('TIME'), findsNWidgets(2));
    expect(find.text('Treatment'), findsNothing);
    expect(find.text('10:00 - 11:00'), findsOneWidget);
    expect(find.byKey(const ValueKey('status-chip-approved')), findsOneWidget);
    expect(find.byKey(const ValueKey('status-chip-pending')), findsOneWidget);
  });

  testWidgets('upcoming tab shows empty and error states', (tester) async {
    await tester.pumpWidget(_harness());
    await tester.pumpAndSettle();
    expect(find.text('No upcoming appointments.'), findsOneWidget);

    await tester.pumpWidget(_harness(appointmentsError: StateError('down')));
    await tester.pumpAndSettle();
    expect(find.text('Could not load health records.'), findsOneWidget);
    expect(find.text('Try again'), findsOneWidget);
  });

  testWidgets('therapy tab shows an empty state when there are no sessions', (tester) async {
    await tester.pumpWidget(_harness());
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(HealthHubTabKeys.therapy));
    await tester.pumpAndSettle();

    expect(find.text('No therapy sessions recorded yet.'), findsOneWidget);
  });

  testWidgets('therapy tab shows plans with sessions, slots and status pills', (tester) async {
    await tester.pumpWidget(_harness(plans: [_plan()]));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(HealthHubTabKeys.therapy));
    await tester.pumpAndSettle();

    expect(find.text('Shirodhara Therapy'), findsOneWidget);
    expect(find.text('2 sessions'), findsOneWidget);
    expect(find.text('NEXT SESSION'), findsOneWidget);
    expect(find.text('Tue, Oct 20, 2026'), findsOneWidget);
    expect(find.text('Oct 10, 2026'), findsOneWidget);
    expect(find.text('09:00 - 10:00'), findsOneWidget);
    expect(find.text('11:00 - 12:00'), findsOneWidget);
    expect(find.byKey(const ValueKey('status-chip-completed')), findsOneWidget);
    expect(find.byKey(const ValueKey('status-chip-approved')), findsOneWidget);
  });

  testWidgets('therapy tab shows an error with retry', (tester) async {
    await tester.pumpWidget(_harness(plansError: StateError('Network Error')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(HealthHubTabKeys.therapy));
    await tester.pumpAndSettle();

    expect(find.text('Could not load health records.'), findsOneWidget);
    expect(find.text('Try again'), findsOneWidget);
  });

  testWidgets('registration tab shows the registered details', (tester) async {
    await tester.pumpWidget(_harness());
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(HealthHubTabKeys.registration));
    await tester.pumpAndSettle();

    expect(find.text('UHID'), findsWidgets);
    expect(find.text('0771234567'), findsOneWidget);
    expect(find.text('sunil@test.local'), findsOneWidget);
    expect(find.text('1985-04-12'), findsOneWidget);
    expect(find.text('Kapha'), findsOneWidget);
    expect(find.text('Vata-Pitta'), findsOneWidget);
    expect(find.text('O+'), findsOneWidget);
  });

  testWidgets('registration tab marks missing details as not recorded', (tester) async {
    await tester.pumpWidget(_harness(
      summary: RegistrationSummary(
        uhid: 'SAH-2026-0002',
        fullName: 'Kamani Perera',
        phone: '0712345678',
        dateOfBirth: DateTime(1990, 8, 25),
        gender: 'Female',
        prakriti: 'Pitta',
        vikriti: '',
      ),
      initialTab: 2,
    ));
    await tester.pumpAndSettle();

    expect(find.text('KP'), findsOneWidget);
    expect(find.text('Not recorded'), findsNWidgets(4));
  });

  testWidgets('deep link opens the requested tab', (tester) async {
    await tester.pumpWidget(_harness(initialTab: healthHubTabIndex('invoices')));
    await tester.pumpAndSettle();

    expect(find.text('No invoices have been issued yet.'), findsOneWidget);
  });

  group('layout', () {
    final appointments = [
      _appointment('1', 'Abhyanga', _soon, AppointmentStatus.approved),
      _appointment('2', 'Basti', _later, AppointmentStatus.pending),
    ];

    testWidgets('stacks cards on a phone', (tester) async {
      _setSize(tester, const Size(390, 844));
      await tester.pumpWidget(_harness(appointments: appointments));
      await tester.pumpAndSettle();

      expect(find.byKey(const ValueKey('responsive-column-start')), findsNothing);
      final first = tester.getTopLeft(find.text('Abhyanga'));
      final second = tester.getTopLeft(find.text('Basti'));
      expect(second.dy, greaterThan(first.dy));
      expect(second.dx, first.dx);
    });

    testWidgets('uses two columns on a wide screen', (tester) async {
      _setSize(tester, const Size(1200, 900));
      await tester.pumpWidget(_harness(appointments: appointments));
      await tester.pumpAndSettle();

      expect(find.byKey(const ValueKey('responsive-column-start')), findsOneWidget);
      expect(find.byKey(const ValueKey('responsive-column-end')), findsOneWidget);
      final first = tester.getTopLeft(find.text('Abhyanga'));
      final second = tester.getTopLeft(find.text('Basti'));
      expect(second.dx, greaterThan(first.dx));
      expect(second.dy, first.dy);
    });

    testWidgets('registration cards sit side by side on a wide screen', (tester) async {
      _setSize(tester, const Size(1200, 900));
      await tester.pumpWidget(_harness(initialTab: 2));
      await tester.pumpAndSettle();

      final uhid = tester.getTopLeft(find.text('SAH-2026-0001').last);
      final prakriti = tester.getTopLeft(find.text('Kapha'));
      expect(prakriti.dx, greaterThan(uhid.dx));
    });
  });

  group('themes', () {
    for (final entry in {'light': AppTheme.light, 'dark': AppTheme.dark}.entries) {
      testWidgets('renders every tab in the ${entry.key} theme', (tester) async {
        _setSize(tester, const Size(390, 844));
        await tester.pumpWidget(_harness(
          theme: entry.value,
          plans: [_plan()],
          appointments: [
            _appointment('1', 'Abhyanga', _soon, AppointmentStatus.approved),
          ],
        ));
        await tester.pumpAndSettle();

        expect(find.text('Abhyanga'), findsOneWidget);
        for (final tab in [
          HealthHubTabKeys.therapy,
          HealthHubTabKeys.registration,
          HealthHubTabKeys.prescriptions,
          HealthHubTabKeys.invoices,
          HealthHubTabKeys.documents,
        ]) {
          await tester.ensureVisible(find.byKey(tab));
          await tester.tap(find.byKey(tab));
          await tester.pumpAndSettle();
        }
        expect(tester.takeException(), isNull);
      });
    }
  });
}


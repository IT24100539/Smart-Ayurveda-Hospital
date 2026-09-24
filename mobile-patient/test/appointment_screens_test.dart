import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/features/appointments/data/appointment_repository.dart';
import 'package:patient_app/src/features/appointments/domain/appointment_models.dart';
import 'package:patient_app/src/features/appointments/presentation/appointments_screen.dart';
import 'package:patient_app/src/features/appointments/presentation/book_appointment_flow.dart';
import 'package:patient_app/src/theme/app_theme.dart';
import 'package:patient_app/src/l10n/app_localizations.dart';
import 'package:patient_app/src/l10n/locale_controller.dart';

class _FakeAppointmentRepository implements AppointmentRepository {
  _FakeAppointmentRepository({
    this.availabilityByDate = const {},
    this.appointments = const [],
  });

  final Map<DateTime, TreatmentAvailability> availabilityByDate;
  final List<Appointment> appointments;

  @override
  Future<TreatmentAvailability> availability(
    String treatmentId,
    DateTime date,
  ) async =>
      availabilityByDate[DateUtils.dateOnly(date)] ??
      const TreatmentAvailability(available: false, slots: []);

  @override
  Future<void> cancel(String id) async {}

  @override
  Future<Appointment> create({
    required String patientId,
    required TreatmentBooking treatment,
    required DateTime date,
    required TreatmentSlot slot,
  }) async => Appointment(
    id: 'created',
    treatmentName: treatment.name,
    requestedDate: date,
    requestedTimeSlot: slot.time,
    status: AppointmentStatus.pending,
  );

  @override
  Future<List<Appointment>> mine() async => appointments;
}

Future<void> _pump(
  WidgetTester tester,
  Widget child,
  AppointmentRepository repository, {
  Locale locale = const Locale('en'),
}) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [appointmentRepositoryProvider.overrideWithValue(repository)],
      child: MaterialApp(
        theme: AppTheme.light,
        locale: locale,
        supportedLocales: supportedLocales,
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        home: child,
      ),
    ),
  );
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('date step disables a day reported unavailable', (tester) async {
    final unavailable = DateTime(2026, 9, 14);
    final available = DateTime(2026, 9, 15);
    final repository = _FakeAppointmentRepository(
      availabilityByDate: {
        unavailable: const TreatmentAvailability(available: false, slots: []),
        available: const TreatmentAvailability(
          available: true,
          slots: [TreatmentSlot(time: '09:00-10:00')],
        ),
      },
    );

    await _pump(
      tester,
      BookAppointmentFlow(
        treatment: const TreatmentBooking(id: 'treatment-1', name: 'Abhyanga'),
        candidateDates: [unavailable, available],
      ),
      repository,
    );

    final unavailableButton = tester.widget<OutlinedButton>(
      find.byKey(BookAppointmentKeys.date(unavailable)),
    );
    final availableButton = tester.widget<OutlinedButton>(
      find.byKey(BookAppointmentKeys.date(available)),
    );
    expect(unavailableButton.onPressed, isNull);
    expect(availableButton.onPressed, isNotNull);
  });

  testWidgets('appointment statuses render their defined chip colors', (
    tester,
  ) async {
    final statuses = AppointmentStatus.values;
    final repository = _FakeAppointmentRepository(
      appointments: [
        for (var index = 0; index < statuses.length; index++)
          Appointment(
            id: '$index',
            treatmentName: 'Treatment $index',
            requestedDate: DateTime(2026, 9, 20 + index),
            requestedTimeSlot: '09:00-10:00',
            status: statuses[index],
          ),
      ],
    );

    await _pump(tester, const MyAppointmentsScreen(), repository);

    for (final status in statuses) {
      final chip = tester.widget<Chip>(
        find.byKey(ValueKey('status-chip-${status.name}')),
      );
      expect(
        chip.backgroundColor,
        AppointmentStatusColors.background(status),
        reason: '${status.name} should use its semantic status color',
      );
    }
  });

  testWidgets('appointments screen translates its copy to Sinhala', (
    tester,
  ) async {
    final repository = _FakeAppointmentRepository(
      appointments: [
        Appointment(
          id: '1',
          treatmentName: 'Abhyanga',
          requestedDate: DateTime(2026, 9, 20),
          requestedTimeSlot: '09:00-10:00',
          status: AppointmentStatus.pending,
        ),
      ],
    );

    await _pump(
      tester,
      const MyAppointmentsScreen(),
      repository,
      locale: const Locale('si'),
    );

    expect(find.text('මගේ හමුවීම්'), findsOneWidget);
    expect(find.text('බලාපොරොත්තුවෙන්'), findsOneWidget);
    expect(find.text('විස්තර සඳහා තට්ටු කරන්න'), findsOneWidget);
  });
}

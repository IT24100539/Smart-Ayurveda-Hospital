import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/core/network/api_exception.dart';
import 'package:patient_app/src/features/appointments/data/appointment_repository.dart';
import 'package:patient_app/src/features/appointments/domain/appointment_models.dart';
import 'package:patient_app/src/features/appointments/presentation/appointments_screen.dart';
import 'package:patient_app/src/features/appointments/presentation/book_appointment_flow.dart';
import 'package:patient_app/src/features/auth/application/auth_controller.dart';
import 'package:patient_app/src/features/auth/domain/auth_models.dart';
import 'package:patient_app/src/theme/app_theme.dart';
import 'package:patient_app/src/l10n/app_localizations.dart';
import 'package:patient_app/src/l10n/locale_controller.dart';

class _FakeAppointmentRepository implements AppointmentRepository {
  _FakeAppointmentRepository({
    this.availabilityByDate = const {},
    this.appointments = const [],
    this.createError,
  });

  final Map<DateTime, TreatmentAvailability> availabilityByDate;
  final List<Appointment> appointments;
  final Object? createError;

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
  }) async {
    final error = createError;
    if (error != null) throw error;
    return Appointment(
    id: 'created',
    treatmentName: treatment.name,
    requestedDate: date,
    requestedTimeSlot: slot.time,
    status: AppointmentStatus.pending,
    );
  }

  @override
  Future<List<Appointment>> mine() async => appointments;
}

class _SignedInAuth extends AuthController {
  @override
  AuthState build() => const AuthState(
    status: AuthStatus.authenticated,
    user: AuthUser(
      id: '11111111-1111-1111-1111-111111111111',
      fullName: 'Meera Nair',
      email: 'meera.nair@example.local',
      phoneNumber: '9876500001',
      role: UserRole.patient,
    ),
  );
}

Future<void> _pump(
  WidgetTester tester,
  Widget child,
  AppointmentRepository repository, {
  Locale locale = const Locale('en'),
  bool signedIn = false,
}) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        appointmentRepositoryProvider.overrideWithValue(repository),
        if (signedIn) authControllerProvider.overrideWith(_SignedInAuth.new),
      ],
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

  testWidgets('submit shows pending staff approval, not a booked confirmation', (
    tester,
  ) async {
    final available = DateTime(2026, 9, 30);
    final repository = _FakeAppointmentRepository(
      availabilityByDate: {
        available: const TreatmentAvailability(
          available: true,
          slots: [TreatmentSlot(time: '08:00-12:00', scheduleId: 'schedule-1')],
        ),
      },
    );

    await _pump(
      tester,
      BookAppointmentFlow(
        treatment: const TreatmentBooking(
          id: '19415cdb-746a-4729-8349-02bc4e632cf7',
          name: 'Panchakarma',
        ),
        candidateDates: [available],
      ),
      repository,
      signedIn: true,
    );

    await tester.tap(find.byKey(BookAppointmentKeys.date(available)));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(BookAppointmentKeys.next));
    await tester.pumpAndSettle();
    await tester.tap(find.text('08:00-12:00'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Review'));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(BookAppointmentKeys.submit));
    await tester.pumpAndSettle();

    expect(find.byKey(BookAppointmentKeys.pendingConfirmation), findsOneWidget);
    expect(
      find.text('Request sent - pending staff approval'),
      findsOneWidget,
    );
    expect(find.textContaining('booked'), findsNothing);
  });

  testWidgets('submit shows the backend validation message', (tester) async {
    final available = DateTime(2026, 9, 30);
    final repository = _FakeAppointmentRepository(
      createError: const ApiException(
        statusCode: 400,
        detail: 'One or more validation errors occurred.',
        fieldErrors: {
          'requestedtimeslot': ['Requested time slot does not match schedule time slot.'],
        },
      ),
      availabilityByDate: {
        available: const TreatmentAvailability(
          available: true,
          slots: [TreatmentSlot(time: '08:00-12:00', scheduleId: 'schedule-1')],
        ),
      },
    );

    await _pump(
      tester,
      BookAppointmentFlow(
        treatment: const TreatmentBooking(
          id: '19415cdb-746a-4729-8349-02bc4e632cf7',
          name: 'Panchakarma',
        ),
        candidateDates: [available],
      ),
      repository,
      signedIn: true,
    );

    await tester.tap(find.byKey(BookAppointmentKeys.date(available)));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(BookAppointmentKeys.next));
    await tester.pumpAndSettle();
    await tester.tap(find.text('08:00-12:00'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Review'));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(BookAppointmentKeys.submit));
    await tester.pumpAndSettle();

    expect(
      find.text('Requested time slot does not match schedule time slot.'),
      findsOneWidget,
    );
    expect(find.textContaining('ApiException'), findsNothing);
    expect(find.text('One or more validation errors occurred.'), findsNothing);
    expect(find.byKey(BookAppointmentKeys.pendingConfirmation), findsNothing);
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
      final chipFinder = find.byKey(ValueKey('status-chip-${status.name}'));
      await tester.scrollUntilVisible(chipFinder, 200);
      final chip = tester.widget<Chip>(chipFinder);
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

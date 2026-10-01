import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/features/appointments/domain/appointment_models.dart';
import 'package:patient_app/src/features/appointments/presentation/appointments_screen.dart';
import 'package:patient_app/src/features/auth/application/auth_controller.dart';
import 'package:patient_app/src/features/auth/domain/auth_models.dart';
import 'package:patient_app/src/features/home/presentation/home_screen.dart';
import 'package:patient_app/src/features/treatments/application/treatments_provider.dart';
import 'package:patient_app/src/features/treatments/domain/treatment_models.dart';
import 'package:patient_app/src/l10n/app_localizations.dart';
import 'package:patient_app/src/theme/app_theme.dart';

class _FakeAuth extends AuthController {
  @override
  AuthState build() => const AuthState(
        status: AuthStatus.authenticated,
        user: AuthUser(
          id: '11111111-1111-1111-1111-111111111111',
          fullName: 'Saman Perera',
          email: 'saman@test.local',
          phoneNumber: '0711234567',
          role: UserRole.patient,
        ),
      );
}

final _mockTreatments = [
  const Treatment(
    id: 't-1',
    nameEnglish: 'Panchakarma Detox',
    nameSinhala: 'පංචකර්ම',
    description: 'Comprehensive classical detox',
    scheduleDays: ['Monday', 'Wednesday'],
  ),
  const Treatment(
    id: 't-2',
    nameEnglish: 'Shirodhara',
    nameSinhala: 'ශිරෝධාරා',
    description: 'Continuous warm oil stream',
    scheduleDays: ['Tuesday', 'Thursday'],
  ),
];

Widget _buildHomeTestHarness({
  required List<Appointment> appointments,
}) {
  return ProviderScope(
    overrides: [
      authControllerProvider.overrideWith(() => _FakeAuth()),
      treatmentsProvider.overrideWith((ref) => Future.value(_mockTreatments)),
      myAppointmentsProvider.overrideWith((ref) => Future.value(appointments)),
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
      home: const HomeScreen(),
    ),
  );
}

void main() {
  testWidgets('renders empty upcoming state when patient has no appointments',
      (tester) async {
    await tester.pumpWidget(_buildHomeTestHarness(appointments: []));
    await tester.pumpAndSettle();

    expect(find.text('No Upcoming Appointments'), findsOneWidget);
    expect(find.text('Schedule your consultation or wellness session'),
        findsOneWidget);
    expect(find.text('Featured Therapies'), findsOneWidget);
    expect(find.text('Panchakarma Detox'), findsOneWidget);
  });

  testWidgets(
      'renders pinned upcoming appointment card with countdown badge when future appointment exists',
      (tester) async {
    final tomorrow = DateTime.now().add(const Duration(days: 1));
    final futureAppt = Appointment(
      id: 'appt-123',
      treatmentName: 'Shirodhara Wellness',
      requestedDate: tomorrow,
      requestedTimeSlot: '10:00 - 11:00',
      status: AppointmentStatus.approved,
    );

    await tester.pumpWidget(_buildHomeTestHarness(appointments: [futureAppt]));
    await tester.pumpAndSettle();

    expect(find.text('Upcoming Appointment'), findsOneWidget);
    expect(find.text('Tomorrow'), findsOneWidget);
    expect(find.text('Shirodhara Wellness'), findsOneWidget);
    expect(find.text('10:00 - 11:00'), findsOneWidget);
  });
}

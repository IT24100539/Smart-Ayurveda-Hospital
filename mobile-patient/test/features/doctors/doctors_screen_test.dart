import 'dart:async';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:patient_app/src/features/appointments/domain/appointment_models.dart';
import 'package:patient_app/src/features/appointments/presentation/appointments_screen.dart';
import 'package:patient_app/src/features/auth/application/auth_controller.dart';
import 'package:patient_app/src/features/auth/domain/auth_models.dart';
import 'package:patient_app/src/features/feedback/application/communication_providers.dart';
import 'package:patient_app/src/features/feedback/domain/communication_models.dart';
import 'package:patient_app/src/features/doctors/application/doctors_provider.dart';
import 'package:patient_app/src/features/doctors/data/doctors_repository.dart';
import 'package:patient_app/src/features/doctors/domain/doctor.dart';
import 'package:patient_app/src/features/doctors/presentation/doctor_profile_screen.dart';
import 'package:patient_app/src/features/doctors/presentation/doctors_screen.dart';
import 'package:patient_app/src/features/doctors/presentation/widgets/doctor_skeleton.dart';
import 'package:patient_app/src/features/home/presentation/home_screen.dart';
import 'package:patient_app/src/features/treatments/application/treatments_provider.dart';
import 'package:patient_app/src/l10n/app_localizations.dart';
import 'package:patient_app/src/router/app_routes.dart';
import 'package:patient_app/src/theme/app_theme.dart';

const _anjali = Doctor(
  id: 'd1',
  name: 'Anjali Perera',
  specialty: 'Kayachikitsa',
  qualifications: 'BAMS, MD (Ayu)',
  bio: 'Consults on vikriti and panchakarma.',
  isActive: true,
  isSample: false,
  hasPhoto: false,
);

const _nimal = Doctor(
  id: 'd2',
  name: 'Nimal Silva',
  specialty: 'Shalya Tantra',
  qualifications: 'BAMS',
  isActive: true,
  isSample: false,
  hasPhoto: true,
  rating: 4.5,
  ratingCount: 2,
);

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

class _DirectoryRepository implements DoctorsRepository {
  _DirectoryRepository(this.doctors);

  final List<Doctor> doctors;
  String? lastQuery;

  @override
  Future<Doctor> getDoctor(String id) async {
    return doctors.firstWhere((doctor) => doctor.id == id);
  }

  @override
  Future<List<Doctor>> listDoctors({String? query}) async {
    lastQuery = query;
    final trimmed = query?.trim().toLowerCase() ?? '';
    if (trimmed.isEmpty) return doctors;
    return doctors
        .where(
          (doctor) =>
              doctor.name.toLowerCase().contains(trimmed) ||
              doctor.specialty.toLowerCase().contains(trimmed),
        )
        .toList();
  }

  @override
  Future<Uint8List?> loadPhoto(String id) async => Uint8List.fromList(const [
    137,
    80,
    78,
    71,
    13,
    10,
    26,
    10,
  ]);
}

Widget _app({
  required Widget home,
  List<Override> overrides = const [],
}) {
  return ProviderScope(
    overrides: overrides,
    child: MaterialApp(
      theme: AppTheme.light,
      locale: const Locale('en'),
      localizationsDelegates: const [
        AppLocalizations.delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      supportedLocales: AppLocalizations.supportedLocales,
      home: home,
    ),
  );
}

void main() {
  test('physician routes stay behind sign-in', () {
    expect(AppRoutes.isPublic(AppRoutes.doctors), isFalse);
    expect(AppRoutes.isPublic(AppRoutes.doctorById('d1')), isFalse);
    expect(AppRoutes.doctorById('d1'), '/doctors/d1');
  });

  testWidgets('shows a skeleton while the directory loads', (tester) async {
    await tester.pumpWidget(
      _app(
        home: const DoctorsScreen(),
        overrides: [
          doctorsListProvider.overrideWith(
            (ref) => Completer<List<Doctor>>().future,
          ),
        ],
      ),
    );
    await tester.pump();

    expect(find.byKey(DoctorsScreenKeys.skeleton), findsOneWidget);
    expect(find.text('4.5'), findsNothing);
  });

  testWidgets('shows an empty directory without a placeholder rating', (
    tester,
  ) async {
    await tester.pumpWidget(
      _app(
        home: const DoctorsScreen(),
        overrides: [
          doctorsListProvider.overrideWith((ref) async => const <Doctor>[]),
        ],
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('No physicians are listed yet.'), findsOneWidget);
    expect(find.textContaining('review'), findsNothing);
  });

  testWidgets('lists physicians with initials or a real rating only', (
    tester,
  ) async {
    final repository = _DirectoryRepository([_anjali, _nimal]);
    final router = GoRouter(
      initialLocation: AppRoutes.doctors,
      routes: [
        GoRoute(
          path: AppRoutes.doctors,
          builder: (context, state) => const DoctorsScreen(),
          routes: [
            GoRoute(
              path: ':id',
              builder: (context, state) => DoctorProfileScreen(
                doctorId: state.pathParameters['id']!,
              ),
            ),
          ],
        ),
      ],
    );
    addTearDown(router.dispose);

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          doctorsRepositoryProvider.overrideWithValue(repository),
          doctorPhotoProvider.overrideWith(
            (ref, id) async => Uint8List.fromList(_png),
          ),
        ],
        child: MaterialApp.router(
          theme: AppTheme.light,
          locale: const Locale('en'),
          routerConfig: router,
          localizationsDelegates: const [
            AppLocalizations.delegate,
            GlobalMaterialLocalizations.delegate,
            GlobalWidgetsLocalizations.delegate,
            GlobalCupertinoLocalizations.delegate,
          ],
          supportedLocales: AppLocalizations.supportedLocales,
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Anjali Perera'), findsOneWidget);
    expect(find.text('AP'), findsOneWidget);
    expect(find.text('Kayachikitsa'), findsOneWidget);
    expect(find.text('4.5 from 2 reviews'), findsOneWidget);
    expect(find.byType(Image), findsOneWidget);
    expect(find.text('5.0'), findsNothing);

    await tester.enterText(find.byKey(DoctorsScreenKeys.search), 'shaly');
    await tester.pumpAndSettle();

    expect(repository.lastQuery, 'shaly');
    expect(find.text('Nimal Silva'), findsOneWidget);
    expect(find.text('Anjali Perera'), findsNothing);
    expect(find.text('No physicians match your search.'), findsNothing);

    await tester.enterText(find.byKey(DoctorsScreenKeys.search), 'missing');
    await tester.pumpAndSettle();
    expect(find.text('No physicians match your search.'), findsOneWidget);

    await tester.enterText(find.byKey(DoctorsScreenKeys.search), '');
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey('doctor-tile-d1')));
    await tester.pumpAndSettle();

    expect(find.text('BAMS, MD (Ayu)'), findsOneWidget);
    expect(find.text('Consults on vikriti and panchakarma.'), findsOneWidget);
    expect(find.text('Qualifications'), findsOneWidget);
    expect(find.byKey(DoctorProfileKeys.rating), findsNothing);
    expect(find.textContaining('review'), findsNothing);
  });

  testWidgets('directory error offers retry', (tester) async {
    var attempts = 0;
    await tester.pumpWidget(
      _app(
        home: const DoctorsScreen(),
        overrides: [
          doctorsListProvider.overrideWith((ref) async {
            attempts++;
            if (attempts == 1) throw StateError('down');
            return const [_anjali];
          }),
        ],
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Could not load physicians.'), findsOneWidget);
    await tester.tap(find.byKey(DoctorsScreenKeys.retry));
    await tester.pumpAndSettle();

    expect(attempts, 2);
    expect(find.text('Anjali Perera'), findsOneWidget);
  });

  testWidgets('profile shows a skeleton while the physician loads', (
    tester,
  ) async {
    await tester.pumpWidget(
      _app(
        home: const DoctorProfileScreen(doctorId: 'd2'),
        overrides: [
          doctorProvider.overrideWith(
            (ref, id) => Completer<Doctor>().future,
          ),
        ],
      ),
    );
    await tester.pump();
    expect(find.byKey(DoctorProfileKeys.skeleton), findsOneWidget);
  });

  testWidgets('profile shows a real rating and retries after an error', (
    tester,
  ) async {
    var attempts = 0;
    await tester.pumpWidget(
      _app(
        home: const DoctorProfileScreen(doctorId: 'd2'),
        overrides: [
          doctorProvider.overrideWith((ref, id) async {
            attempts++;
            if (attempts == 1) throw StateError('down');
            return _nimal;
          }),
          doctorPhotoProvider.overrideWith((ref, id) async => null),
        ],
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('Could not load this physician.'), findsOneWidget);

    await tester.tap(find.byKey(DoctorProfileKeys.retry));
    await tester.pumpAndSettle();

    expect(find.text('Nimal Silva'), findsWidgets);
    expect(find.text('4.5 from 2 reviews'), findsOneWidget);
    expect(find.byKey(DoctorProfileKeys.rating), findsOneWidget);
    expect(find.text('NS'), findsOneWidget);
    expect(find.byType(Image), findsNothing);
  });

  testWidgets('home links into the physician directory', (tester) async {
    final router = GoRouter(
      initialLocation: '/home',
      routes: [
        GoRoute(
          path: '/home',
          builder: (context, state) => const HomeScreen(),
        ),
        GoRoute(
          path: AppRoutes.doctors,
          builder: (context, state) => const Scaffold(body: Text('directory')),
        ),
      ],
    );
    addTearDown(router.dispose);

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          authControllerProvider.overrideWith(_SignedInAuth.new),
          treatmentsProvider.overrideWith((ref) async => const []),
          myAppointmentsProvider.overrideWith(
            (ref) async => const <Appointment>[],
          ),
          notificationsProvider.overrideWith(
            (ref) async => const <PatientNotification>[],
          ),
        ],
        child: MaterialApp.router(
          theme: AppTheme.light,
          locale: const Locale('en'),
          routerConfig: router,
          localizationsDelegates: const [
            AppLocalizations.delegate,
            GlobalMaterialLocalizations.delegate,
            GlobalWidgetsLocalizations.delegate,
            GlobalCupertinoLocalizations.delegate,
          ],
          supportedLocales: AppLocalizations.supportedLocales,
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.scrollUntilVisible(
      find.text('Physicians'),
      300,
      scrollable: find.byType(Scrollable).first,
    );
    await tester.tap(find.text('Physicians'));
    await tester.pumpAndSettle();
    expect(find.text('directory'), findsOneWidget);
  });
}

/// 1x1 transparent PNG.
const _png = <int>[
  137,
  80,
  78,
  71,
  13,
  10,
  26,
  10,
  0,
  0,
  0,
  13,
  73,
  72,
  68,
  82,
  0,
  0,
  0,
  1,
  0,
  0,
  0,
  1,
  8,
  6,
  0,
  0,
  0,
  31,
  21,
  196,
  137,
  0,
  0,
  0,
  13,
  73,
  68,
  65,
  84,
  120,
  156,
  99,
  248,
  207,
  192,
  240,
  31,
  0,
  5,
  133,
  2,
  0,
  30,
  17,
  6,
  2,
  132,
  169,
  140,
  33,
  0,
  0,
  0,
  0,
  73,
  69,
  78,
  68,
  174,
  66,
  96,
  130,
];

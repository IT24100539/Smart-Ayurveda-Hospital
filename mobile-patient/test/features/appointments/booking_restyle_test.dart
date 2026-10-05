import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/features/appointments/data/appointment_repository.dart';
import 'package:patient_app/src/features/appointments/domain/appointment_models.dart';
import 'package:patient_app/src/features/appointments/presentation/book_appointment_flow.dart';
import 'package:patient_app/src/features/appointments/presentation/therapy_card.dart';
import 'package:patient_app/src/features/treatments/application/treatments_provider.dart';
import 'package:patient_app/src/features/treatments/domain/treatment_models.dart' show Treatment;
import 'package:patient_app/src/l10n/app_localizations.dart';
import 'package:patient_app/src/l10n/locale_controller.dart';
import 'package:patient_app/src/shared/widgets/clinic_widgets.dart';
import 'package:patient_app/src/theme/app_theme.dart';

class _FakeRepository implements AppointmentRepository {
  final List<String> availabilityRequests = [];

  @override
  Future<TreatmentAvailability> availability(
    String treatmentId,
    DateTime date,
  ) async {
    availabilityRequests.add(treatmentId);
    return const TreatmentAvailability(
      available: true,
      slots: [TreatmentSlot(time: '09:00-10:00', scheduleId: 's-1')],
    );
  }

  @override
  Future<Appointment> create({
    required String patientId,
    required TreatmentBooking treatment,
    required DateTime date,
    required TreatmentSlot slot,
  }) => throw UnimplementedError();

  @override
  Future<List<Appointment>> mine() async => const [];

  @override
  Future<void> cancel(String appointmentId) async {}

  @override
  Future<Appointment> reschedule({
    required String appointmentId,
    required DateTime date,
    required String timeSlot,
    String? scheduleId,
  }) => throw UnimplementedError();
}

const _abhyanga = Treatment(
  id: 'abhyanga-1',
  nameSinhala: 'අභ්‍යංග',
  nameEnglish: 'Abhyanga',
  description: 'Warm herbal oil massage to settle vata.',
  scheduleDays: ['Monday'],
  category: 'Abhyanga',
  durationMinutes: 60,
  unitPrice: 4500,
);

const _steam = Treatment(
  id: 'steam-1',
  nameSinhala: 'වාෂ්ප',
  nameEnglish: 'Herbal Steam',
  description: 'Herbal steam bath.',
  scheduleDays: ['Tuesday'],
  category: 'HerbalSteam',
  durationMinutes: 30,
  unitPrice: 2500,
);

const _retired = Treatment(
  id: 'retired-1',
  nameSinhala: 'පැරණි',
  nameEnglish: 'Retired Therapy',
  description: 'No longer offered.',
  scheduleDays: [],
  isActive: false,
);

final _day = DateTime(2099, 5, 4);

Future<_FakeRepository> _pump(
  WidgetTester tester,
  Widget home, {
  Future<List<Treatment>> Function()? therapies,
  ThemeData? theme,
  Locale locale = const Locale('en'),
}) async {
  final repository = _FakeRepository();
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        appointmentRepositoryProvider.overrideWithValue(repository),
        treatmentsProvider.overrideWith(
          (ref) => (therapies ?? () async => [_abhyanga, _steam, _retired])(),
        ),
      ],
      child: MaterialApp(
        theme: theme ?? AppTheme.light,
        locale: locale,
        supportedLocales: supportedLocales,
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        home: home,
      ),
    ),
  );
  await tester.pumpAndSettle();
  return repository;
}

void _setSize(WidgetTester tester, Size size) {
  tester.view.physicalSize = size;
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
}

BookAppointmentFlow _picker() => BookAppointmentFlow(candidateDates: [_day]);

void main() {
  group('treatment catalogue fields', () {
    test('reads category, duration and price from GET /api/treatments', () {
      final treatment = Treatment.fromJson({
        'id': 't-1',
        'name': 'Herbal Steam',
        'nameSinhala': 'වාෂ්ප',
        'description': 'Steam bath',
        'category': 'HerbalSteam',
        'durationMinutes': 45,
        'unitPrice': 3200.5,
        'isActive': true,
        'availableDays': ['Monday'],
      });

      expect(treatment.category, 'HerbalSteam');
      expect(treatment.durationMinutes, 45);
      expect(treatment.unitPrice, 3200.5);
      expect(treatment.isActive, isTrue);
    });

    test('tolerates a catalogue entry without the new fields', () {
      final treatment = Treatment.fromJson({'id': 't-2', 'name': 'Nasya'});

      expect(treatment.category, isNull);
      expect(treatment.durationMinutes, isNull);
      expect(treatment.unitPrice, isNull);
      expect(treatment.isActive, isTrue);
    });

    test('splits the category into words', () {
      expect(therapyCategoryLabel('HerbalSteam'), 'Herbal Steam');
      expect(therapyCategoryLabel('Panchakarma'), 'Panchakarma');
    });
  });

  group('therapy step', () {
    testWidgets('shows a centered title, four steps and a rounded panel', (tester) async {
      await _pump(tester, _picker());

      expect(tester.widget<AppBar>(find.byType(AppBar)).centerTitle, isTrue);
      expect(find.text('Book appointment'), findsOneWidget);
      for (final label in ['THERAPY', 'DATE', 'TIME', 'CONFIRM']) {
        expect(find.text(label), findsOneWidget, reason: label);
      }
      expect(find.byType(RoundedPanel), findsOneWidget);
      expect(find.text('Choose a therapy'), findsOneWidget);
    });

    testWidgets('therapy cards show name, description, duration, price and category', (tester) async {
      await _pump(tester, _picker());

      final card = find.byKey(TherapyCardKeys.card('abhyanga-1'));
      expect(card, findsOneWidget);
      Finder inCard(Finder finder) =>
          find.descendant(of: card, matching: finder);

      expect(inCard(find.text('Abhyanga')), findsNWidgets(2)); // name and category
      expect(
        inCard(find.text('Warm herbal oil massage to settle vata.')),
        findsOneWidget,
      );
      expect(inCard(find.byIcon(Icons.schedule)), findsOneWidget);
      expect(inCard(find.text('60 minutes')), findsOneWidget);
      expect(inCard(find.byKey(const ValueKey('therapy-category'))), findsOneWidget);

      final price = tester.widget<Text>(
        inCard(find.byKey(const ValueKey('therapy-price'))),
      );
      expect(price.data, 'LKR 4500.00');
      expect(price.style?.fontFamily, AyurvedaFonts.serif);
      expect(
        price.style?.color,
        AyurvedaThemeExtension.of(tester.element(card)).teal,
      );

      final steam = find.byKey(TherapyCardKeys.card('steam-1'));
      expect(
        find.descendant(of: steam, matching: find.text('Herbal Steam')),
        findsNWidgets(2),
        reason: 'name and the split category label',
      );
    });

    testWidgets('inactive therapies are not offered', (tester) async {
      await _pump(tester, _picker());

      expect(find.text('Retired Therapy'), findsNothing);
      expect(find.byKey(TherapyCardKeys.card('retired-1')), findsNothing);
    });

    testWidgets('continue stays disabled until a therapy is chosen', (tester) async {
      await _pump(tester, _picker());

      FilledButton next() => tester.widget<FilledButton>(
        find.byKey(BookAppointmentKeys.therapyNext),
      );
      expect(next().onPressed, isNull);

      await tester.tap(find.byKey(TherapyCardKeys.card('steam-1')));
      await tester.pumpAndSettle();
      expect(next().onPressed, isNotNull);
    });

    testWidgets('choosing a therapy loads its dates, and Back returns to the list', (tester) async {
      final repository = await _pump(tester, _picker());

      await tester.tap(find.byKey(TherapyCardKeys.card('steam-1')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(BookAppointmentKeys.therapyNext));
      await tester.pumpAndSettle();

      expect(find.text('Choose a date'), findsOneWidget);
      expect(find.byKey(BookAppointmentKeys.date(_day)), findsOneWidget);
      expect(repository.availabilityRequests, ['steam-1']);

      await tester.tap(find.byKey(BookAppointmentKeys.backToTherapy));
      await tester.pumpAndSettle();
      expect(find.text('Choose a therapy'), findsOneWidget);

      // Same therapy again: the dates are not requested a second time.
      await tester.tap(find.byKey(BookAppointmentKeys.therapyNext));
      await tester.pumpAndSettle();
      expect(repository.availabilityRequests, ['steam-1']);
    });

    testWidgets('shows an error with retry when the catalogue cannot load', (tester) async {
      var attempts = 0;
      await _pump(
        tester,
        _picker(),
        therapies: () async {
          attempts++;
          if (attempts == 1) throw StateError('down');
          return [_abhyanga];
        },
      );

      expect(find.text('Could not load therapies.'), findsOneWidget);
      await tester.tap(find.text('Try again'));
      await tester.pumpAndSettle();
      expect(find.byKey(TherapyCardKeys.card('abhyanga-1')), findsOneWidget);
    });

    testWidgets('shows an empty state when nothing is bookable', (tester) async {
      await _pump(tester, _picker(), therapies: () async => [_retired]);

      expect(
        find.text('No therapies are available to book right now.'),
        findsOneWidget,
      );
    });

    testWidgets('copy is translated to Sinhala', (tester) async {
      await _pump(tester, _picker(), locale: const Locale('si'));

      expect(find.text('ප්‍රතිකාරයක් තෝරන්න'), findsOneWidget);
      expect(find.text('අභ්‍යංග'), findsOneWidget);
    });
  });

  group('layout', () {
    testWidgets('therapy cards stack on a phone', (tester) async {
      _setSize(tester, const Size(390, 844));
      await _pump(tester, _picker());

      final first = tester.getTopLeft(find.byKey(TherapyCardKeys.card('abhyanga-1')));
      final second = tester.getTopLeft(find.byKey(TherapyCardKeys.card('steam-1')));
      expect(second.dy, greaterThan(first.dy));
      expect(second.dx, first.dx);
    });

    testWidgets('therapy cards sit in two columns on a wide screen', (tester) async {
      _setSize(tester, const Size(1200, 900));
      await _pump(tester, _picker());

      final first = tester.getTopLeft(find.byKey(TherapyCardKeys.card('abhyanga-1')));
      final second = tester.getTopLeft(find.byKey(TherapyCardKeys.card('steam-1')));
      expect(second.dx, greaterThan(first.dx));
      expect(second.dy, first.dy);
    });
  });

  group('preset therapy and reschedule mode', () {
    testWidgets('a preset therapy starts at the date step with no way back to the list', (tester) async {
      final repository = await _pump(
        tester,
        BookAppointmentFlow(
          treatment: const TreatmentBooking(
            id: 'abhyanga-1',
            name: 'Abhyanga',
            durationMinutes: 60,
          ),
          candidateDates: [_day],
        ),
      );

      expect(find.text('Choose a date'), findsOneWidget);
      expect(find.text('60 minutes'), findsOneWidget);
      expect(find.byIcon(Icons.schedule), findsOneWidget);
      expect(find.byKey(BookAppointmentKeys.backToTherapy), findsNothing);
      expect(find.byKey(TherapyCardKeys.card('abhyanga-1')), findsNothing);
      expect(repository.availabilityRequests, ['abhyanga-1']);
    });

    testWidgets('reschedule mode keeps the four steps and locks the therapy', (tester) async {
      await _pump(
        tester,
        BookAppointmentFlow(
          treatment: const TreatmentBooking(id: 'abhyanga-1', name: 'Abhyanga'),
          candidateDates: [_day],
          appointmentIdToReschedule: 'appt-1',
        ),
      );

      expect(tester.widget<AppBar>(find.byType(AppBar)).centerTitle, isTrue);
      expect(find.text('Reschedule appointment'), findsOneWidget);
      for (final label in ['THERAPY', 'DATE', 'TIME', 'CONFIRM']) {
        expect(find.text(label), findsOneWidget, reason: label);
      }
      // The therapy step is already complete, shown with a check.
      expect(find.byIcon(Icons.check), findsOneWidget);
      expect(find.byKey(BookAppointmentKeys.backToTherapy), findsNothing);
      expect(find.byType(RoundedPanel), findsOneWidget);
      expect(find.text('Abhyanga'), findsOneWidget);
    });
  });

  group('themes', () {
    for (final entry in {'light': AppTheme.light, 'dark': AppTheme.dark}.entries) {
      testWidgets('renders every step in the ${entry.key} theme', (tester) async {
        _setSize(tester, const Size(390, 844));
        await _pump(tester, _picker(), theme: entry.value);

        await tester.tap(find.byKey(TherapyCardKeys.card('abhyanga-1')));
        await tester.pumpAndSettle();
        await tester.tap(find.byKey(BookAppointmentKeys.therapyNext));
        await tester.pumpAndSettle();
        await tester.tap(find.byKey(BookAppointmentKeys.date(_day)));
        await tester.pumpAndSettle();
        await tester.tap(find.byKey(BookAppointmentKeys.next));
        await tester.pumpAndSettle();
        await tester.tap(find.text('09:00-10:00'));
        await tester.pumpAndSettle();
        await tester.tap(find.text('Review'));
        await tester.pumpAndSettle();

        expect(find.text('Confirm request'), findsOneWidget);
        expect(tester.takeException(), isNull);
      });
    }
  });
}


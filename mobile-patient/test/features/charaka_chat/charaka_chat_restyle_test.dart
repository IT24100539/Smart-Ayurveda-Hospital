import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/core/network/api_exception.dart';
import 'package:patient_app/src/features/auth/application/auth_controller.dart';
import 'package:patient_app/src/features/auth/domain/auth_models.dart';
import 'package:patient_app/src/features/charaka_chat/data/charaka_chat_repository.dart';
import 'package:patient_app/src/features/charaka_chat/presentation/charaka_chat_screen.dart';
import 'package:patient_app/src/l10n/app_localizations.dart';
import 'package:patient_app/src/shared/widgets/clinic_widgets.dart';
import 'package:patient_app/src/theme/app_theme.dart';

class _SignedIn extends AuthController {
  @override
  AuthState build() => const AuthState(
    status: AuthStatus.authenticated,
    user: AuthUser(
      id: 'patient-123',
      fullName: 'Meera Nair',
      email: 'meera.nair@example.local',
      phoneNumber: '9876500001',
      role: UserRole.patient,
    ),
  );
}

class _FakeChat implements CharakaChatRepository {
  _FakeChat({this.answer, this.error});

  final CharakaAnswer? answer;
  final Object? error;

  @override
  Future<CharakaAnswer> askTreatment(String question) async {
    if (error != null) throw error!;
    return answer ??
        const CharakaAnswer(
          answer: 'Abhyanga is a warm oil massage.',
          refused: false,
          workflowId: 'wf-1',
        );
  }

  @override
  Future<CharakaAnswer> askPatient(String question) => askTreatment(question);
}

Widget _app(_FakeChat repository, {ThemeData? theme}) {
  return ProviderScope(
    overrides: [
      charakaChatRepositoryProvider.overrideWithValue(repository),
      authControllerProvider.overrideWith(_SignedIn.new),
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
      home: const CharakaChatScreen(),
    ),
  );
}

void _setSize(WidgetTester tester, Size size) {
  tester.view.physicalSize = size;
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
}

final _time = RegExp(r'^\d{1,2}:\d{2} [AP]M$');

void main() {
  testWidgets('header has an avatar, the title and a neutral status pill', (tester) async {
    await tester.pumpWidget(_app(_FakeChat()));
    await tester.pumpAndSettle();

    final header = find.byType(AppBar);
    expect(
      find.descendant(of: header, matching: find.byIcon(Icons.spa)),
      findsOneWidget,
    );
    expect(
      find.descendant(of: header, matching: find.text('Charaka AI Assistant')),
      findsOneWidget,
    );

    final pill = tester.widget<PillChip>(find.byKey(CharakaChatKeys.statusPill));
    final brand = AyurvedaThemeExtension.of(
      tester.element(find.byKey(CharakaChatKeys.statusPill)),
    );
    expect(pill.background, brand.neutralBackground);
    expect(pill.foreground, brand.neutralForeground);
    expect(pill.background, isNot(brand.approvedBackground));
    expect(pill.label, 'Hospital assistant');
  });

  testWidgets('messages, suggestions and the input live in one rounded panel', (tester) async {
    await tester.pumpWidget(_app(_FakeChat()));
    await tester.pumpAndSettle();

    final panel = find.byKey(CharakaChatKeys.panel);
    expect(panel, findsOneWidget);
    expect(tester.widget<RoundedPanel>(panel), isA<RoundedPanel>());
    for (final inside in [
      find.byType(ListView),
      find.byType(SuggestionChips),
      find.byType(PillSendField),
    ]) {
      expect(find.descendant(of: panel, matching: inside), findsWidgets);
    }
    // The disclaimer stays outside, above the panel.
    expect(
      find.descendant(
        of: panel,
        matching: find.textContaining('Charaka provides general information'),
      ),
      findsNothing,
    );
    expect(
      find.textContaining('Charaka provides general information'),
      findsOneWidget,
    );
  });

  testWidgets('assistant bubble shows an avatar and a timestamp', (tester) async {
    await tester.pumpWidget(_app(_FakeChat()));
    await tester.pumpAndSettle();

    expect(find.byIcon(Icons.spa_outlined), findsOneWidget);
    final stamps = find.byWidgetPredicate(
      (widget) => widget is Text && _time.hasMatch(widget.data ?? ''),
    );
    expect(stamps, findsOneWidget);
    expect(
      find.textContaining('Ayubowan! I am Charaka, your hospital assistant.'),
      findsOneWidget,
    );
  });

  testWidgets('suggestion chips are a horizontal row with no scrollbar', (tester) async {
    await tester.pumpWidget(_app(_FakeChat()));
    await tester.pumpAndSettle();

    final chips = find.byType(SuggestionChips);
    expect(
      find.descendant(of: chips, matching: find.byType(Scrollbar)),
      findsNothing,
    );
    // The row scrolls sideways, so chips past the edge are not built yet.
    expect(
      find.descendant(of: chips, matching: find.byType(ActionChip)),
      findsAtLeastNWidgets(2),
    );
    final list = tester.widget<ListView>(
      find.descendant(of: chips, matching: find.byType(ListView)),
    );
    expect(list.scrollDirection, Axis.horizontal);

    // Chips share one row.
    final first = tester.getTopLeft(find.byType(ActionChip).first).dy;
    final second = tester.getTopLeft(find.byType(ActionChip).at(1)).dy;
    expect(second, first);
  });

  testWidgets('input is a pill and Send is teal', (tester) async {
    await tester.pumpWidget(_app(_FakeChat()));
    await tester.pumpAndSettle();

    final brand = AyurvedaThemeExtension.of(
      tester.element(find.byKey(CharakaChatKeys.sendButton)),
    );
    final send = tester.widget<FilledButton>(find.byKey(CharakaChatKeys.sendButton));
    expect(send.style?.backgroundColor?.resolve({}), brand.teal);
    expect(send.style?.foregroundColor?.resolve({}), brand.onTeal);
    expect(send.style?.shape?.resolve({}), isA<StadiumBorder>());

    final field = tester.widget<TextField>(find.byKey(CharakaChatKeys.input));
    final border = field.decoration!.enabledBorder! as OutlineInputBorder;
    expect(border.borderRadius.topLeft.x, 28);
  });

  testWidgets('a user message appears in a teal bubble', (tester) async {
    await tester.pumpWidget(_app(_FakeChat()));
    await tester.pumpAndSettle();

    await tester.enterText(find.byKey(CharakaChatKeys.input), 'Tell me about Abhyanga');
    await tester.tap(find.byKey(CharakaChatKeys.sendButton));
    await tester.pumpAndSettle();

    final brand = AyurvedaThemeExtension.of(
      tester.element(find.byKey(CharakaChatKeys.panel)),
    );
    final bubble = tester.widget<Container>(
      find
          .ancestor(
            of: find.text('Tell me about Abhyanga'),
            matching: find.byType(Container),
          )
          .first,
    );
    expect((bubble.decoration! as BoxDecoration).color, brand.teal);
    expect(find.text('Abhyanga is a warm oil massage.'), findsOneWidget);
  });

  testWidgets('refusal text and badge come from the backend unchanged', (tester) async {
    const refusal =
        'I cannot prescribe treatments or diagnose illnesses. Please consult a doctor.';
    await tester.pumpWidget(
      _app(
        _FakeChat(
          answer: const CharakaAnswer(
            answer: refusal,
            refused: true,
            workflowId: 'wf-refused',
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.enterText(find.byKey(CharakaChatKeys.input), 'What medicine for fever?');
    await tester.tap(find.byKey(CharakaChatKeys.sendButton));
    await tester.pumpAndSettle();

    expect(find.text(refusal), findsOneWidget);
    expect(find.byKey(CharakaChatKeys.refusalBadge), findsOneWidget);
  });

  testWidgets('a network failure still shows the offline banner above the panel', (tester) async {
    await tester.pumpWidget(
      _app(
        _FakeChat(
          error: const ApiException(
            statusCode: null,
            detail: 'Connection refused',
            isNetworkError: true,
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.enterText(find.byKey(CharakaChatKeys.input), 'Hello');
    await tester.tap(find.byKey(CharakaChatKeys.sendButton));
    await tester.pumpAndSettle();

    final banner = find.byKey(CharakaChatKeys.offlineBanner);
    expect(banner, findsOneWidget);
    expect(
      find.descendant(of: find.byKey(CharakaChatKeys.panel), matching: banner),
      findsNothing,
    );
    expect(find.byKey(CharakaChatKeys.retryButton), findsOneWidget);
  });

  for (final entry in {'light': AppTheme.light, 'dark': AppTheme.dark}.entries) {
    testWidgets('renders on a phone and a wide screen in the ${entry.key} theme', (tester) async {
      for (final size in const [Size(390, 844), Size(1200, 900)]) {
        _setSize(tester, size);
        await tester.pumpWidget(_app(_FakeChat(), theme: entry.value));
        await tester.pumpAndSettle();

        expect(find.byKey(CharakaChatKeys.panel), findsOneWidget);
        expect(find.byKey(CharakaChatKeys.statusPill), findsOneWidget);
        expect(tester.takeException(), isNull);
      }
    });
  }
}

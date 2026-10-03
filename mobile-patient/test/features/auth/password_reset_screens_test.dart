import 'dart:async';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:patient_app/src/core/network/api_exception.dart';
import 'package:patient_app/src/core/storage/token_storage.dart';
import 'package:patient_app/src/features/auth/data/auth_repository.dart';
import 'package:patient_app/src/features/auth/presentation/forgot_password_screen.dart';
import 'package:patient_app/src/features/auth/presentation/login_screen.dart';
import 'package:patient_app/src/features/auth/presentation/reset_password_screen.dart';
import 'package:patient_app/src/l10n/app_localizations.dart';
import 'package:patient_app/src/l10n/locale_controller.dart';
import 'package:patient_app/src/router/app_routes.dart';
import 'package:patient_app/src/theme/app_theme.dart';

class FakeAuthRepository extends AuthRepository {
  FakeAuthRepository({
    this.onRequestReset,
    this.onCompleteReset,
  }) : super(Dio());

  Future<void> Function(String email)? onRequestReset;
  Future<void> Function({
    required String email,
    required String token,
    required String newPassword,
    required String confirmPassword,
  })? onCompleteReset;

  String? lastEmail;
  String? lastToken;
  String? lastNewPassword;
  String? lastConfirmPassword;

  @override
  Future<void> requestReset({required String email}) async {
    lastEmail = email;
    final handler = onRequestReset;
    if (handler != null) await handler(email);
  }

  @override
  Future<void> completeReset({
    required String email,
    required String token,
    required String newPassword,
    required String confirmPassword,
  }) async {
    lastEmail = email;
    lastToken = token;
    lastNewPassword = newPassword;
    lastConfirmPassword = confirmPassword;
    final handler = onCompleteReset;
    if (handler != null) {
      await handler(
        email: email,
        token: token,
        newPassword: newPassword,
        confirmPassword: confirmPassword,
      );
    }
  }
}

Future<AppLocalizations> _en() =>
    AppLocalizations.delegate.load(const Locale('en'));

Future<void> _pump({
  required WidgetTester tester,
  required Widget home,
  required FakeAuthRepository repo,
  ThemeData? theme,
  ThemeData? darkTheme,
  ThemeMode themeMode = ThemeMode.light,
}) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        tokenStorageProvider.overrideWithValue(InMemoryTokenStorage()),
        authRepositoryProvider.overrideWithValue(repo),
        localeControllerProvider.overrideWith(_EnLocaleController.new),
      ],
      child: MaterialApp(
        theme: theme ?? AppTheme.light,
        darkTheme: darkTheme ?? AppTheme.dark,
        themeMode: themeMode,
        locale: const Locale('en'),
        supportedLocales: AppLocalizations.supportedLocales,
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        home: home,
      ),
    ),
  );
  await tester.pump();
}

class _EnLocaleController extends LocaleController {
  @override
  Locale build() => const Locale('en');
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  group('ForgotPasswordScreen', () {
    testWidgets('validates email before calling the API', (tester) async {
      final l10n = await _en();
      final repo = FakeAuthRepository(
        onRequestReset: (_) => fail('requestReset should not run'),
      );
      await _pump(
        tester: tester,
        home: const ForgotPasswordScreen(),
        repo: repo,
      );

      await tester.tap(find.byKey(ForgotPasswordScreenKeys.submit));
      await tester.pump();
      expect(find.text(l10n.emailRequired), findsOneWidget);

      await tester.enterText(
        find.byKey(ForgotPasswordScreenKeys.email),
        'not-an-email',
      );
      await tester.tap(find.byKey(ForgotPasswordScreenKeys.submit));
      await tester.pump();
      expect(find.text(l10n.emailInvalid), findsOneWidget);
      expect(repo.lastEmail, isNull);
    });

    testWidgets('shows loading then success for request-reset', (tester) async {
      final l10n = await _en();
      final gate = Completer<void>();
      final repo = FakeAuthRepository(
        onRequestReset: (_) => gate.future,
      );
      await _pump(
        tester: tester,
        home: const ForgotPasswordScreen(),
        repo: repo,
      );

      await tester.enterText(
        find.byKey(ForgotPasswordScreenKeys.email),
        'patient@hospital.lk',
      );
      await tester.tap(find.byKey(ForgotPasswordScreenKeys.submit));
      await tester.pump();

      expect(find.byKey(ForgotPasswordScreenKeys.loading), findsOneWidget);
      gate.complete();
      await tester.pumpAndSettle();

      expect(repo.lastEmail, 'patient@hospital.lk');
      expect(find.byKey(ForgotPasswordScreenKeys.success), findsOneWidget);
      expect(find.text(l10n.forgotPasswordSuccess), findsOneWidget);
    });

    testWidgets('shows an error banner when request-reset fails', (tester) async {
      final l10n = await _en();
      final repo = FakeAuthRepository(
        onRequestReset: (_) => throw const ApiException(statusCode: 0, isNetworkError: true),
      );
      await _pump(
        tester: tester,
        home: const ForgotPasswordScreen(),
        repo: repo,
      );

      await tester.enterText(
        find.byKey(ForgotPasswordScreenKeys.email),
        'patient@hospital.lk',
      );
      await tester.tap(find.byKey(ForgotPasswordScreenKeys.submit));
      await tester.pumpAndSettle();

      expect(find.byKey(ForgotPasswordScreenKeys.error), findsOneWidget);
      expect(find.text(l10n.networkErrorMessage), findsOneWidget);
    });

    testWidgets('renders success and error with the dark color scheme', (
      tester,
    ) async {
      final l10n = await _en();
      final repo = FakeAuthRepository();
      await _pump(
        tester: tester,
        home: const ForgotPasswordScreen(),
        repo: repo,
        themeMode: ThemeMode.dark,
      );

      expect(find.text(l10n.forgotPasswordHeading), findsOneWidget);
      expect(Theme.of(tester.element(find.byType(ForgotPasswordScreen))).brightness, Brightness.dark);

      await tester.enterText(
        find.byKey(ForgotPasswordScreenKeys.email),
        'patient@hospital.lk',
      );
      await tester.tap(find.byKey(ForgotPasswordScreenKeys.submit));
      await tester.pumpAndSettle();

      final successCard = tester.widget<Card>(
        find.descendant(
          of: find.byKey(ForgotPasswordScreenKeys.success),
          matching: find.byType(Card),
        ),
      );
      expect(
        successCard.color,
        AppTheme.dark.colorScheme.primaryContainer,
      );
    });

    testWidgets('exposes Semantics labels on submit and success', (tester) async {
      final l10n = await _en();
      final handle = tester.ensureSemantics();
      final repo = FakeAuthRepository();
      await _pump(
        tester: tester,
        home: const ForgotPasswordScreen(),
        repo: repo,
      );

      expect(
        find.bySemanticsLabel(l10n.forgotPasswordSubmit),
        findsWidgets,
      );

      await tester.enterText(
        find.byKey(ForgotPasswordScreenKeys.email),
        'patient@hospital.lk',
      );
      await tester.tap(find.byKey(ForgotPasswordScreenKeys.submit));
      await tester.pumpAndSettle();

      expect(
        find.bySemanticsLabel(l10n.forgotPasswordSuccess),
        findsWidgets,
      );
      handle.dispose();
    });
  });

  group('ResetPasswordScreen', () {
    testWidgets('validates token, password policy, and confirmation', (
      tester,
    ) async {
      final l10n = await _en();
      final repo = FakeAuthRepository(
        onCompleteReset: ({
          required email,
          required token,
          required newPassword,
          required confirmPassword,
        }) =>
            fail('completeReset should not run'),
      );
      await _pump(
        tester: tester,
        home: const ResetPasswordScreen(),
        repo: repo,
      );

      await tester.tap(find.byKey(ResetPasswordScreenKeys.submit));
      await tester.pump();
      expect(find.text(l10n.emailRequired), findsOneWidget);
      expect(find.text(l10n.resetTokenRequired), findsOneWidget);
      expect(find.text(l10n.passwordRequired), findsOneWidget);

      await tester.enterText(
        find.byKey(ResetPasswordScreenKeys.email),
        'patient@hospital.lk',
      );
      await tester.enterText(
        find.byKey(ResetPasswordScreenKeys.token),
        'reset-token',
      );
      await tester.enterText(
        find.byKey(ResetPasswordScreenKeys.newPassword),
        'weakpass',
      );
      await tester.tap(find.byKey(ResetPasswordScreenKeys.submit));
      await tester.pump();
      expect(find.text(l10n.passwordNeedsUppercase), findsOneWidget);

      await tester.enterText(
        find.byKey(ResetPasswordScreenKeys.newPassword),
        'Abcdefg1!',
      );
      await tester.enterText(
        find.byKey(ResetPasswordScreenKeys.confirmPassword),
        'Mismatch1!',
      );
      await tester.tap(find.byKey(ResetPasswordScreenKeys.submit));
      await tester.pump();
      expect(find.text(l10n.passwordsDoNotMatch), findsOneWidget);
      expect(repo.lastToken, isNull);
    });

    testWidgets('shows loading then success for complete-reset', (tester) async {
      final l10n = await _en();
      final gate = Completer<void>();
      final repo = FakeAuthRepository(
        onCompleteReset: ({
          required email,
          required token,
          required newPassword,
          required confirmPassword,
        }) =>
            gate.future,
      );
      await _pump(
        tester: tester,
        home: const ResetPasswordScreen(
          email: 'patient@hospital.lk',
          token: 'abc123',
        ),
        repo: repo,
      );

      await tester.enterText(
        find.byKey(ResetPasswordScreenKeys.newPassword),
        'Abcdefg1!',
      );
      await tester.enterText(
        find.byKey(ResetPasswordScreenKeys.confirmPassword),
        'Abcdefg1!',
      );
      await tester.tap(find.byKey(ResetPasswordScreenKeys.submit));
      await tester.pump();

      expect(find.byKey(ResetPasswordScreenKeys.loading), findsOneWidget);
      gate.complete();
      await tester.pumpAndSettle();

      expect(repo.lastEmail, 'patient@hospital.lk');
      expect(repo.lastToken, 'abc123');
      expect(repo.lastNewPassword, 'Abcdefg1!');
      expect(find.byKey(ResetPasswordScreenKeys.success), findsOneWidget);
      expect(find.text(l10n.resetPasswordSuccess), findsOneWidget);
      expect(find.byKey(ResetPasswordScreenKeys.signInNow), findsOneWidget);
    });

    testWidgets('shows an error banner when complete-reset fails', (tester) async {
      final l10n = await _en();
      final repo = FakeAuthRepository(
        onCompleteReset: ({
          required email,
          required token,
          required newPassword,
          required confirmPassword,
        }) =>
            throw const ApiException(statusCode: 400),
      );
      await _pump(
        tester: tester,
        home: const ResetPasswordScreen(
          email: 'patient@hospital.lk',
          token: 'expired',
        ),
        repo: repo,
      );

      await tester.enterText(
        find.byKey(ResetPasswordScreenKeys.newPassword),
        'Abcdefg1!',
      );
      await tester.enterText(
        find.byKey(ResetPasswordScreenKeys.confirmPassword),
        'Abcdefg1!',
      );
      await tester.tap(find.byKey(ResetPasswordScreenKeys.submit));
      await tester.pumpAndSettle();

      expect(find.byKey(ResetPasswordScreenKeys.error), findsOneWidget);
      expect(find.text(l10n.resetPasswordError), findsOneWidget);
    });

    testWidgets('uses dark-theme containers for the error banner', (tester) async {
      final repo = FakeAuthRepository(
        onCompleteReset: ({
          required email,
          required token,
          required newPassword,
          required confirmPassword,
        }) =>
            throw const ApiException(statusCode: 400),
      );
      await _pump(
        tester: tester,
        home: const ResetPasswordScreen(
          email: 'patient@hospital.lk',
          token: 'expired',
        ),
        repo: repo,
        themeMode: ThemeMode.dark,
      );

      await tester.enterText(
        find.byKey(ResetPasswordScreenKeys.newPassword),
        'Abcdefg1!',
      );
      await tester.enterText(
        find.byKey(ResetPasswordScreenKeys.confirmPassword),
        'Abcdefg1!',
      );
      await tester.tap(find.byKey(ResetPasswordScreenKeys.submit));
      await tester.pumpAndSettle();

      final errorCard = tester.widget<Card>(
        find.byKey(ResetPasswordScreenKeys.error),
      );
      expect(errorCard.color, AppTheme.dark.colorScheme.errorContainer);
    });
  });

  group('login forgot-password link', () {
    testWidgets('opens the forgot-password screen', (tester) async {
      final l10n = await _en();
      final router = GoRouter(
        initialLocation: AppRoutes.login,
        routes: [
          GoRoute(
            path: AppRoutes.login,
            builder: (context, state) => const LoginScreen(),
          ),
          GoRoute(
            path: AppRoutes.forgotPassword,
            builder: (context, state) => const ForgotPasswordScreen(),
          ),
        ],
      );

      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            tokenStorageProvider.overrideWithValue(InMemoryTokenStorage()),
            authRepositoryProvider.overrideWithValue(FakeAuthRepository()),
            localeControllerProvider.overrideWith(_EnLocaleController.new),
          ],
          child: MaterialApp.router(
            theme: AppTheme.light,
            locale: const Locale('en'),
            supportedLocales: AppLocalizations.supportedLocales,
            localizationsDelegates: AppLocalizations.localizationsDelegates,
            routerConfig: router,
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.byKey(LoginScreenKeys.forgotPassword), findsOneWidget);
      expect(find.text(l10n.forgotPasswordLink), findsOneWidget);

      await tester.tap(find.byKey(LoginScreenKeys.forgotPassword));
      await tester.pumpAndSettle();

      expect(find.byType(ForgotPasswordScreen), findsOneWidget);
      expect(find.text(l10n.forgotPasswordHeading), findsOneWidget);
    });
  });
}

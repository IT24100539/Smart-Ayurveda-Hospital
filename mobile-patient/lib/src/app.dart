import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/network/api_client.dart';
import 'features/auth/application/auth_controller.dart';
import 'features/notifications/application/push_registration.dart';
import 'l10n/app_localizations.dart';
import 'l10n/locale_controller.dart';
import 'router/app_router.dart';
import 'session_cache.dart';
import 'theme/app_theme.dart';
import 'theme/theme_mode_controller.dart';

/// Connects the dio 401 interceptor to the auth controller.
///
/// Kept as an override rather than a direct dependency so `api_client.dart`
/// stays free of any reference to the auth feature.
final unauthorizedOverride = onUnauthorizedProvider.overrideWith(
  (ref) =>
      () => ref.read(authControllerProvider.notifier).handleUnauthorized(),
);

class PatientApp extends ConsumerWidget {
  const PatientApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final router = ref.watch(routerProvider);
    final locale = ref.watch(localeControllerProvider);
    final themeMode = ref.watch(themeControllerProvider);

    ref.listen(authControllerProvider, (previous, next) {
      final sessionEnded =
          previous?.isAuthenticated == true && !next.isAuthenticated;
      if (!sessionEnded) return;
      // Wait until the shell has left the tree so cached providers are not
      // refetched with a token that was just cleared.
      final container = ProviderScope.containerOf(context);
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (!context.mounted) return;
        container.read(sessionCacheProvider).clear();
      });
    });

    return PushRegistrationHost(
      child: MaterialApp.router(
        title: 'Smart Ayurveda',
        debugShowCheckedModeBanner: false,
        theme: AppTheme.light,
        darkTheme: AppTheme.dark,
        themeMode: themeMode,
        routerConfig: router,
        // Sinhala unless the patient switches, regardless of device locale.
        locale: locale,
        supportedLocales: supportedLocales,
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        onGenerateTitle: (context) => AppLocalizations.of(context).appTitle,
      ),
    );
  }
}

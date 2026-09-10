import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/network/api_client.dart';
import 'features/auth/application/auth_controller.dart';
import 'l10n/app_localizations.dart';
import 'l10n/locale_controller.dart';
import 'router/app_router.dart';
import 'theme/app_theme.dart';

/// Connects the dio 401 interceptor to the auth controller.
///
/// Kept as an override rather than a direct dependency so `api_client.dart`
/// stays free of any reference to the auth feature.
final unauthorizedOverride = onUnauthorizedProvider.overrideWith(
  (ref) => () => ref.read(authControllerProvider.notifier).handleUnauthorized(),
);

class PatientApp extends ConsumerWidget {
  const PatientApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final router = ref.watch(routerProvider);
    final locale = ref.watch(localeControllerProvider);

    return MaterialApp.router(
      title: 'Smart Ayurveda',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light,
      routerConfig: router,
      // Sinhala unless the patient switches, regardless of device locale.
      locale: locale,
      supportedLocales: supportedLocales,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      onGenerateTitle: (context) => AppLocalizations.of(context).appTitle,
    );
  }
}

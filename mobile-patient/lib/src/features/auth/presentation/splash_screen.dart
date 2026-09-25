import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../l10n/app_localizations.dart';
import '../../../l10n/language_switcher.dart';
import '../../../router/app_routes.dart';
import '../../../theme/app_theme.dart';
import '../application/auth_controller.dart';

/// Entry screen: picks up any stored session and lets the patient choose a
/// language before signing in.
class SplashScreen extends ConsumerStatefulWidget {
  const SplashScreen({super.key});

  @override
  ConsumerState<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends ConsumerState<SplashScreen> {
  @override
  void initState() {
    super.initState();
    // Deferred: reading secure storage touches a platform channel, which must
    // not run during the first build.
    Future.microtask(
      () => ref.read(authControllerProvider.notifier).restoreSession(),
    );
  }

  void _continue() {
    final isAuthenticated = ref.read(authControllerProvider).isAuthenticated;
    context.go(isAuthenticated ? AppRoutes.home : AppRoutes.login);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final authState = ref.watch(authControllerProvider);

    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 28),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Spacer(flex: 2),
              Center(
                child: Container(
                  padding: const EdgeInsets.all(22),
                  decoration: const BoxDecoration(
                    color: AyurvedaColors.sageMuted,
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(
                    Icons.spa,
                    size: 44,
                    color: AyurvedaColors.forest,
                  ),
                ),
              ),
              const SizedBox(height: 24),
              Text(
                l10n.appTitle,
                textAlign: TextAlign.center,
                style: theme.textTheme.headlineMedium?.copyWith(
                  fontWeight: FontWeight.w700,
                  color: AyurvedaColors.forest,
                ),
              ),
              const SizedBox(height: 10),
              Text(
                l10n.splashTagline,
                textAlign: TextAlign.center,
                style: theme.textTheme.bodyMedium?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              ),
              const Spacer(flex: 2),
              Text(
                l10n.chooseLanguage,
                textAlign: TextAlign.center,
                style: theme.textTheme.titleSmall?.copyWith(
                  fontWeight: FontWeight.w600,
                ),
              ),
              const SizedBox(height: 12),
              const Center(child: LanguageSwitcher()),
              const SizedBox(height: 32),
              FilledButton(
                // Disabled until the stored token has been read, so tapping
                // through cannot race the session restore.
                onPressed: authState.isResolved ? _continue : null,
                child: authState.isResolved
                    ? Text(l10n.continueLabel)
                    : SizedBox.square(
                        dimension: 22,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: theme.colorScheme.onPrimary,
                        ),
                      ),
              ),
              const SizedBox(height: 28),
            ],
          ),
        ),
      ),
    );
  }
}

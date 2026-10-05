import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/network/api_exception.dart';
import '../../../l10n/app_localizations.dart';
import '../../../router/app_routes.dart';
import '../../../theme/app_theme.dart';
import '../data/auth_repository.dart';
import '../domain/auth_form_rules.dart';

/// Keys used by widget tests and Semantics probes.
abstract final class ForgotPasswordScreenKeys {
  static const email = Key('forgot_password_email_field');
  static const submit = Key('forgot_password_submit_button');
  static const backToSignIn = Key('forgot_password_back_button');
  static const success = Key('forgot_password_success');
  static const error = Key('forgot_password_error');
  static const loading = Key('forgot_password_loading');
}

class ForgotPasswordScreen extends ConsumerStatefulWidget {
  const ForgotPasswordScreen({super.key});

  @override
  ConsumerState<ForgotPasswordScreen> createState() =>
      _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends ConsumerState<ForgotPasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  bool _isLoading = false;
  bool _succeeded = false;
  String? _error;

  @override
  void dispose() {
    _emailController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;

    setState(() {
      _isLoading = true;
      _succeeded = false;
      _error = null;
    });

    try {
      await ref.read(authRepositoryProvider).requestReset(
            email: _emailController.text.trim(),
          );
      if (!mounted) return;
      setState(() => _succeeded = true);
    } on ApiException catch (error) {
      if (!mounted) return;
      setState(() => _error = _describe(error));
    } catch (_) {
      if (!mounted) return;
      setState(
        () => _error = AppLocalizations.of(context).forgotPasswordError,
      );
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  String _describe(ApiException error) {
    final l10n = AppLocalizations.of(context);
    if (error.isNetworkError) return l10n.networkErrorMessage;
    return l10n.forgotPasswordError;
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final colors = theme.colorScheme;

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.forgotPasswordTitle),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const SizedBox(height: 16),
                Icon(
                  Icons.lock_reset_rounded,
                  size: 64,
                  color: colors.primary,
                ),
                const SizedBox(height: 16),
                Text(
                  l10n.forgotPasswordHeading,
                  style: theme.textTheme.headlineSmall?.copyWith(
                    fontWeight: FontWeight.bold,
                    fontFamily: AyurvedaFonts.serif,
                    fontFamilyFallback: AyurvedaFonts.fallback,
                  ),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 8),
                Text(
                  l10n.forgotPasswordBody,
                  style: theme.textTheme.bodyMedium?.copyWith(
                    color: colors.onSurfaceVariant,
                  ),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 32),
                if (_succeeded)
                  _StatusBanner(
                    key: ForgotPasswordScreenKeys.success,
                    message: l10n.forgotPasswordSuccess,
                    background: colors.primaryContainer,
                    foreground: colors.onPrimaryContainer,
                    icon: Icons.check_circle_outline,
                    semanticLabel: l10n.forgotPasswordSuccess,
                  ),
                if (_error != null)
                  _StatusBanner(
                    key: ForgotPasswordScreenKeys.error,
                    message: _error!,
                    background: colors.errorContainer,
                    foreground: colors.onErrorContainer,
                    icon: Icons.error_outline,
                    semanticLabel: l10n.forgotPasswordError,
                  ),
                Semantics(
                  textField: true,
                  label: l10n.emailLabel,
                  child: TextFormField(
                    key: ForgotPasswordScreenKeys.email,
                    controller: _emailController,
                    enabled: !_isLoading,
                    keyboardType: TextInputType.emailAddress,
                    textInputAction: TextInputAction.done,
                    autofillHints: const [AutofillHints.email],
                    onFieldSubmitted: (_) => _submit(),
                    decoration: InputDecoration(
                      labelText: l10n.emailLabel,
                      prefixIcon: const Icon(Icons.email_outlined),
                    ),
                    validator: (value) => validateEmailField(value, l10n),
                  ),
                ),
                const SizedBox(height: 24),
                Semantics(
                  button: true,
                  enabled: !_isLoading,
                  label: l10n.forgotPasswordSubmit,
                  child: FilledButton(
                    key: ForgotPasswordScreenKeys.submit,
                    onPressed: _isLoading ? null : _submit,
                    style: FilledButton.styleFrom(
                      minimumSize: const Size(double.infinity, 48),
                    ),
                    child: _isLoading
                        ? SizedBox.square(
                            key: ForgotPasswordScreenKeys.loading,
                            dimension: 20,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: colors.onPrimary,
                            ),
                          )
                        : Text(l10n.forgotPasswordSubmit),
                  ),
                ),
                const SizedBox(height: 16),
                Semantics(
                  button: true,
                  label: l10n.forgotPasswordBackToSignIn,
                  child: TextButton(
                    key: ForgotPasswordScreenKeys.backToSignIn,
                    onPressed: _isLoading
                        ? null
                        : () => context.go(AppRoutes.login),
                    child: Text(l10n.forgotPasswordBackToSignIn),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _StatusBanner extends StatelessWidget {
  const _StatusBanner({
    required this.message,
    required this.background,
    required this.foreground,
    required this.icon,
    required this.semanticLabel,
    super.key,
  });

  final String message;
  final Color background;
  final Color foreground;
  final IconData icon;
  final String semanticLabel;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 24),
      child: Semantics(
        container: true,
        liveRegion: true,
        label: semanticLabel,
        child: Card(
          color: background,
          elevation: 0,
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Row(
              children: [
                Icon(icon, color: foreground),
                const SizedBox(width: 12),
                Expanded(
                  child: Text(
                    message,
                    style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                          color: foreground,
                          fontWeight: FontWeight.w600,
                        ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

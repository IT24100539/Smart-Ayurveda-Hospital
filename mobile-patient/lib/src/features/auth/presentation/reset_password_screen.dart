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
abstract final class ResetPasswordScreenKeys {
  static const email = Key('reset_password_email_field');
  static const token = Key('reset_password_token_field');
  static const newPassword = Key('reset_password_new_password_field');
  static const confirmPassword = Key('reset_password_confirm_password_field');
  static const submit = Key('reset_password_submit_button');
  static const success = Key('reset_password_success');
  static const error = Key('reset_password_error');
  static const signInNow = Key('reset_password_sign_in_now');
  static const loading = Key('reset_password_loading');
}

class ResetPasswordScreen extends ConsumerStatefulWidget {
  const ResetPasswordScreen({
    this.email,
    this.token,
    super.key,
  });

  final String? email;
  final String? token;

  @override
  ConsumerState<ResetPasswordScreen> createState() =>
      _ResetPasswordScreenState();
}

class _ResetPasswordScreenState extends ConsumerState<ResetPasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _emailController;
  late final TextEditingController _tokenController;
  final _newPasswordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();

  bool _isLoading = false;
  bool _obscureNew = true;
  bool _obscureConfirm = true;
  bool _succeeded = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _emailController = TextEditingController(text: widget.email ?? '');
    _tokenController = TextEditingController(text: widget.token ?? '');
  }

  @override
  void dispose() {
    _emailController.dispose();
    _tokenController.dispose();
    _newPasswordController.dispose();
    _confirmPasswordController.dispose();
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
      await ref.read(authRepositoryProvider).completeReset(
            email: _emailController.text.trim(),
            token: _tokenController.text.trim(),
            newPassword: _newPasswordController.text,
            confirmPassword: _confirmPasswordController.text,
          );
      if (!mounted) return;
      setState(() => _succeeded = true);
    } on ApiException catch (error) {
      if (!mounted) return;
      setState(() => _error = _describe(error));
    } catch (_) {
      if (!mounted) return;
      setState(
        () => _error = AppLocalizations.of(context).resetPasswordError,
      );
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  String _describe(ApiException error) {
    final l10n = AppLocalizations.of(context);
    if (error.isNetworkError) return l10n.networkErrorMessage;
    return l10n.resetPasswordError;
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final colors = theme.colorScheme;

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.resetPasswordTitle),
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
                Text(
                  l10n.resetPasswordHeading,
                  style: theme.textTheme.headlineSmall?.copyWith(
                    fontWeight: FontWeight.bold,
                    fontFamily: AyurvedaFonts.serif,
                    fontFamilyFallback: AyurvedaFonts.fallback,
                  ),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 8),
                Text(
                  l10n.resetPasswordBody,
                  style: theme.textTheme.bodyMedium?.copyWith(
                    color: colors.onSurfaceVariant,
                  ),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 32),
                if (_succeeded) ...[
                  Semantics(
                    container: true,
                    liveRegion: true,
                    label: l10n.resetPasswordSuccess,
                    child: Card(
                      key: ResetPasswordScreenKeys.success,
                      color: colors.primaryContainer,
                      elevation: 0,
                      child: Padding(
                        padding: const EdgeInsets.all(16),
                        child: Column(
                          children: [
                            Icon(
                              Icons.check_circle_outline,
                              color: colors.onPrimaryContainer,
                              size: 36,
                            ),
                            const SizedBox(height: 8),
                            Text(
                              l10n.resetPasswordSuccess,
                              style: theme.textTheme.bodyMedium?.copyWith(
                                color: colors.onPrimaryContainer,
                                fontWeight: FontWeight.bold,
                              ),
                              textAlign: TextAlign.center,
                            ),
                            const SizedBox(height: 12),
                            Semantics(
                              button: true,
                              label: l10n.resetPasswordSignInNow,
                              child: FilledButton(
                                key: ResetPasswordScreenKeys.signInNow,
                                onPressed: () => context.go(AppRoutes.login),
                                child: Text(l10n.resetPasswordSignInNow),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(height: 24),
                ],
                if (_error != null)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 24),
                    child: Semantics(
                      container: true,
                      liveRegion: true,
                      label: l10n.resetPasswordError,
                      child: Card(
                        key: ResetPasswordScreenKeys.error,
                        color: colors.errorContainer,
                        elevation: 0,
                        child: Padding(
                          padding: const EdgeInsets.all(16),
                          child: Text(
                            _error!,
                            style: theme.textTheme.bodyMedium?.copyWith(
                              color: colors.onErrorContainer,
                            ),
                          ),
                        ),
                      ),
                    ),
                  ),
                if (!_succeeded) ...[
                  Semantics(
                    textField: true,
                    label: l10n.emailLabel,
                    child: TextFormField(
                      key: ResetPasswordScreenKeys.email,
                      controller: _emailController,
                      enabled: !_isLoading,
                      keyboardType: TextInputType.emailAddress,
                      textInputAction: TextInputAction.next,
                      autofillHints: const [AutofillHints.email],
                      decoration: InputDecoration(
                        labelText: l10n.emailLabel,
                        prefixIcon: const Icon(Icons.email_outlined),
                      ),
                      validator: (value) => validateEmailField(value, l10n),
                    ),
                  ),
                  const SizedBox(height: 16),
                  Semantics(
                    textField: true,
                    label: l10n.resetTokenLabel,
                    child: TextFormField(
                      key: ResetPasswordScreenKeys.token,
                      controller: _tokenController,
                      enabled: !_isLoading,
                      textInputAction: TextInputAction.next,
                      decoration: InputDecoration(
                        labelText: l10n.resetTokenLabel,
                        prefixIcon: const Icon(Icons.key_outlined),
                      ),
                      validator: (value) => (value == null || value.trim().isEmpty)
                          ? l10n.resetTokenRequired
                          : null,
                    ),
                  ),
                  const SizedBox(height: 16),
                  Semantics(
                    textField: true,
                    label: l10n.newPasswordLabel,
                    child: TextFormField(
                      key: ResetPasswordScreenKeys.newPassword,
                      controller: _newPasswordController,
                      enabled: !_isLoading,
                      obscureText: _obscureNew,
                      textInputAction: TextInputAction.next,
                      autofillHints: const [AutofillHints.newPassword],
                      decoration: InputDecoration(
                        labelText: l10n.newPasswordLabel,
                        prefixIcon: const Icon(Icons.lock_outline),
                        suffixIcon: IconButton(
                          tooltip: _obscureNew
                              ? l10n.showPassword
                              : l10n.hidePassword,
                          onPressed: () =>
                              setState(() => _obscureNew = !_obscureNew),
                          icon: Icon(
                            _obscureNew
                                ? Icons.visibility_outlined
                                : Icons.visibility_off_outlined,
                          ),
                        ),
                      ),
                      validator: (value) => validateNewPassword(value, l10n),
                    ),
                  ),
                  const SizedBox(height: 16),
                  Semantics(
                    textField: true,
                    label: l10n.confirmPasswordLabel,
                    child: TextFormField(
                      key: ResetPasswordScreenKeys.confirmPassword,
                      controller: _confirmPasswordController,
                      enabled: !_isLoading,
                      obscureText: _obscureConfirm,
                      textInputAction: TextInputAction.done,
                      onFieldSubmitted: (_) => _submit(),
                      decoration: InputDecoration(
                        labelText: l10n.confirmPasswordLabel,
                        prefixIcon: const Icon(Icons.lock_outline),
                        suffixIcon: IconButton(
                          tooltip: _obscureConfirm
                              ? l10n.showPassword
                              : l10n.hidePassword,
                          onPressed: () => setState(
                            () => _obscureConfirm = !_obscureConfirm,
                          ),
                          icon: Icon(
                            _obscureConfirm
                                ? Icons.visibility_outlined
                                : Icons.visibility_off_outlined,
                          ),
                        ),
                      ),
                      validator: (value) => validatePasswordConfirmation(
                        value,
                        _newPasswordController.text,
                        l10n,
                      ),
                    ),
                  ),
                  const SizedBox(height: 24),
                  Semantics(
                    button: true,
                    enabled: !_isLoading,
                    label: l10n.resetPasswordSubmit,
                    child: FilledButton(
                      key: ResetPasswordScreenKeys.submit,
                      onPressed: _isLoading ? null : _submit,
                      style: FilledButton.styleFrom(
                        minimumSize: const Size(double.infinity, 48),
                      ),
                      child: _isLoading
                          ? SizedBox.square(
                              key: ResetPasswordScreenKeys.loading,
                              dimension: 20,
                              child: CircularProgressIndicator(
                                strokeWidth: 2,
                                color: colors.onPrimary,
                              ),
                            )
                          : Text(l10n.resetPasswordSubmit),
                    ),
                  ),
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }
}

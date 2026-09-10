import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/config/app_config.dart';
import '../../../core/network/api_exception.dart';
import '../../../l10n/app_localizations.dart';
import '../../../theme/app_theme.dart';
import '../application/auth_controller.dart';

enum AuthFormMode { login, register }

/// Keys used by the widget tests and by callers that need to drive the form.
abstract final class LoginScreenKeys {
  static const email = Key('login_email_field');
  static const password = Key('login_password_field');
  static const fullName = Key('login_full_name_field');
  static const phoneNumber = Key('login_phone_number_field');
  static const submit = Key('login_submit_button');
  static const modeToggle = Key('login_mode_toggle');
}

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _fullNameController = TextEditingController();
  final _emailController = TextEditingController();
  final _phoneNumberController = TextEditingController();
  final _passwordController = TextEditingController();

  AuthFormMode _mode = AuthFormMode.login;
  bool _isSubmitting = false;
  bool _obscurePassword = true;
  String? _errorMessage;

  bool get _isRegister => _mode == AuthFormMode.register;

  @override
  void dispose() {
    _fullNameController.dispose();
    _emailController.dispose();
    _phoneNumberController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  void _toggleMode() {
    setState(() {
      _mode = _isRegister ? AuthFormMode.login : AuthFormMode.register;
      _errorMessage = null;
    });
    _formKey.currentState?.reset();
  }

  Future<void> _submit() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;

    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    final controller = ref.read(authControllerProvider.notifier);
    final email = _emailController.text.trim();
    final password = _passwordController.text;

    try {
      if (_isRegister) {
        await controller.register(
          fullName: _fullNameController.text.trim(),
          email: email,
          phoneNumber: _phoneNumberController.text.trim(),
          password: password,
        );
      } else {
        await controller.login(email: email, password: password);
      }
      // On success the auth status flips and the router redirects to Home, so
      // this screen is disposed; nothing more to do here.
    } on ApiException catch (error) {
      if (!mounted) return;
      setState(() => _errorMessage = _describe(error));
    } catch (_) {
      if (!mounted) return;
      setState(
        () => _errorMessage = AppLocalizations.of(context).genericErrorMessage,
      );
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  String _describe(ApiException error) {
    final l10n = AppLocalizations.of(context);
    if (error.isNetworkError) return l10n.networkErrorMessage;
    if (error.isUnauthorized) return l10n.invalidCredentialsMessage;
    if (error.isConflict) return l10n.emailAlreadyRegisteredMessage;
    return error.message ?? l10n.genericErrorMessage;
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final sessionExpired = ref.watch(authControllerProvider).sessionExpired;

    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 32),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 420),
              child: Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Icon(
                      Icons.spa,
                      size: 40,
                      color: theme.colorScheme.primary,
                    ),
                    const SizedBox(height: 18),
                    Text(
                      _isRegister ? l10n.register : l10n.signIn,
                      textAlign: TextAlign.center,
                      style: theme.textTheme.headlineSmall?.copyWith(
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      _isRegister
                          ? l10n.registerSubtitle
                          : l10n.signInSubtitle,
                      textAlign: TextAlign.center,
                      style: theme.textTheme.bodyMedium?.copyWith(
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                    const SizedBox(height: 28),

                    if (sessionExpired && _errorMessage == null)
                      _MessageBanner(
                        message: l10n.sessionExpiredMessage,
                        color: theme.colorScheme.secondaryContainer,
                        textColor: theme.colorScheme.onSecondaryContainer,
                        icon: Icons.info_outline,
                      ),
                    if (_errorMessage != null)
                      _MessageBanner(
                        message: _errorMessage!,
                        color: AyurvedaColors.danger.withValues(alpha: 0.10),
                        textColor: AyurvedaColors.danger,
                        icon: Icons.error_outline,
                      ),

                    if (_isRegister) ...[
                      TextFormField(
                        key: LoginScreenKeys.fullName,
                        controller: _fullNameController,
                        textInputAction: TextInputAction.next,
                        textCapitalization: TextCapitalization.words,
                        autofillHints: const [AutofillHints.name],
                        decoration: InputDecoration(
                          labelText: l10n.fullNameLabel,
                          prefixIcon: const Icon(Icons.person_outline),
                        ),
                        validator: (value) =>
                            (value == null || value.trim().isEmpty)
                            ? l10n.fullNameRequired
                            : null,
                      ),
                      const SizedBox(height: 16),
                    ],

                    TextFormField(
                      key: LoginScreenKeys.email,
                      controller: _emailController,
                      keyboardType: TextInputType.emailAddress,
                      textInputAction: TextInputAction.next,
                      autofillHints: const [AutofillHints.email],
                      decoration: InputDecoration(
                        labelText: l10n.emailLabel,
                        prefixIcon: const Icon(Icons.mail_outline),
                      ),
                      validator: (value) {
                        final email = value?.trim() ?? '';
                        if (email.isEmpty) return l10n.emailRequired;
                        if (!_emailPattern.hasMatch(email)) {
                          return l10n.emailInvalid;
                        }
                        return null;
                      },
                    ),

                    if (_isRegister) ...[
                      const SizedBox(height: 16),
                      TextFormField(
                        key: LoginScreenKeys.phoneNumber,
                        controller: _phoneNumberController,
                        keyboardType: TextInputType.phone,
                        textInputAction: TextInputAction.next,
                        autofillHints: const [AutofillHints.telephoneNumber],
                        decoration: InputDecoration(
                          labelText: l10n.phoneNumberLabel,
                          prefixIcon: const Icon(Icons.phone_outlined),
                        ),
                        validator: (value) =>
                            (value == null || value.trim().isEmpty)
                            ? l10n.phoneNumberRequired
                            : null,
                      ),
                    ],

                    const SizedBox(height: 16),
                    TextFormField(
                      key: LoginScreenKeys.password,
                      controller: _passwordController,
                      obscureText: _obscurePassword,
                      textInputAction: TextInputAction.done,
                      autofillHints: const [AutofillHints.password],
                      onFieldSubmitted: (_) => _submit(),
                      decoration: InputDecoration(
                        labelText: l10n.passwordLabel,
                        prefixIcon: const Icon(Icons.lock_outline),
                        suffixIcon: IconButton(
                          onPressed: () => setState(
                            () => _obscurePassword = !_obscurePassword,
                          ),
                          icon: Icon(
                            _obscurePassword
                                ? Icons.visibility_outlined
                                : Icons.visibility_off_outlined,
                          ),
                        ),
                      ),
                      validator: (value) {
                        final password = value ?? '';
                        if (password.isEmpty) return l10n.passwordRequired;
                        if (password.length < AppConfig.minPasswordLength) {
                          return l10n.passwordTooShort(
                            AppConfig.minPasswordLength,
                          );
                        }
                        if (password.length > AppConfig.maxPasswordLength) {
                          return l10n.passwordTooLong(
                            AppConfig.maxPasswordLength,
                          );
                        }
                        return null;
                      },
                    ),

                    const SizedBox(height: 28),
                    FilledButton(
                      key: LoginScreenKeys.submit,
                      onPressed: _isSubmitting ? null : _submit,
                      child: _isSubmitting
                          ? const SizedBox.square(
                              dimension: 22,
                              child: CircularProgressIndicator(
                                strokeWidth: 2,
                                color: Colors.white,
                              ),
                            )
                          : Text(_isRegister ? l10n.register : l10n.signIn),
                    ),
                    const SizedBox(height: 8),
                    TextButton(
                      key: LoginScreenKeys.modeToggle,
                      onPressed: _isSubmitting ? null : _toggleMode,
                      child: Text(
                        _isRegister ? l10n.haveAccount : l10n.needAccount,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

/// Intentionally permissive; `EmailValidator` on the API is the real gate.
final _emailPattern = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');

class _MessageBanner extends StatelessWidget {
  const _MessageBanner({
    required this.message,
    required this.color,
    required this.textColor,
    required this.icon,
  });

  final String message;
  final Color color;
  final Color textColor;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: 20),
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
      decoration: BoxDecoration(
        color: color,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        children: [
          Icon(icon, size: 20, color: textColor),
          const SizedBox(width: 10),
          Expanded(
            child: Text(
              message,
              style: Theme.of(
                context,
              ).textTheme.bodySmall?.copyWith(color: textColor),
            ),
          ),
        ],
      ),
    );
  }
}

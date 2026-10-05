import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../l10n/feature_localizations.dart';
import '../../../../router/app_routes.dart';
import '../../../../shared/widgets/clinic_widgets.dart';
import '../../../auth/application/auth_controller.dart';
import '../../data/treatments_repository.dart';
import '../../domain/treatment_models.dart';

/// Patient ask card for the treatment-info agent (catalogue days and fees).
class AskTreatmentCard extends ConsumerStatefulWidget {
  const AskTreatmentCard({super.key});

  @override
  ConsumerState<AskTreatmentCard> createState() => _AskTreatmentCardState();
}

class _AskTreatmentCardState extends ConsumerState<AskTreatmentCard> {
  final _controller = TextEditingController();
  bool _busy = false;
  String? _error;
  TreatmentAskResult? _result;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final question = _controller.text.trim();
    if (question.isEmpty || _busy) return;

    final auth = ref.read(authControllerProvider);
    if (!auth.isAuthenticated) {
      if (!mounted) return;
      context.push(AppRoutes.loginWithReturn(AppRoutes.treatments));
      return;
    }

    setState(() {
      _busy = true;
      _error = null;
      _result = null;
    });

    try {
      final result = await ref
          .read(treatmentsRepositoryProvider)
          .askTreatmentInfo(question);
      if (!mounted) return;
      setState(() => _result = result);
    } on ApiException catch (error) {
      if (!mounted) return;
      final copy = FeatureLocalizations.of(context);
      setState(() => _error = error.message ?? copy.askUnavailable);
    } catch (_) {
      if (!mounted) return;
      setState(() => _error = FeatureLocalizations.of(context).askUnavailable);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final copy = FeatureLocalizations.of(context);
    final theme = Theme.of(context);
    final signedIn = ref.watch(authControllerProvider).isAuthenticated;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              copy.askAboutTreatments,
              style: theme.textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.w600,
              ),
            ),
            const SizedBox(height: 6),
            Text(
              copy.askAboutTreatmentsHint,
              style: theme.textTheme.bodySmall?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
            const SizedBox(height: 12),
            if (!signedIn) ...[
              Text(
                copy.askSignInRequired,
                style: theme.textTheme.bodyMedium,
              ),
              const SizedBox(height: 12),
              OutlinedButton(
                onPressed: () =>
                    context.push(AppRoutes.loginWithReturn(AppRoutes.treatments)),
                child: Text(copy.askSignIn),
              ),
            ] else ...[
              TextField(
                key: const Key('ask-treatment-question'),
                controller: _controller,
                maxLines: 3,
                maxLength: 2000,
                enabled: !_busy,
                textInputAction: TextInputAction.newline,
                decoration: InputDecoration(
                  labelText: copy.askQuestionLabel,
                  hintText: copy.askQuestionPlaceholder,
                  alignLabelWithHint: true,
                ),
                onChanged: (_) => setState(() {}),
              ),
              const SizedBox(height: 8),
              Align(
                alignment: Alignment.centerLeft,
                child: FilledButton(
                  key: const Key('ask-treatment-submit'),
                  onPressed:
                      _busy || _controller.text.trim().isEmpty ? null : _submit,
                  child: Text(_busy ? copy.asking : copy.askButton),
                ),
              ),
            ],
            if (_error != null) ...[
              const SizedBox(height: 12),
              ErrorLine(message: _error!),
            ],
            if (_result != null) ...[
              const SizedBox(height: 12),
              if (_result!.refused)
                Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: ErrorLine(message: copy.medicalAdviceRefused),
                ),
              DecoratedBox(
                key: const Key('ask-treatment-reply'),
                decoration: BoxDecoration(
                  color: _result!.refused
                      ? theme.colorScheme.secondaryContainer
                      : theme.colorScheme.primaryContainer,
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: theme.colorScheme.outline),
                ),
                child: Padding(
                  padding: const EdgeInsets.all(12),
                  child: Text(
                    _result!.answer,
                    style: theme.textTheme.bodyMedium?.copyWith(
                      color: theme.colorScheme.onSurface,
                    ),
                  ),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../l10n/feature_localizations.dart';
import '../../../../theme/app_theme.dart';
import '../../../auth/application/auth_controller.dart';
import '../../data/patient_info_repository.dart';

/// Patient ask card for the patient-info agent (administrative details such as UHID, district, registered contact).
class AskPatientInfoCard extends ConsumerStatefulWidget {
  const AskPatientInfoCard({super.key});

  @override
  ConsumerState<AskPatientInfoCard> createState() => _AskPatientInfoCardState();
}

class _AskPatientInfoCardState extends ConsumerState<AskPatientInfoCard> {
  final _controller = TextEditingController();
  bool _busy = false;
  String? _error;
  PatientInfoAskResult? _result;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final question = _controller.text.trim();
    if (question.isEmpty || _busy) return;

    final auth = ref.read(authControllerProvider);
    if (!auth.isAuthenticated) return;

    setState(() {
      _busy = true;
      _error = null;
      _result = null;
    });

    try {
      final result = await ref
          .read(patientInfoRepositoryProvider)
          .askPatientInfo(question);
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

    if (!signedIn) return const SizedBox.shrink();

    final title = copy.text(
      'Ask About Your Hospital Record',
      'ඔබේ රෝහල් වාර්තාව ගැන විමසන්න',
    );
    final hint = copy.text(
      'Ask questions about your registered details, district, assigned UHID, or recorded Prakriti. Medical advice questions are refused.',
      'ඔබගේ ලියාපදිංචි විස්තර, දිස්ත්‍රික්කය, UHID අංකය හෝ ප්‍රකෘතිය පිළිබඳව විමසන්න. වෛද්‍ය උපදෙස් ප්‍රශ්න ප්‍රතික්ෂේප කරනු ලැබේ.',
    );
    final questionPlaceholder = copy.text(
      'e.g. What district am I registered in?',
      'උදා: මම ලියාපදිංචි වී ඇති දිස්ත්‍රික්කය කුමක්ද?',
    );

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                const Icon(
                  Icons.psychology_alt_outlined,
                  color: AyurvedaColors.forest,
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    title,
                    style: theme.textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 6),
            Text(
              hint,
              style: theme.textTheme.bodySmall?.copyWith(
                color: AyurvedaColors.inkMuted,
              ),
            ),
            const SizedBox(height: 12),
            TextField(
              key: const Key('ask-patient-info-question'),
              controller: _controller,
              maxLines: 2,
              maxLength: 1000,
              enabled: !_busy,
              textInputAction: TextInputAction.newline,
              decoration: InputDecoration(
                labelText: copy.askQuestionLabel,
                hintText: questionPlaceholder,
                alignLabelWithHint: true,
              ),
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: 8),
            Align(
              alignment: Alignment.centerLeft,
              child: FilledButton.icon(
                key: const Key('ask-patient-info-submit'),
                icon: _busy
                    ? const SizedBox(
                        width: 16,
                        height: 16,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: Colors.white,
                        ),
                      )
                    : const Icon(Icons.send, size: 16),
                onPressed: _busy || _controller.text.trim().isEmpty ? null : _submit,
                label: Text(_busy ? copy.asking : copy.askButton),
              ),
            ),
            if (_error != null) ...[
              const SizedBox(height: 12),
              Text(
                _error!,
                style: theme.textTheme.bodySmall?.copyWith(
                  color: AyurvedaColors.danger,
                ),
              ),
            ],
            if (_result != null) ...[
              const SizedBox(height: 12),
              if (_result!.refused)
                Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: Text(
                    copy.medicalAdviceRefused,
                    style: theme.textTheme.labelLarge?.copyWith(
                      color: AyurvedaColors.danger,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
              DecoratedBox(
                decoration: BoxDecoration(
                  color: _result!.refused
                      ? AyurvedaColors.goldMuted
                      : AyurvedaColors.sageMuted,
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: AyurvedaColors.border),
                ),
                child: Padding(
                  padding: const EdgeInsets.all(12),
                  child: Text(
                    _result!.answer,
                    style: theme.textTheme.bodyMedium,
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

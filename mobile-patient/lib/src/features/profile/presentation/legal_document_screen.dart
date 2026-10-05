import 'package:flutter/material.dart';

import '../../../l10n/app_localizations.dart';

abstract final class LegalDocumentKeys {
  static const placeholderBanner = ValueKey('legal-placeholder-banner');
}

/// Privacy policy. The body is a placeholder until the hospital supplies copy.
class PrivacyPolicyScreen extends StatelessWidget {
  const PrivacyPolicyScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return LegalDocumentScreen(
      title: l10n.privacyPolicyTitle,
      body: l10n.privacyPolicyBody,
      extraTitle: l10n.retentionTitle,
      extraBody: l10n.retentionNote,
    );
  }
}

/// Terms of use. The body is a placeholder until the hospital supplies copy.
class TermsOfUseScreen extends StatelessWidget {
  const TermsOfUseScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return LegalDocumentScreen(
      title: l10n.termsOfUseTitle,
      body: l10n.termsOfUseBody,
    );
  }
}

class LegalDocumentScreen extends StatelessWidget {
  const LegalDocumentScreen({
    required this.title,
    required this.body,
    this.extraTitle,
    this.extraBody,
    super.key,
  });

  final String title;
  final String body;
  final String? extraTitle;
  final String? extraBody;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final extraTitle = this.extraTitle;
    final extraBody = this.extraBody;

    return Scaffold(
      appBar: AppBar(title: Text(title)),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(20, 12, 20, 32),
        children: [
          const LegalPlaceholderBanner(),
          const SizedBox(height: 16),
          Text(body, style: theme.textTheme.bodyLarge),
          if (extraTitle != null && extraBody != null) ...[
            const SizedBox(height: 24),
            Text(
              extraTitle,
              style: theme.textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 8),
            Text(extraBody, style: theme.textTheme.bodyLarge),
          ],
        ],
      ),
    );
  }
}

/// Marks legal copy that must not be treated as the hospital's wording.
class LegalPlaceholderBanner extends StatelessWidget {
  const LegalPlaceholderBanner({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    return Material(
      key: LegalDocumentKeys.placeholderBanner,
      color: scheme.errorContainer,
      borderRadius: BorderRadius.circular(12),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(Icons.warning_amber_rounded, color: scheme.onErrorContainer),
            const SizedBox(width: 10),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    l10n.legalPlaceholderBanner,
                    style: theme.textTheme.titleSmall?.copyWith(
                      color: scheme.onErrorContainer,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    l10n.legalPlaceholderHint,
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: scheme.onErrorContainer,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

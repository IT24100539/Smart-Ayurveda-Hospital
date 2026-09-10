import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'app_localizations.dart';
import 'locale_controller.dart';

/// Segmented si/en toggle bound to [localeControllerProvider].
class LanguageSwitcher extends ConsumerWidget {
  const LanguageSwitcher({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final current = ref.watch(localeControllerProvider);

    return SegmentedButton<String>(
      segments: [
        ButtonSegment(
          value: 'si',
          label: Text(l10n.languageSinhala),
        ),
        ButtonSegment(
          value: 'en',
          label: Text(l10n.languageEnglish),
        ),
      ],
      selected: {current.languageCode},
      showSelectedIcon: false,
      onSelectionChanged: (selection) {
        ref
            .read(localeControllerProvider.notifier)
            .setLocale(Locale(selection.first));
      },
    );
  }
}

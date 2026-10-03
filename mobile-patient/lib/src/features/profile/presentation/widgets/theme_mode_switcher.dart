import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../l10n/app_localizations.dart';
import '../../../../theme/theme_mode_controller.dart';

abstract final class ThemeModeSwitcherKeys {
  static const control = ValueKey('profile-theme-switcher');
}

/// System, light, or dark. The choice is written locally by [ThemeModeController].
class ThemeModeSwitcher extends ConsumerWidget {
  const ThemeModeSwitcher({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final mode = ref.watch(themeControllerProvider);

    return SegmentedButton<ThemeMode>(
      key: ThemeModeSwitcherKeys.control,
      showSelectedIcon: false,
      segments: [
        ButtonSegment(value: ThemeMode.system, label: Text(l10n.themeSystem)),
        ButtonSegment(value: ThemeMode.light, label: Text(l10n.themeLight)),
        ButtonSegment(value: ThemeMode.dark, label: Text(l10n.themeDark)),
      ],
      selected: {mode},
      onSelectionChanged: (selection) {
        ref.read(themeControllerProvider.notifier).setMode(selection.first);
      },
    );
  }
}

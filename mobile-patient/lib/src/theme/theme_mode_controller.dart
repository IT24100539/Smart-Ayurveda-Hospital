import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'theme_mode_storage.dart';

/// Seeded by [main] after reading storage so the first frame matches the saved mode.
final initialThemeModeProvider = Provider<ThemeMode?>((ref) => null);

/// Appearance choice for the patient app, restored from local storage.
class ThemeModeController extends Notifier<ThemeMode> {
  bool _userChose = false;

  @override
  ThemeMode build() {
    final seeded = ref.read(initialThemeModeProvider);
    if (seeded != null) return seeded;
    Future.microtask(_restore);
    return ThemeMode.system;
  }

  Future<void> _restore() async {
    final stored = await ref.read(themeModeStorageProvider).read();
    if (_userChose) return;
    state = stored;
  }

  Future<void> setMode(ThemeMode mode) async {
    _userChose = true;
    state = mode;
    await ref.read(themeModeStorageProvider).write(mode);
  }
}

final themeControllerProvider =
    NotifierProvider<ThemeModeController, ThemeMode>(ThemeModeController.new);

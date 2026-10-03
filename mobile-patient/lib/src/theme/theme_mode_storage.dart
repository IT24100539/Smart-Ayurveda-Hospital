import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Local preference for light, dark, or system appearance.
///
/// Not a secret. Stored beside other device preferences so it survives
/// sign-out. Tests use [InMemoryThemeModeStorage].
abstract interface class ThemeModeStorage {
  Future<ThemeMode> read();
  Future<void> write(ThemeMode mode);
}

ThemeMode decodeThemeMode(String? raw) {
  return switch (raw) {
    'light' => ThemeMode.light,
    'dark' => ThemeMode.dark,
    _ => ThemeMode.system,
  };
}

String encodeThemeMode(ThemeMode mode) {
  return switch (mode) {
    ThemeMode.light => 'light',
    ThemeMode.dark => 'dark',
    ThemeMode.system => 'system',
  };
}

class SecureThemeModeStorage implements ThemeModeStorage {
  SecureThemeModeStorage(this._storage);

  static const _key = 'theme_mode';

  final FlutterSecureStorage _storage;

  @override
  Future<ThemeMode> read() async {
    try {
      return decodeThemeMode(await _storage.read(key: _key));
    } catch (_) {
      return ThemeMode.system;
    }
  }

  @override
  Future<void> write(ThemeMode mode) async {
    try {
      await _storage.write(key: _key, value: encodeThemeMode(mode));
    } catch (_) {}
  }
}

class InMemoryThemeModeStorage implements ThemeModeStorage {
  InMemoryThemeModeStorage([this._mode = ThemeMode.system]);

  ThemeMode _mode;

  @override
  Future<ThemeMode> read() async => _mode;

  @override
  Future<void> write(ThemeMode mode) async => _mode = mode;
}

ThemeModeStorage createThemeModeStorage() {
  return SecureThemeModeStorage(
    const FlutterSecureStorage(
      aOptions: AndroidOptions(),
      iOptions: IOSOptions(
        accessibility: KeychainAccessibility.first_unlock_this_device,
      ),
      webOptions: WebOptions(
        dbName: 'FlutterEncryptedStorage',
        publicKey: 'FlutterSecureStorage',
      ),
    ),
  );
}

final themeModeStorageProvider = Provider<ThemeModeStorage>((ref) {
  return createThemeModeStorage();
});

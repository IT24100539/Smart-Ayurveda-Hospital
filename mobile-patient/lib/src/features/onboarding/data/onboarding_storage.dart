import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

abstract interface class OnboardingStorage {
  Future<bool> hasSeenOnboarding();
  Future<void> markOnboardingSeen();
  Future<void> reset();
}

class SecureOnboardingStorage implements OnboardingStorage {
  SecureOnboardingStorage(this._storage);

  static const _seenKey = 'has_seen_onboarding';
  final FlutterSecureStorage _storage;
  bool? _cachedSeen;

  @override
  Future<bool> hasSeenOnboarding() async {
    if (_cachedSeen != null) return _cachedSeen!;
    try {
      final val = await _storage.read(key: _seenKey);
      _cachedSeen = val == 'true';
      return _cachedSeen!;
    } catch (_) {
      return false;
    }
  }

  @override
  Future<void> markOnboardingSeen() async {
    _cachedSeen = true;
    try {
      await _storage.write(key: _seenKey, value: 'true');
    } catch (_) {}
  }

  @override
  Future<void> reset() async {
    _cachedSeen = false;
    try {
      await _storage.delete(key: _seenKey);
    } catch (_) {}
  }
}

class InMemoryOnboardingStorage implements OnboardingStorage {
  InMemoryOnboardingStorage({bool seen = false}) : _seen = seen;

  bool _seen;

  @override
  Future<bool> hasSeenOnboarding() async => _seen;

  @override
  Future<void> markOnboardingSeen() async => _seen = true;

  @override
  Future<void> reset() async => _seen = false;
}

final onboardingStorageProvider = Provider<OnboardingStorage>((ref) {
  return SecureOnboardingStorage(
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
});

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Persistence for the patient's JWT.
///
/// An interface rather than a concrete class so widget tests can swap in
/// [InMemoryTokenStorage] and stay off the platform channels.
abstract interface class TokenStorage {
  Future<String?> readToken();
  Future<void> writeToken(String token);
  Future<void> clear();
}

class SecureTokenStorage implements TokenStorage {
  const SecureTokenStorage(this._storage);

  static const _tokenKey = 'auth_token';

  final FlutterSecureStorage _storage;

  @override
  Future<String?> readToken() => _storage.read(key: _tokenKey);

  @override
  Future<void> writeToken(String token) =>
      _storage.write(key: _tokenKey, value: token);

  @override
  Future<void> clear() => _storage.delete(key: _tokenKey);
}

class InMemoryTokenStorage implements TokenStorage {
  InMemoryTokenStorage([this._token]);

  String? _token;

  @override
  Future<String?> readToken() async => _token;

  @override
  Future<void> writeToken(String token) async => _token = token;

  @override
  Future<void> clear() async => _token = null;
}

final tokenStorageProvider = Provider<TokenStorage>((ref) {
  return const SecureTokenStorage(
    FlutterSecureStorage(
      // Android defaults to AES-GCM with RSA-OAEP key wrapping in v11.
      aOptions: AndroidOptions(),
      // Readable in the background after one unlock, but never synced to
      // iCloud or restored onto a different device.
      iOptions: IOSOptions(
        accessibility: KeychainAccessibility.first_unlock_this_device,
      ),
    ),
  );
});

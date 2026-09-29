import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Persistence for the patient's JWT and a lightweight user snapshot.
///
/// An interface rather than a concrete class so widget tests can swap in
/// [InMemoryTokenStorage] and stay off the platform channels.
abstract interface class TokenStorage {
  Future<String?> readToken();
  Future<String?> readUserJson();
  Future<void> writeSession({required String token, String? userJson});
  Future<void> writeToken(String token);
  Future<void> clear();
}

class SecureTokenStorage implements TokenStorage {
  SecureTokenStorage(this._storage);

  static const _tokenKey = 'auth_token';
  static const _userKey = 'auth_user';

  final FlutterSecureStorage _storage;
  String? _cachedToken;
  String? _cachedUserJson;

  @override
  Future<String?> readToken() async =>
      _cachedToken ??= await _storage.read(key: _tokenKey);

  @override
  Future<String?> readUserJson() async =>
      _cachedUserJson ??= await _storage.read(key: _userKey);

  @override
  Future<void> writeSession({required String token, String? userJson}) async {
    _cachedToken = token;
    _cachedUserJson = userJson;
    await _storage.write(key: _tokenKey, value: token);
    if (userJson == null) {
      await _storage.delete(key: _userKey);
    } else {
      await _storage.write(key: _userKey, value: userJson);
    }
  }

  @override
  Future<void> writeToken(String token) => writeSession(token: token);

  @override
  Future<void> clear() async {
    _cachedToken = null;
    _cachedUserJson = null;
    await _storage.delete(key: _tokenKey);
    await _storage.delete(key: _userKey);
  }
}

class InMemoryTokenStorage implements TokenStorage {
  InMemoryTokenStorage([this._token, this._userJson]);

  String? _token;
  String? _userJson;

  @override
  Future<String?> readToken() async => _token;

  @override
  Future<String?> readUserJson() async => _userJson;

  @override
  Future<void> writeSession({required String token, String? userJson}) async {
    _token = token;
    _userJson = userJson;
  }

  @override
  Future<void> writeToken(String token) async => _token = token;

  @override
  Future<void> clear() async {
    _token = null;
    _userJson = null;
  }
}

final tokenStorageProvider = Provider<TokenStorage>((ref) {
  return SecureTokenStorage(
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

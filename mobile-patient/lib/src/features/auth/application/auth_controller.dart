import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/storage/token_storage.dart';
import '../data/auth_repository.dart';
import '../domain/auth_models.dart';

enum AuthStatus {
  /// Startup state, before the stored token has been read. The router holds the
  /// patient on the splash screen until this resolves.
  unknown,
  authenticated,
  unauthenticated,
}

class AuthState {
  const AuthState({
    required this.status,
    this.user,
    this.sessionExpired = false,
  });

  const AuthState.unknown() : this(status: AuthStatus.unknown);

  final AuthStatus status;
  final AuthUser? user;

  /// Set when a 401 ended the session, so the login screen can explain why the
  /// patient is back there.
  final bool sessionExpired;

  bool get isAuthenticated => status == AuthStatus.authenticated;
  bool get isResolved => status != AuthStatus.unknown;
}

class AuthController extends Notifier<AuthState> {
  @override
  AuthState build() => const AuthState.unknown();

  TokenStorage get _tokenStorage => ref.read(tokenStorageProvider);

  /// Reads the persisted token on startup.
  ///
  /// The token is not verified against the API here; the first authenticated
  /// request will 401 and [handleUnauthorized] will clean up if it is stale.
  Future<void> restoreSession() async {
    final token = await _tokenStorage.readToken();
    final hasToken = token != null && token.isNotEmpty;
    state = AuthState(
      status: hasToken ? AuthStatus.authenticated : AuthStatus.unauthenticated,
      user: hasToken ? AuthUser.fromJwt(token) : null,
    );
  }

  Future<void> login({required String email, required String password}) async {
    final result = await ref
        .read(authRepositoryProvider)
        .login(email: email, password: password);
    await _persist(result);
  }

  Future<void> register({
    required String fullName,
    required String email,
    required String phoneNumber,
    required String password,
  }) async {
    final result = await ref
        .read(authRepositoryProvider)
        .register(
          fullName: fullName,
          email: email,
          phoneNumber: phoneNumber,
          password: password,
        );
    await _persist(result);
  }

  Future<void> signOut() async {
    await _tokenStorage.clear();
    state = const AuthState(status: AuthStatus.unauthenticated);
  }

  /// Invoked by `UnauthorizedInterceptor` after it clears a rejected token.
  Future<void> handleUnauthorized() async {
    if (state.status == AuthStatus.unauthenticated) return;
    state = const AuthState(
      status: AuthStatus.unauthenticated,
      sessionExpired: true,
    );
  }

  void acknowledgeSessionExpired() {
    if (!state.sessionExpired) return;
    state = AuthState(status: state.status, user: state.user);
  }

  Future<void> _persist(AuthResult result) async {
    await _tokenStorage.writeToken(result.token);
    state = AuthState(status: AuthStatus.authenticated, user: result.user);
  }
}

final authControllerProvider = NotifierProvider<AuthController, AuthState>(
  AuthController.new,
);

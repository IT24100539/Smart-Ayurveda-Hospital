import 'dart:convert';

/// Mirrors `Hospital.Domain/Enums/UserRole.cs`, which is serialized as a
/// PascalCase string by `JsonStringEnumConverter`.
enum UserRole {
  patient('Patient'),
  frontDeskStaff('FrontDeskStaff'),
  doctor('Doctor'),
  admin('Admin');

  const UserRole(this.wireName);

  final String wireName;

  static UserRole fromWire(Object? value) {
    return UserRole.values.firstWhere(
      (role) => role.wireName == value,
      orElse: () => UserRole.patient,
    );
  }
}

/// Mirrors the `UserSummary` record returned inside `AuthResponse`.
class AuthUser {
  const AuthUser({
    required this.id,
    required this.fullName,
    required this.email,
    required this.phoneNumber,
    required this.role,
  });

  final String id;
  final String fullName;
  final String email;
  final String phoneNumber;
  final UserRole role;

  /// First word of [fullName], for greetings.
  String get firstName {
    final trimmed = fullName.trim();
    if (trimmed.isEmpty) return '';
    return trimmed.split(RegExp(r'\s+')).first;
  }

  factory AuthUser.fromJson(Map<String, dynamic> json) {
    return AuthUser(
      id: json['id'] as String? ?? '',
      fullName: json['fullName'] as String? ?? '',
      email: json['email'] as String? ?? '',
      phoneNumber: json['phoneNumber'] as String? ?? '',
      role: UserRole.fromWire(json['role']),
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'fullName': fullName,
    'email': email,
    'phoneNumber': phoneNumber,
    'role': role.wireName,
  };

  /// Rebuilds the lightweight signed-in user after a browser/app restart.
  /// Signature validation remains the API's responsibility on the next call;
  /// this only reads claims from the already persisted token for UI state.
  static AuthUser? fromJwt(String token) {
    try {
      final parts = token.split('.');
      if (parts.length != 3) return null;
      final payload =
          jsonDecode(
                utf8.decode(base64Url.decode(base64Url.normalize(parts[1]))),
              )
              as Map<String, dynamic>;
      String? claim(String shortName, String schemaName) =>
          (payload[shortName] ?? payload[schemaName])?.toString();

      final id = claim(
        'sub',
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier',
      );
      if (id == null || id.isEmpty) return null;
      return AuthUser(
        id: id,
        fullName:
            claim(
              'name',
              'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name',
            ) ??
            '',
        email:
            claim(
              'email',
              'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress',
            ) ??
            '',
        phoneNumber: '',
        role: UserRole.fromWire(
          claim(
            'role',
            'http://schemas.microsoft.com/ws/2008/06/identity/claims/role',
          ),
        ),
      );
    } catch (_) {
      return null;
    }
  }
}

/// Mirrors the `AuthResponse` record from `POST /api/auth/login|register`.
class AuthResult {
  const AuthResult({
    required this.token,
    required this.expiresAt,
    required this.user,
  });

  final String token;
  final DateTime expiresAt;
  final AuthUser user;

  factory AuthResult.fromJson(Map<String, dynamic> json) {
    return AuthResult(
      token: json['token'] as String,
      expiresAt:
          DateTime.tryParse(json['expiresAt'] as String? ?? '')?.toLocal() ??
          DateTime.now(),
      user: AuthUser.fromJson(json['user'] as Map<String, dynamic>),
    );
  }
}

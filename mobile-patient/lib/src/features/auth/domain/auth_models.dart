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

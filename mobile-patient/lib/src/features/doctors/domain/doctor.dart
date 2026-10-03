/// Physician directory record from `GET /api/doctors`.
///
/// Ratings are present only when the API computed them from visible feedback.
/// A missing rating stays null so the UI never invents a score.
class Doctor {
  const Doctor({
    required this.id,
    required this.name,
    required this.specialty,
    required this.qualifications,
    required this.isActive,
    required this.isSample,
    required this.hasPhoto,
    this.bio,
    this.photoUrl,
    this.rating,
    this.ratingCount,
  });

  final String id;
  final String name;
  final String specialty;
  final String qualifications;
  final String? bio;
  final bool isActive;
  final bool isSample;
  final bool hasPhoto;
  final String? photoUrl;
  final double? rating;
  final int? ratingCount;

  /// True only when the API returned both an average and a positive count.
  bool get showsRating =>
      rating != null && ratingCount != null && ratingCount! > 0;

  String get initials => initialsFor(name);

  factory Doctor.fromJson(Map<String, dynamic> json) {
    return Doctor(
      id: (json['id'] ?? '').toString(),
      name: (json['name'] ?? '').toString(),
      specialty: (json['specialty'] ?? '').toString(),
      qualifications: (json['qualifications'] ?? '').toString(),
      bio: _blankToNull(json['bio']?.toString()),
      isActive: json['isActive'] as bool? ?? false,
      isSample: json['isSample'] as bool? ?? false,
      hasPhoto: json['hasPhoto'] as bool? ?? false,
      photoUrl: _blankToNull(json['photoUrl']?.toString()),
      rating: _asDouble(json['rating']),
      ratingCount: _asInt(json['ratingCount']),
    );
  }
}

class DoctorPage {
  const DoctorPage({
    required this.items,
    required this.totalCount,
    required this.page,
    required this.pageSize,
  });

  final List<Doctor> items;
  final int totalCount;
  final int page;
  final int pageSize;

  factory DoctorPage.fromJson(Map<String, dynamic> json) {
    final rawItems = json['items'] as List<dynamic>? ?? const [];
    return DoctorPage(
      items: rawItems
          .map((item) => Doctor.fromJson(Map<String, dynamic>.from(item as Map)))
          .toList(),
      totalCount: json['totalCount'] as int? ?? 0,
      page: json['page'] as int? ?? 1,
      pageSize: json['pageSize'] as int? ?? rawItems.length,
    );
  }
}

/// First letter of the given name and family name, or the first letter of a
/// single name. Used when the physician has no uploaded portrait.
String initialsFor(String name) {
  final parts = name
      .trim()
      .split(RegExp(r'\s+'))
      .where((part) => part.isNotEmpty)
      .toList();
  if (parts.isEmpty) return '?';
  if (parts.length == 1) return _firstLetter(parts.first);
  return '${_firstLetter(parts.first)}${_firstLetter(parts.last)}';
}

String formatDoctorRating(double rating) => rating.toStringAsFixed(1);

String? _blankToNull(String? value) {
  if (value == null) return null;
  final trimmed = value.trim();
  return trimmed.isEmpty ? null : trimmed;
}

double? _asDouble(Object? value) {
  if (value is num) return value.toDouble();
  return null;
}

int? _asInt(Object? value) {
  if (value is int) return value;
  if (value is num) return value.toInt();
  return null;
}

String _firstLetter(String value) {
  final iterator = value.runes.iterator;
  if (!iterator.moveNext()) return '?';
  final letter = String.fromCharCode(iterator.current);
  return letter.toUpperCase();
}

class PaginatedResponse<T> {
  const PaginatedResponse({
    required this.items,
    required this.totalCount,
    required this.page,
    required this.pageSize,
  });

  final List<T> items;
  final int totalCount;
  final int page;
  final int pageSize;

  factory PaginatedResponse.fromJson(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic> json) fromJsonT,
  ) {
    return PaginatedResponse<T>(
      items: (json['items'] as List<dynamic>)
          .map((item) => fromJsonT(item as Map<String, dynamic>))
          .toList(),
      totalCount: json['totalCount'] as int? ?? 0,
      page: json['page'] as int? ?? 1,
      pageSize: json['pageSize'] as int? ?? 10,
    );
  }
}

class Treatment {
  const Treatment({
    required this.id,
    required this.nameSinhala,
    required this.nameEnglish,
    required this.description,
    required this.scheduleDays,
    this.therapistName,
    this.photoUrl,
  });

  final String id;
  final String nameSinhala;
  final String nameEnglish;
  final String description;

  /// Day names this treatment is scheduled (e.g. "Monday", "Wednesday").
  final List<String> scheduleDays;

  final String? therapistName;
  final String? photoUrl;

  factory Treatment.fromJson(Map<String, dynamic> json) {
    return Treatment(
      id: json['id'] as String,
      nameSinhala: json['nameSinhala'] as String,
      nameEnglish: json['name'] as String,
      description: json['description'] as String,
      scheduleDays: (json['availableDays'] as List<dynamic>?)
              ?.map((e) => e as String)
              .toList() ??
          const [],
      therapistName: json['therapistName'] as String?,
      photoUrl: json['photoUrl'] as String?,
    );
  }
}

class TreatmentAvailability {
  const TreatmentAvailability({
    required this.isAvailable,
    this.message,
  });

  final bool isAvailable;
  final String? message;

  factory TreatmentAvailability.fromJson(Map<String, dynamic> json) {
    return TreatmentAvailability(
      isAvailable: json['isAvailable'] as bool? ?? false,
      message: json['message'] as String?,
    );
  }
}

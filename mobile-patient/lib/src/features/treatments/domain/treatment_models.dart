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
    this.category,
    this.durationMinutes,
    this.unitPrice,
    this.isActive = true,
  });

  final String id;
  final String nameSinhala;
  final String nameEnglish;
  final String description;

  /// Wire name of the category, for example `HerbalSteam`.
  final String? category;
  final int? durationMinutes;

  /// Price from the catalogue. The API does not send a currency.
  final double? unitPrice;
  final bool isActive;

  /// Day names this treatment is scheduled (e.g. "Monday", "Wednesday").
  final List<String> scheduleDays;

  final String? therapistName;
  final String? photoUrl;

  factory Treatment.fromJson(Map<String, dynamic> json) {
    final rawName = (json['name'] ?? json['nameEnglish'] ?? '').toString();
    final rawNameSi = (json['nameSinhala'] ?? '').toString();
    return Treatment(
      id: (json['id'] ?? '').toString(),
      nameSinhala: rawNameSi.isNotEmpty ? rawNameSi : rawName,
      nameEnglish: rawName.isNotEmpty ? rawName : rawNameSi,
      description: (json['description'] ?? '').toString(),
      scheduleDays: (json['availableDays'] as List<dynamic>?)
              ?.map((e) => e.toString())
              .toList() ??
          const [],
      therapistName: json['therapistName']?.toString(),
      photoUrl: json['photoUrl']?.toString(),
      category: _nonEmpty(json['category']),
      durationMinutes: (json['durationMinutes'] as num?)?.toInt(),
      unitPrice: (json['unitPrice'] as num?)?.toDouble(),
      isActive: json['isActive'] as bool? ?? true,
    );
  }
}

String? _nonEmpty(Object? value) {
  final text = value?.toString().trim();
  return text == null || text.isEmpty ? null : text;
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
      isAvailable:
          json['isAvailable'] as bool? ?? json['available'] as bool? ?? false,
      message: json['message'] as String? ?? json['reason'] as String?,
    );
  }
}

/// Catalogue-grounded answer from POST /agent-workflows/ask-treatment.
class TreatmentAskResult {
  const TreatmentAskResult({
    required this.answer,
    required this.matchedTreatmentIds,
    required this.refused,
    required this.workflowId,
  });

  final String answer;
  final List<String> matchedTreatmentIds;
  final bool refused;
  final String workflowId;

  factory TreatmentAskResult.fromJson(Map<String, dynamic> json) {
    final matched = json['matchedTreatmentIds'] as List<dynamic>? ?? const [];
    return TreatmentAskResult(
      answer: json['answer'] as String? ?? '',
      matchedTreatmentIds: matched.map((id) => id.toString()).toList(),
      refused: json['refused'] as bool? ?? false,
      workflowId: json['workflowId']?.toString() ?? '',
    );
  }
}

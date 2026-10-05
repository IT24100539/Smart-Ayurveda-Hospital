import 'package:flutter/foundation.dart';

enum CharakaTopic {
  treatments,
  patientInfo,
}

@immutable
class CharakaChatMessage {
  const CharakaChatMessage({
    required this.id,
    required this.text,
    required this.isUser,
    required this.timestamp,
    this.topic = CharakaTopic.treatments,
    this.refused = false,
    this.matchedTreatmentIds = const [],
    this.isError = false,
    this.errorMessage,
  });

  final String id;
  final String text;
  final bool isUser;
  final DateTime timestamp;
  final CharakaTopic topic;
  final bool refused;
  final List<String> matchedTreatmentIds;
  final bool isError;
  final String? errorMessage;

  CharakaChatMessage copyWith({
    String? id,
    String? text,
    bool? isUser,
    DateTime? timestamp,
    CharakaTopic? topic,
    bool? refused,
    List<String>? matchedTreatmentIds,
    bool? isError,
    String? errorMessage,
  }) {
    return CharakaChatMessage(
      id: id ?? this.id,
      text: text ?? this.text,
      isUser: isUser ?? this.isUser,
      timestamp: timestamp ?? this.timestamp,
      topic: topic ?? this.topic,
      refused: refused ?? this.refused,
      matchedTreatmentIds: matchedTreatmentIds ?? this.matchedTreatmentIds,
      isError: isError ?? this.isError,
      errorMessage: errorMessage ?? this.errorMessage,
    );
  }
}

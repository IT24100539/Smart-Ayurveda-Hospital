DateTime _date(Object? value) =>
    DateTime.tryParse(value as String? ?? '')?.toLocal() ??
    DateTime.fromMillisecondsSinceEpoch(0);

Map<String, dynamic> _map(Object? value) =>
    Map<String, dynamic>.from(value as Map);

List<T> _list<T>(Object? value, T Function(Map<String, dynamic>) parse) {
  if (value is! List) return <T>[];
  return value.map((item) => parse(_map(item))).toList();
}

enum ReactionType {
  like('Like'),
  dislike('Dislike');

  const ReactionType(this.wireName);
  final String wireName;
}

enum ReplyRole {
  staff('Staff'),
  patient('Patient');

  const ReplyRole(this.wireName);
  final String wireName;

  static ReplyRole fromWire(Object? value) {
    return ReplyRole.values.firstWhere(
      (role) => role.wireName == value,
      orElse: () => ReplyRole.staff,
    );
  }
}

enum ComplaintPriority {
  normal('Normal'),
  high('High');

  const ComplaintPriority(this.wireName);
  final String wireName;

  static ComplaintPriority fromWire(Object? value) {
    return ComplaintPriority.values.firstWhere(
      (priority) => priority.wireName == value,
      orElse: () => ComplaintPriority.normal,
    );
  }
}

enum ComplaintStatus {
  open('Open'),
  inProgress('InProgress'),
  escalated('Escalated'),
  resolved('Resolved');

  const ComplaintStatus(this.wireName);
  final String wireName;

  static ComplaintStatus fromWire(Object? value) {
    return ComplaintStatus.values.firstWhere(
      (status) => status.wireName == value,
      orElse: () => ComplaintStatus.open,
    );
  }
}

/// Mirrors `NotificationType` on the API.
enum NotificationKind {
  reply('FeedbackReply'),
  statusChange('ComplaintUpdate'),
  escalation('ComplaintEscalated'),
  general('General');

  const NotificationKind(this.wireName);
  final String wireName;

  static NotificationKind fromWire(Object? value) {
    return NotificationKind.values.firstWhere(
      (kind) => kind.wireName == value,
      orElse: () => NotificationKind.general,
    );
  }
}

class PublicReply {
  const PublicReply({
    required this.id,
    required this.role,
    required this.reply,
    required this.createdAt,
  });

  final String id;
  final ReplyRole role;
  final String reply;
  final DateTime createdAt;

  factory PublicReply.fromJson(Map<String, dynamic> json) {
    return PublicReply(
      id: json['id'] as String? ?? '',
      role: ReplyRole.fromWire(json['userRole']),
      reply: json['reply'] as String? ?? '',
      createdAt: _date(json['createdAt']),
    );
  }
}

class PublicFeedback {
  const PublicFeedback({
    required this.id,
    required this.patientName,
    required this.isAnonymous,
    required this.rating,
    required this.comment,
    required this.likeCount,
    required this.dislikeCount,
    required this.replies,
    required this.createdAt,
  });

  final String id;
  final String patientName;
  final bool isAnonymous;
  final int rating;
  final String comment;
  final int likeCount;
  final int dislikeCount;
  final List<PublicReply> replies;
  final DateTime createdAt;

  factory PublicFeedback.fromJson(Map<String, dynamic> json) {
    return PublicFeedback(
      id: json['id'] as String? ?? '',
      patientName: json['patientName'] as String? ?? '',
      isAnonymous: json['isAnonymous'] as bool? ?? false,
      rating: json['rating'] as int? ?? 0,
      comment: json['comment'] as String? ?? '',
      likeCount: json['likeCount'] as int? ?? 0,
      dislikeCount: json['dislikeCount'] as int? ?? 0,
      replies: _list(json['replies'], PublicReply.fromJson),
      createdAt: _date(json['createdAt']),
    );
  }
}

class PatientComplaint {
  const PatientComplaint({
    required this.id,
    required this.subject,
    required this.description,
    required this.priority,
    required this.status,
    required this.createdAt,
    this.escalatedAt,
  });

  final String id;
  final String subject;
  final String description;
  final ComplaintPriority priority;
  final ComplaintStatus status;
  final DateTime createdAt;
  final DateTime? escalatedAt;

  factory PatientComplaint.fromJson(Map<String, dynamic> json) {
    final escalated = json['escalatedAt'] as String?;
    return PatientComplaint(
      id: json['id'] as String? ?? '',
      subject: json['subject'] as String? ?? '',
      description: json['description'] as String? ?? '',
      priority: ComplaintPriority.fromWire(json['priority']),
      status: ComplaintStatus.fromWire(json['status']),
      createdAt: _date(json['createdAt']),
      escalatedAt: escalated == null || escalated.isEmpty
          ? null
          : _date(escalated),
    );
  }
}

class PatientFeedback {
  const PatientFeedback({
    required this.id,
    required this.rating,
    required this.comment,
    required this.isAnonymous,
    required this.status,
    required this.createdAt,
    required this.canEdit,
    this.appointmentId,
    this.treatmentId,
  });

  final String id;
  final int rating;
  final String comment;
  final bool isAnonymous;
  final String status;
  final DateTime createdAt;
  final bool canEdit;
  final String? appointmentId;
  final String? treatmentId;

  factory PatientFeedback.fromJson(Map<String, dynamic> json) {
    return PatientFeedback(
      id: json['id'] as String? ?? '',
      rating: json['rating'] as int? ?? 0,
      comment: json['comment'] as String? ?? '',
      isAnonymous: json['isAnonymous'] as bool? ?? false,
      status: json['status'] as String? ?? '',
      createdAt: _date(json['createdAt']),
      canEdit: json['canEdit'] as bool? ?? false,
      appointmentId: json['appointmentId'] as String?,
      treatmentId: json['treatmentId'] as String?,
    );
  }
}

class PatientNotification {
  const PatientNotification({
    required this.id,
    required this.title,
    required this.message,
    required this.kind,
    required this.isRead,
    required this.createdAt,
  });

  final String id;
  final String title;
  final String message;
  final NotificationKind kind;
  final bool isRead;
  final DateTime createdAt;

  PatientNotification copyWith({bool? isRead}) {
    return PatientNotification(
      id: id,
      title: title,
      message: message,
      kind: kind,
      isRead: isRead ?? this.isRead,
      createdAt: createdAt,
    );
  }

  factory PatientNotification.fromJson(Map<String, dynamic> json) {
    return PatientNotification(
      id: json['id'] as String? ?? '',
      title: json['title'] as String? ?? '',
      message: json['message'] as String? ?? '',
      kind: NotificationKind.fromWire(json['type']),
      isRead: json['isRead'] as bool? ?? false,
      createdAt: _date(json['createdAt']),
    );
  }
}

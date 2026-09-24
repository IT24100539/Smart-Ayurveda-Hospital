import 'package:flutter/widgets.dart';

/// Keys shared by the feedback screens and their widget tests.
abstract final class FeedbackKeys {
  static Key star(int value) => Key('feedback_star_$value');

  static const anonymousToggle = Key('feedback_anonymous_toggle');
  static const namePreview = Key('feedback_name_preview');
  static const unreadBadge = Key('notification_unread_badge');
  static const comment = Key('feedback_comment');
  static const submit = Key('feedback_submit');
  static const leaveFeedback = Key('leave_feedback_button');
}

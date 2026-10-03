import 'package:flutter/widgets.dart';

/// Keys shared by the feedback screens and their widget tests.
abstract final class FeedbackKeys {
  static Key star(int value) => Key('feedback_star_$value');

  static const anonymousToggle = Key('feedback_anonymous_toggle');
  static const namePreview = Key('feedback_name_preview');
  static const unreadBadge = Key('notification_unread_badge');
  static const homeUnreadBadge = Key('home_notification_unread_badge');
  static const markAllRead = Key('notification_mark_all_read');
  static Key notificationTile(String id) => Key('notification_tile_$id');
  static const comment = Key('feedback_comment');
  static const submit = Key('feedback_submit');
  static const leaveFeedback = Key('leave_feedback_button');
  static const writeFeedback = Key('write_feedback_button');
  static const linkVisit = Key('feedback_link_visit');
  static const linkTreatment = Key('feedback_link_treatment');
  static const hubCommunity = Key('feedback_hub_community');
  static const hubMine = Key('feedback_hub_mine');
  static const hubComplaints = Key('feedback_hub_complaints');
  static const hubNotifications = Key('feedback_hub_notifications');
  static const feedSkeleton = Key('feedback_feed_skeleton');
  static const feedRetry = Key('feedback_feed_retry');
  static const mineSkeleton = Key('feedback_mine_skeleton');
  static const mineRetry = Key('feedback_mine_retry');
  static const complaintsSkeleton = Key('complaints_skeleton');
  static const complaintsRetry = Key('complaints_retry');
  static const notificationsSkeleton = Key('notifications_skeleton');
  static const notificationsRetry = Key('notifications_retry');
}

abstract final class AppRoutes {
  static const splash = '/';
  static const login = '/login';
  static const home = '/home';
  static const treatments = '/treatments';
  static const appointments = '/appointments';
static const wards = '/wards';
static const bookAppointment = '/treatments/:treatmentId/book';
static const billing = '/billing';
static const feedback = '/feedback';
static const submitFeedback = '/feedback/submit';
static const myFeedback = '/feedback/mine';
static const complaints = '/feedback/complaints';
static const submitComplaint = '/feedback/complaints/new';
static const notifications = '/feedback/notifications';
  static const profile = '/profile';

  static String bookingFor(String treatmentId) =>
      '/treatments/$treatmentId/book';

  /// Routes reachable without a token.
  static const public = {splash, login};
}

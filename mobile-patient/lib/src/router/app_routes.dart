abstract final class AppRoutes {
  static const splash = '/';
  static const login = '/login';
  static const forgotPassword = '/forgot-password';
  static const resetPassword = '/reset-password';
  static const home = '/home';
  static const treatments = '/treatments';
  static const treatmentDetail = '/treatments/:id';
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
  static const healthHub = '/profile/health-hub';
  static const charakaChat = '/charaka-chat';
  static const onboarding = '/onboarding';
  static const faq = '/faq';
  static const contact = '/contact';

  static String bookingFor(String treatmentId) =>
      '/treatments/$treatmentId/book';

  static String treatmentById(String id) => '/treatments/$id';

  /// Catalogue browsing, onboarding, FAQs, and contact info are public.
  static bool isPublic(String location) {
    if (location == splash ||
        location == login ||
        location == forgotPassword ||
        location == resetPassword ||
        location == onboarding ||
        location == faq ||
        location == contact ||
        location == treatments) {
      return true;
    }
    final treatmentDetail = RegExp(r'^/treatments/[^/]+$');
    return treatmentDetail.hasMatch(location);
  }

  static String loginWithReturn(String returnPath) {
    final sanitized = sanitizeReturnPath(returnPath);
    if (sanitized == null) return login;
    return Uri(path: login, queryParameters: {'returnPath': sanitized}).toString();
  }

  static String? sanitizeReturnPath(String? path) {
    if (path == null || path.isEmpty) return null;
    if (path == splash ||
        path == login ||
        path == onboarding ||
        path.startsWith('$login?')) {
      return null;
    }
    return path;
  }
}

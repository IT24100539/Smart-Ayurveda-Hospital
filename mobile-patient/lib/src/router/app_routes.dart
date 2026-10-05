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

  /// Booking that starts by choosing a therapy from the catalogue.
  static const chooseTherapyToBook = '/book-appointment';
  static const feedback = '/feedback';
  static const submitFeedback = '/feedback/submit';
  static const myFeedback = '/feedback/mine';
  static const complaints = '/feedback/complaints';
  static const submitComplaint = '/feedback/complaints/new';
  static const notifications = '/feedback/notifications';
  static const profile = '/profile';
  static const healthHub = '/profile/health-hub';
  static const privacyPolicy = '/profile/privacy';
  static const termsOfUse = '/profile/terms';

  /// Opens one Health Hub tab: care, prescriptions, invoices, or documents.
  static String healthHubTab(String tab) => '$healthHub?tab=$tab';
  static const charakaChat = '/charaka-chat';
  static const onboarding = '/onboarding';
  static const faq = '/faq';
  static const contact = '/contact';
  static const doctors = '/doctors';

  /// Debug-only component catalogue. Not registered in release builds.
  static const gallery = '/dev/gallery';

  static String bookingFor(String treatmentId) =>
      '/treatments/$treatmentId/book';

  static String treatmentById(String id) => '/treatments/$id';

  static String doctorById(String id) => '/doctors/${Uri.encodeComponent(id)}';

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
    return Uri(
      path: login,
      queryParameters: {'returnPath': sanitized},
    ).toString();
  }

  static String? sanitizeReturnPath(String? path) {
    if (path == null || path.isEmpty) return null;
    if (!path.startsWith('/') || path.startsWith('//')) return null;
    final uri = Uri.tryParse(path);
    if (uri == null || uri.hasScheme || uri.host.isNotEmpty) return null;
    final location = uri.path;
    if (location.isEmpty ||
        location == splash ||
        location == login ||
        location == onboarding ||
        location == forgotPassword ||
        location == resetPassword) {
      return null;
    }
    return path;
  }
}

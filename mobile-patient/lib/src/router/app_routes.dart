abstract final class AppRoutes {
  static const splash = '/';
  static const login = '/login';
  static const home = '/home';
  static const treatments = '/treatments';
  static const appointments = '/appointments';
  static const wards = '/wards';
  static const bookAppointment = '/treatments/:treatmentId/book';
  static const billing = '/billing';
  static const profile = '/profile';

  static String bookingFor(String treatmentId) =>
      '/treatments/$treatmentId/book';

  /// Routes reachable without a token.
  static const public = {splash, login};
}

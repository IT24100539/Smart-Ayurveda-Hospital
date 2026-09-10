abstract final class AppRoutes {
  static const splash = '/';
  static const login = '/login';
  static const home = '/home';
  static const treatments = '/treatments';
  static const appointments = '/appointments';
  static const billing = '/billing';
  static const profile = '/profile';

  /// Routes reachable without a token.
  static const public = {splash, login};
}

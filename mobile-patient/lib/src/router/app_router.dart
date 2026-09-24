import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../features/appointments/presentation/appointments_screen.dart';
import '../features/appointments/presentation/book_appointment_flow.dart';
import '../features/appointments/domain/appointment_models.dart';
import '../features/auth/application/auth_controller.dart';
import '../features/auth/presentation/login_screen.dart';
import '../features/auth/presentation/splash_screen.dart';
import '../features/billing/presentation/billing_screen.dart';
import '../features/home/presentation/home_screen.dart';
import '../features/profile/presentation/profile_screen.dart';
import '../features/shell/presentation/home_shell.dart';
import '../features/treatments/presentation/treatments_screen.dart';
import '../features/wards/presentation/ward_availability_screen.dart';
import 'app_routes.dart';

final routerProvider = Provider<GoRouter>((ref) {
  // Bridges Riverpod's auth state onto the Listenable that GoRouter refreshes
  // from, so a sign-in, sign-out or 401 re-runs the redirect below.
  final authListenable = ValueNotifier<AuthStatus>(AuthStatus.unknown);
  ref.listen<AuthState>(
    authControllerProvider,
    (_, next) => authListenable.value = next.status,
    fireImmediately: true,
  );
  ref.onDispose(authListenable.dispose);

  return GoRouter(
    initialLocation: AppRoutes.splash,
    refreshListenable: authListenable,
    redirect: (context, state) {
      final authState = ref.read(authControllerProvider);
      final location = state.matchedLocation;

      // The splash screen is what performs the session restore, so it is never
      // redirected away from; it routes onwards itself.
      if (location == AppRoutes.splash) return null;

      // Hold off until the stored token has been read, otherwise a returning
      // patient briefly lands on the login screen.
      if (!authState.isResolved) return AppRoutes.splash;

      final isPublic = AppRoutes.public.contains(location);
      if (!authState.isAuthenticated && !isPublic) return AppRoutes.login;
      if (authState.isAuthenticated && location == AppRoutes.login) {
        return AppRoutes.home;
      }
      return null;
    },
    routes: [
      GoRoute(
        path: AppRoutes.splash,
        builder: (context, state) => const SplashScreen(),
      ),
      GoRoute(
        path: AppRoutes.login,
        builder: (context, state) => const LoginScreen(),
      ),
      GoRoute(
        path: AppRoutes.bookAppointment,
        builder: (context, state) {
          final extra = state.extra;
          final treatment = extra is TreatmentBooking
              ? extra
              : extra is Map
              ? TreatmentBooking.fromRouteMap(extra)
              : TreatmentBooking(
                  id: state.pathParameters['treatmentId']!,
                  name: state.uri.queryParameters['name'] ?? 'Treatment',
                );
          return BookAppointmentFlow(treatment: treatment);
        },
      ),
      GoRoute(
        path: AppRoutes.wards,
        builder: (context, state) => const WardAvailabilityScreen(),
      ),
      StatefulShellRoute.indexedStack(
        builder: (context, state, navigationShell) =>
            HomeShell(navigationShell: navigationShell),
        branches: [
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.home,
                builder: (context, state) => const HomeScreen(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.treatments,
                builder: (context, state) => const TreatmentsScreen(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.appointments,
                builder: (context, state) => const MyAppointmentsScreen(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.billing,
                builder: (context, state) => const BillingScreen(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.profile,
                builder: (context, state) => const ProfileScreen(),
              ),
            ],
          ),
        ],
      ),
    ],
  );
});

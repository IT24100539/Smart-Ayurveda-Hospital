import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../features/appointments/presentation/appointments_screen.dart';
import '../features/appointments/presentation/book_appointment_flow.dart';
import '../features/appointments/domain/appointment_models.dart';
import '../features/auth/application/auth_controller.dart';
import '../features/auth/presentation/login_screen.dart';
import '../features/auth/presentation/splash_screen.dart';
import '../features/feedback/presentation/my_complaints_screen.dart';
import '../features/feedback/presentation/my_feedback_screen.dart';
import '../features/feedback/presentation/notifications_screen.dart';
import '../features/feedback/presentation/public_feedback_feed_screen.dart';
import '../features/feedback/presentation/submit_complaint_screen.dart';
import '../features/feedback/presentation/submit_feedback_screen.dart';
import '../features/home/presentation/home_screen.dart';
import '../features/profile/presentation/profile_screen.dart';
import '../features/shell/presentation/home_shell.dart';
import '../features/treatments/presentation/treatment_detail_screen.dart';
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

      final isPublic = AppRoutes.public.contains(location) || location.startsWith('/treatments');
      if (!authState.isAuthenticated && !isPublic) return AppRoutes.login;
      if (authState.isAuthenticated && location == AppRoutes.login) {
        final returnPath = state.uri.queryParameters['returnPath'];
        return returnPath ?? AppRoutes.home;
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
                routes: [
                  GoRoute(
                    path: ':id',
                    builder: (context, state) {
                      final id = state.pathParameters['id']!;
                      // We will need to import TreatmentDetailScreen
                      // Return the placeholder for now until we create it
                      return TreatmentDetailScreen(treatmentId: id);
                    },
                  ),
                ],
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
                path: AppRoutes.feedback,
                builder: (context, state) => const PublicFeedbackFeedScreen(),
                routes: [
                  GoRoute(
                    path: 'mine',
                    builder: (context, state) => const MyFeedbackScreen(),
                  ),
                  GoRoute(
                    path: 'submit',
                    builder: (context, state) => SubmitFeedbackScreen(
                      appointmentId: state.uri.queryParameters['appointmentId'],
                      treatmentId: state.uri.queryParameters['treatmentId'],
                    ),
                  ),
                  GoRoute(
                    path: 'complaints',
                    builder: (context, state) => const MyComplaintsScreen(),
                    routes: [
                      GoRoute(
                        path: 'new',
                        builder: (context, state) =>
                            const SubmitComplaintScreen(),
                      ),
                    ],
                  ),
                  GoRoute(
                    path: 'notifications',
                    builder: (context, state) => const NotificationsScreen(),
                  ),
                ],
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

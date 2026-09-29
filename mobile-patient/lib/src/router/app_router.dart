import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../features/appointments/presentation/appointments_screen.dart';
import '../features/appointments/presentation/book_appointment_flow.dart';
import '../features/appointments/domain/appointment_models.dart';
import '../features/auth/application/auth_controller.dart';
import '../features/auth/presentation/login_screen.dart';
import '../features/auth/presentation/splash_screen.dart';
import '../features/feedback/presentation/feedback_hub_screen.dart';
import '../features/feedback/presentation/my_complaints_screen.dart';
import '../features/feedback/presentation/my_feedback_screen.dart';
import '../features/feedback/presentation/notifications_screen.dart';
import '../features/feedback/presentation/submit_complaint_screen.dart';
import '../features/feedback/presentation/submit_feedback_screen.dart';
import '../features/home/presentation/home_screen.dart';
import '../features/profile/presentation/profile_screen.dart';
import '../features/shell/presentation/home_shell.dart';
import '../features/treatments/presentation/treatment_detail_screen.dart';
import '../features/treatments/presentation/treatments_screen.dart';
import '../features/wards/presentation/ward_availability_screen.dart';
import 'app_routes.dart';

/// One [GoRouter] for the process. Auth changes notify [refreshListenable]
/// instead of rebuilding this provider, which would reset navigation.
final routerProvider = Provider<GoRouter>((ref) {
  final authListenable = ValueNotifier<AuthStatus>(AuthStatus.loading);
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
      final fullPath = '${state.uri.path}${state.uri.hasQuery ? '?${state.uri.query}' : ''}';

      if (!authState.isResolved) {
        if (location == AppRoutes.splash) return null;
        return Uri(
          path: AppRoutes.splash,
          queryParameters: {'from': fullPath},
        ).toString();
      }

      final pendingFrom = AppRoutes.sanitizeReturnPath(
        state.uri.queryParameters['from'] ?? state.uri.queryParameters['returnPath'],
      );

      if (authState.isAuthenticated) {
        if (location == AppRoutes.login || location == AppRoutes.splash) {
          return pendingFrom ?? AppRoutes.home;
        }
        return null;
      }

      if (location == AppRoutes.splash) {
        return pendingFrom == null
            ? AppRoutes.login
            : AppRoutes.loginWithReturn(pendingFrom);
      }
      if (AppRoutes.isPublic(location)) return null;
      return AppRoutes.loginWithReturn(fullPath);
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
                builder: (context, state) => FeedbackHubScreen(
                  section: feedbackSectionIndex(
                    state.uri.queryParameters['section'],
                  ),
                ),
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

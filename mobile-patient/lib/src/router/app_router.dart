import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../features/appointments/presentation/appointments_screen.dart';
import '../features/appointments/presentation/book_appointment_flow.dart';
import '../features/appointments/domain/appointment_models.dart';
import '../features/auth/application/auth_controller.dart';
import '../features/auth/presentation/login_screen.dart';
import '../features/auth/presentation/splash_screen.dart';
import '../features/charaka_chat/presentation/charaka_chat_screen.dart';
import '../features/contact/presentation/contact_location_screen.dart';
import '../features/faq/presentation/faq_screen.dart';
import '../features/feedback/presentation/feedback_hub_screen.dart';
import '../features/feedback/presentation/my_complaints_screen.dart';
import '../features/feedback/presentation/my_feedback_screen.dart';
import '../features/feedback/presentation/notifications_screen.dart';
import '../features/feedback/presentation/submit_complaint_screen.dart';
import '../features/feedback/presentation/submit_feedback_screen.dart';
import '../features/health_hub/presentation/health_hub_screen.dart';
import '../features/home/presentation/home_screen.dart';
import '../features/onboarding/application/onboarding_controller.dart';
import '../features/onboarding/presentation/onboarding_screen.dart';
import '../features/profile/presentation/profile_screen.dart';
import '../features/shell/presentation/home_shell.dart';
import '../features/treatments/presentation/treatment_detail_screen.dart';
import '../features/treatments/presentation/treatments_screen.dart';
import '../features/wards/presentation/ward_availability_screen.dart';
import 'app_routes.dart';

/// One [GoRouter] for the process. Auth & onboarding changes notify [refreshListenable]
/// instead of rebuilding this provider, which would reset navigation.
final routerProvider = Provider<GoRouter>((ref) {
  final routerListenable = ValueNotifier<int>(0);
  ref.listen<AuthState>(
    authControllerProvider,
    (previous, next) {
      if (previous?.status != next.status) {
        routerListenable.value++;
      }
    },
  );
  ref.listen<OnboardingState>(
    onboardingControllerProvider,
    (previous, next) {
      if (previous?.hasSeenOnboarding != next.hasSeenOnboarding ||
          previous?.isResolved != next.isResolved) {
        routerListenable.value++;
      }
    },
  );
  ref.onDispose(routerListenable.dispose);

  return GoRouter(
    initialLocation: AppRoutes.splash,
    refreshListenable: routerListenable,
    redirect: (context, state) {
      final authState = ref.read(authControllerProvider);
      final onboardingState = ref.read(onboardingControllerProvider);
      final location = state.matchedLocation;
      final fullPath = '${state.uri.path}${state.uri.hasQuery ? '?${state.uri.query}' : ''}';

      if (!authState.isResolved || !onboardingState.isResolved) {
        if (location == AppRoutes.splash) return null;
        return Uri(
          path: AppRoutes.splash,
          queryParameters: {'from': fullPath},
        ).toString();
      }

      // First-launch onboarding check
      if (!onboardingState.hasSeenOnboarding) {
        if (location == AppRoutes.onboarding) return null;
        return AppRoutes.onboarding;
      }
      if (location == AppRoutes.onboarding) {
        return authState.isAuthenticated ? AppRoutes.home : AppRoutes.login;
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
        path: AppRoutes.onboarding,
        builder: (context, state) => const OnboardingScreen(),
      ),
      GoRoute(
        path: AppRoutes.login,
        builder: (context, state) => const LoginScreen(),
      ),
      GoRoute(
        path: AppRoutes.faq,
        builder: (context, state) => const FaqScreen(),
      ),
      GoRoute(
        path: AppRoutes.contact,
        builder: (context, state) => const ContactLocationScreen(),
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
      GoRoute(
        path: AppRoutes.charakaChat,
        builder: (context, state) => const CharakaChatScreen(),
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
                routes: [
                  GoRoute(
                    path: ':appointmentId/reschedule',
                    builder: (context, state) {
                      final appointmentId = state.pathParameters['appointmentId']!;
                      final treatmentId = state.uri.queryParameters['treatmentId'] ?? '';
                      final treatmentName = state.uri.queryParameters['name'] ?? 'Treatment';
                      return BookAppointmentFlow(
                        treatment: TreatmentBooking(
                          id: treatmentId,
                          name: treatmentName,
                        ),
                        appointmentIdToReschedule: appointmentId,
                      );
                    },
                  ),
                ],
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
                routes: [
                  GoRoute(
                    path: 'health-hub',
                    builder: (context, state) => const HealthHubScreen(),
                  ),
                ],
              ),
            ],
          ),
        ],
      ),
    ],
  );
});

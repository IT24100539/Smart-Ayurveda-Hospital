import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../l10n/feature_localizations.dart';
import '../../../l10n/language_switcher.dart';
import '../../../router/app_routes.dart';
import '../../../theme/app_theme.dart';
import '../../auth/application/auth_controller.dart';
import '../application/onboarding_controller.dart';

abstract final class OnboardingKeys {
  static const skipButton = ValueKey('onboarding-skip-button');
  static const nextButton = ValueKey('onboarding-next-button');
  static const getStartedButton = ValueKey('onboarding-get-started-button');
  static const pageView = ValueKey('onboarding-page-view');
  static ValueKey<String> page(int index) => ValueKey('onboarding-page-$index');
}

class OnboardingScreen extends ConsumerStatefulWidget {
  const OnboardingScreen({super.key});

  @override
  ConsumerState<OnboardingScreen> createState() => _OnboardingScreenState();
}

class _OnboardingScreenState extends ConsumerState<OnboardingScreen> {
  final PageController _pageController = PageController();
  int _currentPage = 0;

  @override
  void dispose() {
    _pageController.dispose();
    super.dispose();
  }

  Future<void> _finishOnboarding() async {
    await ref.read(onboardingControllerProvider.notifier).completeOnboarding();
    if (!mounted) return;
    final authState = ref.read(authControllerProvider);
    if (authState.isAuthenticated) {
      context.go(AppRoutes.home);
    } else {
      context.go(AppRoutes.login);
    }
  }

  void _nextPage() {
    if (_currentPage < 2) {
      _pageController.nextPage(
        duration: const Duration(milliseconds: 350),
        curve: Curves.easeInOut,
      );
    } else {
      _finishOnboarding();
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final copy = FeatureLocalizations.of(context);

    final slides = [
      _OnboardingSlideData(
        title: copy.text(
          'Authentic Ayurvedic Care',
          'ප්‍රමිතිගත ආයුර්වේද සත්කාර',
        ),
        subtitle: copy.text(
          'Experience time-tested holistic healing with personalized Panchakarma and natural herbal therapies guided by experienced practitioners.',
          'පළපුරුදු වෛද්‍යවරුන්ගේ මගපෙන්වීම යටතේ ඔබේ ප්‍රකෘතියට ගැළපෙන පංචකර්ම සහ ස්වාභාවික ඔසු සත්කාර ලබාගන්න.',
        ),
        icon: Icons.spa_rounded,
        imageAsset: 'assets/images/hero-ayurveda-wellness.png',
        badge: copy.text('Holistic Health', 'පූර්ණ සුවතාව'),
      ),
      _OnboardingSlideData(
        title: copy.text(
          'Appointments & Health Hub',
          'වෛද්‍ය හමුවීම් සහ සෞඛ්‍ය කේන්ද්‍රය',
        ),
        subtitle: copy.text(
          'Book therapy sessions with real-time slot selection, easily reschedule or cancel appointments, and monitor your personal treatment history.',
          'වේලාවන් තෝරාගෙන හමුවීම් වෙන්කරවා ගන්න, පහසුවෙන් වෙනස් කරන්න, සහ ඔබගේ ප්‍රතිකාර ඉතිහාසය පරීක්ෂා කරන්න.',
        ),
        icon: Icons.calendar_month_rounded,
        imageAsset: 'assets/images/therapy-panchakarma.png',
        badge: copy.text('Flexible Scheduling', 'පහසු කාලසටහන්'),
      ),
      _OnboardingSlideData(
        title: copy.text(
          'Charaka Hospital Assistant',
          'චරක රෝහල් සහායක',
        ),
        subtitle: copy.text(
          'Get verified answers about hospital therapies, session fees, clinic schedules, and your patient identification summary anytime.',
          'රෝහල් ප්‍රතිකාර, සැසි ගාස්තු, සායන කාලසටහන් සහ ඔබගේ රෝගී වාර්තා සාරාංශය පිළිබඳ තොරතුරු ක්ෂණිකව ලබාගන්න.',
        ),
        icon: Icons.support_agent_rounded,
        imageAsset: 'assets/images/herbal-garden.png',
        badge: copy.text('Instant Guidance', 'ක්ෂණික මගපෙන්වීම'),
      ),
    ];

    return Scaffold(
      backgroundColor: theme.colorScheme.surface,
      body: SafeArea(
        child: Column(
          children: [
            // Top navigation row: Language selector & Skip button
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const LanguageSwitcher(),
                  if (_currentPage < 2)
                    TextButton(
                      key: OnboardingKeys.skipButton,
                      onPressed: _finishOnboarding,
                      child: Text(
                        copy.text('Skip', 'මගහරින්න'),
                        style: TextStyle(
                          color: theme.colorScheme.onSurfaceVariant,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    )
                  else
                    const SizedBox(width: 48),
                ],
              ),
            ),

            // Carousel Pages
            Expanded(
              child: PageView.builder(
                key: OnboardingKeys.pageView,
                controller: _pageController,
                onPageChanged: (index) => setState(() => _currentPage = index),
                itemCount: slides.length,
                itemBuilder: (context, index) {
                  final slide = slides[index];
                  return Padding(
                    key: OnboardingKeys.page(index),
                    padding: const EdgeInsets.symmetric(horizontal: 24),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        // Image or Graphic Container
                        Container(
                          height: 240,
                          width: double.infinity,
                          decoration: BoxDecoration(
                            borderRadius: BorderRadius.circular(24),
                            color: AyurvedaColors.sageMuted,
                            boxShadow: [
                              BoxShadow(
                                color: AyurvedaColors.forest.withValues(alpha: 0.08),
                                blurRadius: 20,
                                offset: const Offset(0, 10),
                              ),
                            ],
                          ),
                          clipBehavior: Clip.antiAlias,
                          child: Stack(
                            children: [
                              Positioned.fill(
                                child: Container(
                                  decoration: BoxDecoration(
                                    gradient: LinearGradient(
                                      begin: Alignment.topLeft,
                                      end: Alignment.bottomRight,
                                      colors: [
                                        AyurvedaColors.sageMuted,
                                        AyurvedaColors.forest.withValues(alpha: 0.15),
                                      ],
                                    ),
                                  ),
                                  child: Center(
                                    child: Icon(
                                      slide.icon,
                                      size: 72,
                                      color: AyurvedaColors.forest,
                                    ),
                                  ),
                                ),
                              ),
                              Positioned(
                                top: 14,
                                left: 14,
                                child: Container(
                                  padding: const EdgeInsets.symmetric(
                                    horizontal: 12,
                                    vertical: 6,
                                  ),
                                  decoration: BoxDecoration(
                                    color: Colors.white.withValues(alpha: 0.92),
                                    borderRadius: BorderRadius.circular(20),
                                  ),
                                  child: Row(
                                    mainAxisSize: MainAxisSize.min,
                                    children: [
                                      Icon(
                                        slide.icon,
                                        size: 16,
                                        color: AyurvedaColors.forest,
                                      ),
                                      const SizedBox(width: 6),
                                      Text(
                                        slide.badge,
                                        style: const TextStyle(
                                          fontSize: 12,
                                          fontWeight: FontWeight.bold,
                                          color: AyurvedaColors.forest,
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(height: 32),

                        // Title
                        Text(
                          slide.title,
                          textAlign: TextAlign.center,
                          style: theme.textTheme.headlineSmall?.copyWith(
                            fontWeight: FontWeight.bold,
                            color: AyurvedaColors.forest,
                          ),
                        ),
                        const SizedBox(height: 14),

                        // Subtitle
                        Text(
                          slide.subtitle,
                          textAlign: TextAlign.center,
                          style: theme.textTheme.bodyMedium?.copyWith(
                            color: theme.colorScheme.onSurfaceVariant,
                            height: 1.5,
                          ),
                        ),
                      ],
                    ),
                  );
                },
              ),
            ),

            // Bottom controls: Indicators and Action Button
            Padding(
              padding: const EdgeInsets.fromLTRB(24, 16, 24, 28),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  // Indicators
                  Row(
                    children: List.generate(
                      slides.length,
                      (index) => AnimatedContainer(
                        duration: const Duration(milliseconds: 250),
                        margin: const EdgeInsets.only(right: 6),
                        height: 8,
                        width: _currentPage == index ? 24 : 8,
                        decoration: BoxDecoration(
                          color: _currentPage == index
                              ? AyurvedaColors.forest
                              : AyurvedaColors.sageMuted,
                          borderRadius: BorderRadius.circular(4),
                        ),
                      ),
                    ),
                  ),

                  // Action Button
                  if (_currentPage < 2)
                    FilledButton(
                      key: OnboardingKeys.nextButton,
                      onPressed: _nextPage,
                      style: FilledButton.styleFrom(
                        backgroundColor: AyurvedaColors.forest,
                        minimumSize: const Size(120, 48),
                        padding: const EdgeInsets.symmetric(
                          horizontal: 24,
                          vertical: 14,
                        ),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(14),
                        ),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Text(copy.text('Next', 'මීළඟ')),
                          const SizedBox(width: 8),
                          const Icon(Icons.arrow_forward_rounded, size: 18),
                        ],
                      ),
                    )
                  else
                    FilledButton(
                      key: OnboardingKeys.getStartedButton,
                      onPressed: _finishOnboarding,
                      style: FilledButton.styleFrom(
                        backgroundColor: AyurvedaColors.forest,
                        minimumSize: const Size(140, 48),
                        padding: const EdgeInsets.symmetric(
                          horizontal: 28,
                          vertical: 14,
                        ),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(14),
                        ),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Text(copy.text('Get Started', 'ආරම්භ කරන්න')),
                          const SizedBox(width: 8),
                          const Icon(Icons.check_circle_outline, size: 18),
                        ],
                      ),
                    ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _OnboardingSlideData {
  const _OnboardingSlideData({
    required this.title,
    required this.subtitle,
    required this.icon,
    required this.imageAsset,
    required this.badge,
  });

  final String title;
  final String subtitle;
  final IconData icon;
  final String imageAsset;
  final String badge;
}

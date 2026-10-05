import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../l10n/feature_localizations.dart';
import '../../../l10n/language_switcher.dart';
import '../../../router/app_routes.dart';
import '../../../shared/widgets/clinic_widgets.dart';
import '../../../shared/widgets/page_layout.dart';
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
          'Ayurvedic Care',
          'ආයුර්වේද සත්කාර',
        ),
        subtitle: copy.text(
          'Browse panchakarma and herbal therapies in the hospital catalogue. Staff review each appointment request before it is confirmed.',
          'රෝහල් ලැයිස්තුවෙන් පංචකර්ම සහ ඔසු ප්‍රතිකාර බලන්න. හමුවීම් ඉල්ලීම් කාර්ය මණ්ඩලය අනුමත කරන තුරු තහවුරු නොවේ.',
        ),
        icon: Icons.spa_rounded,
        imageAsset: 'assets/images/hero-ayurveda-wellness.png',
        badge: copy.text('Therapies', 'ප්‍රතිකාර'),
      ),
      _OnboardingSlideData(
        title: copy.text(
          'Appointments & Health Hub',
          'වෛද්‍ය හමුවීම් සහ සෞඛ්‍ය කේන්ද්‍රය',
        ),
        subtitle: copy.text(
          'Book therapy sessions, choose an available slot, reschedule or cancel, and review your visit history.',
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
          'Ask about hospital therapies, session fees, clinic schedules, and your patient identification summary.',
          'රෝහල් ප්‍රතිකාර, සැසි ගාස්තු, සායන කාලසටහන් සහ ඔබගේ රෝගී වාර්තා සාරාංශය පිළිබඳ තොරතුරු අසන්න.',
        ),
        icon: Icons.support_agent_rounded,
        imageAsset: 'assets/images/herbal-garden.png',
        badge: copy.text('Hospital assistant', 'රෝහල් සහායක'),
      ),
    ];

    final brand = AyurvedaThemeExtension.of(context);
    final lastPage = slides.length - 1;

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
                  if (_currentPage < lastPage)
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
                itemBuilder: (context, index) => _OnboardingSlide(
                  key: OnboardingKeys.page(index),
                  slide: slides[index],
                ),
              ),
            ),

            // Bottom controls: Indicators and Action Button
            CenteredContent(
              maxWidth: 720,
              child: Padding(
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
                                ? brand.teal
                                : theme.colorScheme.outlineVariant,
                            borderRadius: BorderRadius.circular(4),
                          ),
                        ),
                      ),
                    ),

                    // Action Button
                    if (_currentPage < lastPage)
                      FilledButton(
                        key: OnboardingKeys.nextButton,
                        onPressed: _nextPage,
                        style: FilledButton.styleFrom(
                          minimumSize: const Size(120, 48),
                          padding: const EdgeInsets.symmetric(
                            horizontal: 24,
                            vertical: 14,
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
                          minimumSize: const Size(140, 48),
                          padding: const EdgeInsets.symmetric(
                            horizontal: 28,
                            vertical: 14,
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
            ),
          ],
        ),
      ),
    );
  }
}

/// One slide. Phones stack photo over text. Tablets, web and landscape put them side by side.
/// The content scrolls, so short screens and large text sizes do not overflow.
class _OnboardingSlide extends StatelessWidget {
  const _OnboardingSlide({required this.slide, super.key});

  final _OnboardingSlideData slide;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final brand = AyurvedaThemeExtension.of(context);

    final photo = Container(
      height: 240,
      width: double.infinity,
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(brand.headerRadius),
        color: theme.colorScheme.primaryContainer,
        border: Border.all(color: brand.cardBorderColor),
      ),
      clipBehavior: Clip.antiAlias,
      child: Stack(
        children: [
          Positioned.fill(
            child: Image.asset(
              slide.imageAsset,
              fit: BoxFit.cover,
              errorBuilder: (context, error, stackTrace) => Center(
                child: Icon(slide.icon, size: 72, color: brand.teal),
              ),
            ),
          ),
          Positioned(
            top: 14,
            left: 14,
            child: PillChip(
              icon: slide.icon,
              label: slide.badge,
              background: theme.colorScheme.surface,
              foreground: brand.teal,
            ),
          ),
        ],
      ),
    );

    Widget text(TextAlign align) => Column(
      crossAxisAlignment: align == TextAlign.center
          ? CrossAxisAlignment.center
          : CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(
          slide.title,
          textAlign: align,
          style: theme.textTheme.headlineSmall?.copyWith(
            color: theme.colorScheme.onSurface,
          ),
        ),
        const SizedBox(height: 14),
        Text(
          slide.subtitle,
          textAlign: align,
          style: theme.textTheme.bodyMedium?.copyWith(
            color: theme.colorScheme.onSurfaceVariant,
            height: 1.5,
          ),
        ),
      ],
    );

    return LayoutBuilder(
      builder: (context, constraints) {
        final sideBySide = constraints.maxWidth >= 720;
        return Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 8),
            child: ConstrainedBox(
              constraints: BoxConstraints(maxWidth: sideBySide ? 960 : 560),
              child: sideBySide
                  ? Row(
                      crossAxisAlignment: CrossAxisAlignment.center,
                      children: [
                        Expanded(child: photo),
                        const SizedBox(width: 40),
                        Expanded(child: text(TextAlign.start)),
                      ],
                    )
                  : Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        photo,
                        const SizedBox(height: 32),
                        text(TextAlign.center),
                      ],
                    ),
            ),
          ),
        );
      },
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

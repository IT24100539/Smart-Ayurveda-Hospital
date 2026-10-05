import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../l10n/app_localizations.dart';
import '../../feedback/presentation/unread_count_badge.dart';

/// Bottom navigation on phones, rail on wide screens.
///
/// Each tab is a branch of a [StatefulShellRoute], so its navigation stack and
/// scroll position survive switching tabs.
class HomeShell extends StatelessWidget {
  const HomeShell({required this.navigationShell, super.key});

  final StatefulNavigationShell navigationShell;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    return LayoutBuilder(
      builder: (context, constraints) {
        final expanded = constraints.maxWidth > 1024;
        void select(int index) => navigationShell.goBranch(
          index,
          initialLocation: index == navigationShell.currentIndex,
        );

        final barDestinations = [
          NavigationDestination(
            icon: const Icon(Icons.home_outlined),
            selectedIcon: const Icon(Icons.home),
            label: l10n.navHome,
          ),
          NavigationDestination(
            icon: const Icon(Icons.spa_outlined),
            selectedIcon: const Icon(Icons.spa),
            label: l10n.navTreatments,
          ),
          NavigationDestination(
            icon: const Icon(Icons.event_available_outlined),
            selectedIcon: const Icon(Icons.event_available),
            label: l10n.navAppointments,
          ),
          NavigationDestination(
            icon: const UnreadCountBadge(
              child: Icon(Icons.reviews_outlined),
            ),
            selectedIcon: const UnreadCountBadge(
              child: Icon(Icons.reviews),
            ),
            label: l10n.navFeedback,
          ),
          NavigationDestination(
            icon: const Icon(Icons.person_outline),
            selectedIcon: const Icon(Icons.person),
            label: l10n.navProfile,
          ),
        ];

        if (expanded) {
          return Scaffold(
            body: Row(
              children: [
                NavigationRail(
                  selectedIndex: navigationShell.currentIndex,
                  onDestinationSelected: select,
                  labelType: NavigationRailLabelType.all,
                  destinations: [
                    NavigationRailDestination(
                      icon: const Icon(Icons.home_outlined),
                      selectedIcon: const Icon(Icons.home),
                      label: Text(l10n.navHome),
                    ),
                    NavigationRailDestination(
                      icon: const Icon(Icons.spa_outlined),
                      selectedIcon: const Icon(Icons.spa),
                      label: Text(l10n.navTreatments),
                    ),
                    NavigationRailDestination(
                      icon: const Icon(Icons.event_available_outlined),
                      selectedIcon: const Icon(Icons.event_available),
                      label: Text(l10n.navAppointments),
                    ),
                    NavigationRailDestination(
                      icon: const UnreadCountBadge(
                        child: Icon(Icons.reviews_outlined),
                      ),
                      selectedIcon: const UnreadCountBadge(
                        child: Icon(Icons.reviews),
                      ),
                      label: Text(l10n.navFeedback),
                    ),
                    NavigationRailDestination(
                      icon: const Icon(Icons.person_outline),
                      selectedIcon: const Icon(Icons.person),
                      label: Text(l10n.navProfile),
                    ),
                  ],
                ),
                const VerticalDivider(width: 1),
                Expanded(child: navigationShell),
              ],
            ),
          );
        }

        return Scaffold(
          body: navigationShell,
          bottomNavigationBar: NavigationBar(
            selectedIndex: navigationShell.currentIndex,
            onDestinationSelected: select,
            destinations: barDestinations,
          ),
        );
      },
    );
  }
}

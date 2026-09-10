import 'package:flutter/material.dart';

import '../../../l10n/app_localizations.dart';
import '../../../shared/widgets/placeholder_panel.dart';

class AppointmentsScreen extends StatelessWidget {
  const AppointmentsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.navAppointments)),
      body: PlaceholderPanel(
        icon: Icons.event_available_outlined,
        message: l10n.appointmentsPlaceholder,
      ),
    );
  }
}

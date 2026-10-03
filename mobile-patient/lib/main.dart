import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'src/app.dart';
import 'src/theme/theme_mode_controller.dart';
import 'src/theme/theme_mode_storage.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  final storage = createThemeModeStorage();
  final themeMode = await storage.read();
  runApp(
    ProviderScope(
      overrides: [
        unauthorizedOverride,
        themeModeStorageProvider.overrideWithValue(storage),
        initialThemeModeProvider.overrideWithValue(themeMode),
      ],
      child: const PatientApp(),
    ),
  );
}

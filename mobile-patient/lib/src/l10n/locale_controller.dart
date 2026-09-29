import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Locales the patient app ships, in preference order.
///
/// Sinhala is first so it is both the default and the fallback for an
/// unmatched device locale.
const supportedLocales = <Locale>[Locale('si'), Locale('en')];

const defaultLocale = Locale('si');

/// The active locale.
///
/// Switched from the login screen, the splash screen, and the Profile tab.
/// Not persisted yet, so it resets on a cold start.
class LocaleController extends Notifier<Locale> {
  @override
  Locale build() => defaultLocale;

  void setLocale(Locale locale) {
    if (!supportedLocales.contains(locale)) return;
    state = locale;
  }
}

final localeControllerProvider = NotifierProvider<LocaleController, Locale>(
  LocaleController.new,
);

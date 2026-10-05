import 'package:flutter/material.dart';

/// Short fades and slides. All motion stops when the patient asks for less animation.
abstract final class AyurvedaMotion {
  static const Duration short = Duration(milliseconds: 160);
  static const Duration medium = Duration(milliseconds: 220);
  static const Curve curve = Curves.easeOut;

  static bool reduce(BuildContext context) {
    return MediaQuery.disableAnimationsOf(context);
  }

  static Duration of(BuildContext context, Duration duration) {
    return reduce(context) ? Duration.zero : duration;
  }
}

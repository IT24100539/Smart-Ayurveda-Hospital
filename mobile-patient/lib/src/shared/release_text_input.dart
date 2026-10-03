import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

/// Drops the active field before a route or list item is removed.
///
/// Flutter web asserts if a [TextField] is detached while its DOM input is
/// still the editing strategy. Login, tab changes, and feed rebuilds hit that.
void releaseTextInput() {
  FocusManager.instance.primaryFocus?.unfocus();
  TextInput.finishAutofillContext(shouldSave: false);
}

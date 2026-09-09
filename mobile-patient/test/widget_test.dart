import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/main.dart';

void main() {
  testWidgets('home shows hospital greeting', (WidgetTester tester) async {
    await tester.pumpWidget(const PatientApp());
    expect(find.text('Namaste'), findsOneWidget);
    expect(find.text('Smart Ayurveda'), findsOneWidget);
  });
}

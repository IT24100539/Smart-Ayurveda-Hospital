import 'package:flutter/material.dart';

void main() {
  runApp(const PatientApp());
}

class PatientApp extends StatelessWidget {
  const PatientApp({super.key});

  @override
  Widget build(BuildContext context) {
    const forest = Color(0xFF1B4332);
    return MaterialApp(
      title: 'Smart Ayurveda',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: forest,
          primary: forest,
          surface: const Color(0xFFF7F3EA),
        ),
        useMaterial3: true,
      ),
      home: const HomePage(),
    );
  }
}

class HomePage extends StatelessWidget {
  const HomePage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Smart Ayurveda')),
      body: ListView(
        padding: const EdgeInsets.all(20),
        children: const [
          Text(
            'Namaste',
            style: TextStyle(fontSize: 28, fontWeight: FontWeight.w600),
          ),
          SizedBox(height: 8),
          Text('Book consultations, view treatments, and follow your panchakarma plan.'),
          SizedBox(height: 24),
          _HomeCard(
            title: 'Upcoming appointment',
            body: 'No visit scheduled. Call the front desk or request a slot from the staff portal.',
          ),
          SizedBox(height: 12),
          _HomeCard(
            title: 'Today’s advice',
            body: 'Warm, freshly cooked meals. Avoid iced drinks if kapha is high.',
          ),
        ],
      ),
    );
  }
}

class _HomeCard extends StatelessWidget {
  const _HomeCard({required this.title, required this.body});

  final String title;
  final String body;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(title, style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            Text(body),
          ],
        ),
      ),
    );
  }
}

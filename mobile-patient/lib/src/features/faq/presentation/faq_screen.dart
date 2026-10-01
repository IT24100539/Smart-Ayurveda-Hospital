import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../l10n/feature_localizations.dart';
import '../../../router/app_routes.dart';
import '../../../theme/app_theme.dart';

abstract final class FaqKeys {
  static const searchInput = ValueKey('faq-search-input');
  static const bookingTile = ValueKey('faq-tile-booking');
  static const rescheduleTile = ValueKey('faq-tile-reschedule');
  static const cancelTile = ValueKey('faq-tile-cancel');
  static const charakaCapabilitiesTile = ValueKey('faq-tile-charaka-capabilities');
  static const charakaLimitationsTile = ValueKey('faq-tile-charaka-limitations');
  static const wardsTile = ValueKey('faq-tile-wards');
}

class FaqScreen extends StatefulWidget {
  const FaqScreen({super.key});

  @override
  State<FaqScreen> createState() => _FaqScreenState();
}

class _FaqScreenState extends State<FaqScreen> {
  final TextEditingController _searchController = TextEditingController();
  String _filterQuery = '';

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final copy = FeatureLocalizations.of(context);

    final List<_FaqItem> items = [
      _FaqItem(
        key: FaqKeys.bookingTile,
        category: copy.text('Appointments', 'වෙන්කිරීම්'),
        icon: Icons.calendar_today_outlined,
        question: copy.text(
          'How do I request an appointment?',
          'හමුවීමක් වෙන්කරවා ගන්නේ කෙසේද?',
        ),
        answer: copy.text(
          'Go to the Treatments tab and select your required Ayurvedic therapy (e.g. Abhyanga, Shirodhara). Tap "Book Session", select an available date and timeslot from our schedule, and submit your request. Hospital front-desk staff will review and approve your appointment.',
          'ප්‍රතිකාර පටිත්තට පිවිස ඔබට අවශ්‍ය ආයුර්වේද ප්‍රතිකාරය තෝරන්න (උදා: අභ්‍යංග, ශිරෝධාරා). "හමුවීමක් වෙන්කරන්න" තට්ටු කර, පවතින දිනයක් සහ වේලාවක් තෝරා ඉල්ලීම යොමු කරන්න. රෝහල් කාර්ය මණ්ඩලය එය පරීක්ෂා කර අනුමත කරනු ඇත.',
        ),
      ),
      _FaqItem(
        key: FaqKeys.rescheduleTile,
        category: copy.text('Appointments', 'වෙන්කිරීම්'),
        icon: Icons.event_repeat_outlined,
        question: copy.text(
          'Can I reschedule my appointment?',
          'මගේ හමුවීම වෙනත් දිනකට මාරු කළ හැකිද?',
        ),
        answer: copy.text(
          'Yes. Any appointment in "Pending" or "Approved" status can be rescheduled. Open the appointment from the Appointments tab, tap "Reschedule appointment", and choose a new available date and time slot. Completed, cancelled, or rejected appointments cannot be rescheduled.',
          'ඔව්. "අනුමැතිය අපේක්ෂිත" හෝ "අනුමත" තත්ත්වයේ පවතින හමුවීම් වෙනත් දිනයකට මාරු කළ හැක. වෙන්කිරීම් පටිත්තෙන් හමුවීම තෝරා, "හමුවීම වෙනත් දිනකට මාරු කරන්න" ඔබා නව දිනයක් සහ වේලාවක් තෝරන්න. සම්පූර්ණ වූ, අවලංගු කළ හෝ ප්‍රතික්ෂේප වූ හමුවීම් වෙනස් කළ නොහැක.',
        ),
      ),
      _FaqItem(
        key: FaqKeys.cancelTile,
        category: copy.text('Appointments', 'වෙන්කිරීම්'),
        icon: Icons.cancel_outlined,
        question: copy.text(
          'How do I cancel an appointment?',
          'හමුවීමක් අවලංගු කරන්නේ කෙසේද?',
        ),
        answer: copy.text(
          'Open your pending or approved appointment from the Appointments tab and select "Cancel appointment". Confirm the cancellation prompt. The status will immediately update to Cancelled. Closed or already cancelled appointments cannot be modified.',
          'වෙන්කිරීම් පටිත්තෙන් අදාළ හමුවීම තෝරා "හමුවීම අවලංගු කරන්න" තෝරන්න. අනතුරුව තහවුරු කරන්න. හමුවීමේ තත්ත්වය අවලංගු වූ ලෙස යාවත්කාලීන වේ. දැනටමත් සම්පූර්ණ වූ හෝ අවලංගු වූ හමුවීම් වෙනස් කළ නොහැක.',
        ),
      ),
      _FaqItem(
        key: FaqKeys.charakaCapabilitiesTile,
        category: copy.text('Charaka AI Assistant', 'චරක AI සහායක'),
        icon: Icons.support_agent_outlined,
        question: copy.text(
          'What information can Charaka AI provide?',
          'චරක AI සහායකයාගෙන් ලබාගත හැකි තොරතුරු මොනවාද?',
        ),
        answer: copy.text(
          'Charaka can answer questions regarding available hospital therapies, session durations, fees, and general facility schedules. When signed in, Charaka can also look up your registered administrative records such as your UHID and registered province.',
          'රෝහලේ ඇති ආයුර්වේද ප්‍රතිකාර, ප්‍රතිකාර කාලසීමාවන්, ගාස්තු සහ සායන කාලසටහන් පිළිබඳ තොරතුරු චරක සහායකයාගෙන් ලබාගත හැක. ඔබ ලොග් වී සිටින විට ඔබගේ ලියාපදිංචි UHID අංකය සහ පළාත වැනි පරිපාලන තොරතුරු ද විමසිය හැක.',
        ),
      ),
      _FaqItem(
        key: FaqKeys.charakaLimitationsTile,
        category: copy.text('Charaka AI Assistant', 'චරක AI සහායක'),
        icon: Icons.health_and_safety_outlined,
        question: copy.text(
          'Can Charaka AI provide medical diagnoses or prescriptions?',
          'චරක සහායකයාට වෛද්‍ය උපදෙස් හෝ බෙහෙත් නියම කළ හැකිද?',
        ),
        answer: copy.text(
          'No. Charaka is an informational assistant and by strict safety policy cannot prescribe treatments or diagnose illnesses. If you require medical evaluation, please book a consultation with our qualified Ayurvedic doctors.',
          'නැත. චරක යනු තොරතුරු සහායකයෙකු පමණක් වන අතර රෝග විනිශ්චය කිරීම හෝ ඖෂධ නියම කිරීම සිදු නොකරයි. වෛද්‍ය පරීක්ෂණයක් අවශ්‍ය නම් කරුණාකර අපගේ සුදුසුකම් ලත් ආයුර්වේද වෛද්‍යවරයෙකු හමුවන්න.',
        ),
      ),
      _FaqItem(
        key: FaqKeys.wardsTile,
        category: copy.text('Facilities & Wards', 'පහසුකම් සහ වාට්ටු'),
        icon: Icons.hotel_outlined,
        question: copy.text(
          'How can I check ward bed occupancy?',
          'රෝහල් වාට්ටුවල ඇඳන් පවතින බව පරීක්ෂා කරන්නේ කෙසේද?',
        ),
        answer: copy.text(
          'You can view live bed availability across General, Semi-Private, and Private wards directly from the Wards screen accessible via the hospital menu or quick actions.',
          'සාමාන්‍ය, අර්ධ-පුද්ගලික සහ පුද්ගලික වාට්ටුවල සජීවී ඇඳන් පවතින බව රෝහල් මෙනුව හෝ කෙටිමං හරහා වාට්ටු අංශයට පිවිස පරීක්ෂා කළ හැක.',
        ),
      ),
    ];

    final filteredItems = _filterQuery.isEmpty
        ? items
        : items.where((item) {
            final q = _filterQuery.toLowerCase();
            return item.question.toLowerCase().contains(q) ||
                item.answer.toLowerCase().contains(q) ||
                item.category.toLowerCase().contains(q);
          }).toList();

    return Scaffold(
      appBar: AppBar(
        title: Text(
          copy.text('Frequently Asked Questions', 'නිතර අසන ප්‍රශ්න'),
        ),
      ),
      body: SafeArea(
        child: Column(
          children: [
            // Search field
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
              child: TextField(
                key: FaqKeys.searchInput,
                controller: _searchController,
                onChanged: (val) => setState(() => _filterQuery = val.trim()),
                decoration: InputDecoration(
                  hintText: copy.text(
                    'Search questions and answers…',
                    'ප්‍රශ්න සහ පිළිතුරු සොයන්න…',
                  ),
                  prefixIcon: const Icon(Icons.search),
                  suffixIcon: _filterQuery.isNotEmpty
                      ? IconButton(
                          icon: const Icon(Icons.clear),
                          onPressed: () {
                            _searchController.clear();
                            setState(() => _filterQuery = '');
                          },
                        )
                      : null,
                  filled: true,
                  fillColor: theme.colorScheme.surfaceContainerHighest.withValues(
                    alpha: 0.5,
                  ),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(14),
                    borderSide: BorderSide.none,
                  ),
                ),
              ),
            ),

            // FAQ List
            Expanded(
              child: filteredItems.isEmpty
                  ? Center(
                      child: Text(
                        copy.text(
                          'No matching questions found.',
                          'ගැළපෙන ප්‍රශ්න හමු නොවීය.',
                        ),
                        style: TextStyle(
                          color: theme.colorScheme.onSurfaceVariant,
                        ),
                      ),
                    )
                  : ListView.separated(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 16,
                        vertical: 12,
                      ),
                      itemCount: filteredItems.length,
                      separatorBuilder: (context, index) =>
                          const SizedBox(height: 10),
                      itemBuilder: (context, index) {
                        final item = filteredItems[index];
                        return Card(
                          key: item.key,
                          elevation: 0,
                          color: theme.colorScheme.surfaceContainerLow,
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(16),
                            side: BorderSide(
                              color: theme.colorScheme.outlineVariant.withValues(
                                alpha: 0.4,
                              ),
                            ),
                          ),
                          child: Theme(
                            data: theme.copyWith(dividerColor: Colors.transparent),
                            child: ExpansionTile(
                              leading: Container(
                                padding: const EdgeInsets.all(8),
                                decoration: BoxDecoration(
                                  color: AyurvedaColors.forest.withValues(alpha: 0.1),
                                  shape: BoxShape.circle,
                                ),
                                child: Icon(
                                  item.icon,
                                  size: 20,
                                  color: AyurvedaColors.forest,
                                ),
                              ),
                              title: Text(
                                item.question,
                                style: theme.textTheme.titleSmall?.copyWith(
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                              subtitle: Padding(
                                padding: const EdgeInsets.only(top: 4),
                                child: Text(
                                  item.category,
                                  style: TextStyle(
                                    fontSize: 12,
                                    color: AyurvedaColors.forest,
                                    fontWeight: FontWeight.w500,
                                  ),
                                ),
                              ),
                              children: [
                                Padding(
                                  padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
                                  child: Text(
                                    item.answer,
                                    style: theme.textTheme.bodyMedium?.copyWith(
                                      color: theme.colorScheme.onSurfaceVariant,
                                      height: 1.5,
                                    ),
                                  ),
                                ),
                              ],
                            ),
                          ),
                        );
                      },
                    ),
            ),

            // Bottom Contact Button
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: theme.colorScheme.surface,
                border: Border(
                  top: BorderSide(
                    color: theme.colorScheme.outlineVariant.withValues(alpha: 0.3),
                  ),
                ),
              ),
              child: Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          copy.text('Still have questions?', 'තවත් ප්‍රශ්න තිබේද?'),
                          style: const TextStyle(fontWeight: FontWeight.bold),
                        ),
                        Text(
                          copy.text(
                            'View hospital location & contact details',
                            'රෝහල් ස්ථානය සහ සම්බන්ධතා බලන්න',
                          ),
                          style: TextStyle(
                            fontSize: 12,
                            color: theme.colorScheme.onSurfaceVariant,
                          ),
                        ),
                      ],
                    ),
                  ),
                  OutlinedButton.icon(
                    style: OutlinedButton.styleFrom(
                      minimumSize: const Size(80, 40),
                      padding: const EdgeInsets.symmetric(
                        horizontal: 14,
                        vertical: 8,
                      ),
                    ),
                    onPressed: () => context.push(AppRoutes.contact),
                    icon: const Icon(Icons.place_outlined, size: 16),
                    label: Text(copy.text('Contact', 'සම්බන්ධතා')),
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

class _FaqItem {
  const _FaqItem({
    required this.key,
    required this.category,
    required this.icon,
    required this.question,
    required this.answer,
  });

  final Key key;
  final String category;
  final IconData icon;
  final String question;
  final String answer;
}

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../l10n/feature_localizations.dart';
import '../../../router/app_routes.dart';
import '../../../shared/widgets/clinic_widgets.dart';
import '../../../shared/widgets/empty_state.dart';
import '../../../shared/widgets/page_layout.dart';
import '../../../shared/widgets/safe_asset_image.dart';
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
          'Charaka can talk through any Ayurveda question — doshas, herbs, food, daily routine, panchakarma — even when it is not on the hospital list. He can also tell you this hospital\'s therapy days and fees, and, when you are signed in, your registered details such as your UHID. He does not answer questions outside Ayurveda.',
          'චරකට ආයුර්වේද ප්‍රශ්න ඕනෑම එකක් ගැන කතා කළ හැක — දෝෂ, ඖෂධ, ආහාර, දිනචර්යාව, පංචකර්ම — ඒවා රෝහල් ලැයිස්තුවේ නැතත්. මෙම රෝහලේ ප්‍රතිකාර දින සහ ගාස්තු ද කියයි. ඔබ ලොග් වී සිටින විට UHID වැනි ලියාපදිංචි තොරතුරු ද විමසිය හැක. ආයුර්වේදයෙන් එහා ප්‍රශ්නවලට ඔහු පිළිතුරු නොදෙයි.',
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
          'No. Charaka is an informational assistant and cannot prescribe treatments or diagnose illnesses. If you need a medical evaluation, book a consultation with a hospital vaidya.',
          'නැත. චරක යනු තොරතුරු සහායකයෙකු පමණක් වන අතර රෝග විනිශ්චය කිරීම හෝ ඖෂධ නියම කිරීම සිදු නොකරයි. වෛද්‍ය පරීක්ෂණයක් අවශ්‍ය නම් රෝහලේ වෛද්‍යවරයෙකු හමුවන්න.',
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
          'Open the Wards screen from Home to see current bed occupancy. Only occupancy totals are shown.',
          'මුල් පිටුවෙන් වාට්ටු තිරය විවෘත කර වත්මන් ඇඳන් ප්‍රමාණය බලන්න. පෙන්වන්නේ සමස්ත සංඛ්‍යා පමණි.',
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

    final brand = AyurvedaThemeExtension.of(context);

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
            CenteredContent(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(20, 12, 20, 8),
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
                            tooltip: MaterialLocalizations.of(
                              context,
                            ).deleteButtonTooltip,
                            icon: const Icon(Icons.clear),
                            onPressed: () {
                              _searchController.clear();
                              setState(() => _filterQuery = '');
                            },
                          )
                        : null,
                  ),
                ),
              ),
            ),

            // FAQ list. The contact prompt sits at the end of the list, so it
            // never covers a question on a short screen.
            Expanded(
              child: PageScrollView(
                maxWidth: contentWidth,
                padding: const EdgeInsets.fromLTRB(20, 4, 20, 24),
                children: [
                  ClipRRect(
                    borderRadius: BorderRadius.circular(brand.cardRadius),
                    child: const SafeAssetImage(
                      asset: 'assets/images/herb-pharmacy.png',
                      height: 140,
                      width: double.infinity,
                    ),
                  ),
                  const SizedBox(height: 16),
                  if (filteredItems.isEmpty)
                    EmptyState(
                      compact: true,
                      icon: Icons.search_off_outlined,
                      message: copy.text(
                        'No matching questions found.',
                        'ගැළපෙන ප්‍රශ්න හමු නොවීය.',
                      ),
                    )
                  else
                    for (final item in filteredItems) ...[
                      ClinicCard(
                        key: item.key,
                        padding: EdgeInsets.zero,
                        child: Theme(
                          data: theme.copyWith(dividerColor: Colors.transparent),
                          child: ExpansionTile(
                            shape: const Border(),
                            collapsedShape: const Border(),
                            iconColor: brand.teal,
                            collapsedIconColor: theme.colorScheme.onSurfaceVariant,
                            tilePadding: const EdgeInsets.symmetric(
                              horizontal: 16,
                              vertical: 4,
                            ),
                            leading: Container(
                              padding: const EdgeInsets.all(8),
                              decoration: BoxDecoration(
                                color: theme.colorScheme.primaryContainer,
                                shape: BoxShape.circle,
                              ),
                              child: Icon(item.icon, size: 20, color: brand.teal),
                            ),
                            title: Text(
                              item.question,
                              style: theme.textTheme.titleSmall,
                            ),
                            subtitle: Padding(
                              padding: const EdgeInsets.only(top: 4),
                              child: Text(
                                item.category.toUpperCase(),
                                style: AyurvedaType.eyebrow(
                                  context,
                                  color: brand.teal,
                                ),
                              ),
                            ),
                            children: [
                              Padding(
                                padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
                                child: Align(
                                  alignment: Alignment.centerLeft,
                                  child: Text(
                                    item.answer,
                                    style: theme.textTheme.bodyMedium?.copyWith(
                                      color: theme.colorScheme.onSurfaceVariant,
                                      height: 1.5,
                                    ),
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                      const SizedBox(height: 10),
                    ],
                  const SizedBox(height: 6),
                  ClinicCard(
                    child: Row(
                      children: [
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                copy.text(
                                  'Still have questions?',
                                  'තවත් ප්‍රශ්න තිබේද?',
                                ),
                                style: theme.textTheme.titleSmall,
                              ),
                              const SizedBox(height: 2),
                              Text(
                                copy.text(
                                  'View hospital location & contact details',
                                  'රෝහල් ස්ථානය සහ සම්බන්ධතා බලන්න',
                                ),
                                style: theme.textTheme.bodySmall?.copyWith(
                                  color: theme.colorScheme.onSurfaceVariant,
                                ),
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(width: 12),
                        OutlinedButton.icon(
                          style: OutlinedButton.styleFrom(
                            minimumSize: const Size(80, 44),
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

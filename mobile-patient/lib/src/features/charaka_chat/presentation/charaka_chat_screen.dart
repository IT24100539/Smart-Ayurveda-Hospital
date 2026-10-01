import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../core/network/api_exception.dart';
import '../../../l10n/feature_localizations.dart';
import '../../../theme/app_theme.dart';
import '../../auth/application/auth_controller.dart';
import '../data/charaka_chat_repository.dart';
import '../domain/charaka_models.dart';

abstract final class CharakaChatKeys {
  static const input = ValueKey('charaka-chat-input');
  static const sendButton = ValueKey('charaka-send-button');
  static const offlineBanner = ValueKey('charaka-offline-banner');
  static const typingIndicator = ValueKey('charaka-typing-indicator');
  static const retryButton = ValueKey('charaka-retry-button');
  static const refusalBadge = ValueKey('charaka-refusal-badge');
}

class CharakaChatScreen extends ConsumerStatefulWidget {
  const CharakaChatScreen({super.key});

  @override
  ConsumerState<CharakaChatScreen> createState() => _CharakaChatScreenState();
}

class _CharakaChatScreenState extends ConsumerState<CharakaChatScreen> {
  final _textController = TextEditingController();
  final _scrollController = ScrollController();
  final List<CharakaChatMessage> _messages = [];

  CharakaTopic _currentTopic = CharakaTopic.treatments;
  bool _isLoading = false;
  bool _isOffline = false;
  String? _offlineReason;

  @override
  void initState() {
    super.initState();
    // Seed initial assistant welcome message
    _messages.add(
      CharakaChatMessage(
        id: 'welcome-initial',
        text: '', // Will be resolved dynamically with localization in build/didChangeDependencies
        isUser: false,
        timestamp: DateTime.now(),
        topic: CharakaTopic.treatments,
      ),
    );
  }

  @override
  void dispose() {
    _textController.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  List<String> _suggestedPromptsFor(CharakaTopic topic) {
    if (topic == CharakaTopic.treatments) {
      return [
        'When is Panchakarma available and what is the fee?',
        'What therapies do you offer for stress and relaxation?',
        'How many days does Shirodhara therapy take?',
      ];
    } else {
      return [
        'What is my registered UHID and contact number?',
        'Which district is my profile registered under?',
        'What is my registered Prakriti type?',
      ];
    }
  }

  void _scrollToBottom() {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (_scrollController.hasClients) {
        _scrollController.animateTo(
          _scrollController.position.maxScrollExtent,
          duration: const Duration(milliseconds: 300),
          curve: Curves.easeOut,
        );
      }
    });
  }

  Future<void> _sendMessage([String? overrideText]) async {
    final text = (overrideText ?? _textController.text).trim();
    if (text.isEmpty || _isLoading) return;

    if (overrideText == null) {
      _textController.clear();
    }

    final userMsg = CharakaChatMessage(
      id: DateTime.now().microsecondsSinceEpoch.toString(),
      text: text,
      isUser: true,
      timestamp: DateTime.now(),
      topic: _currentTopic,
    );

    setState(() {
      _messages.add(userMsg);
      _isLoading = true;
      _isOffline = false;
      _offlineReason = null;
    });
    _scrollToBottom();

    await _performAsk(userMsg);
  }

  Future<void> _retryMessage(CharakaChatMessage failedUserMsg) async {
    if (_isLoading) return;

    // Remove any previous error message for this question
    setState(() {
      _messages.removeWhere(
        (m) => m.isError && m.errorMessage == failedUserMsg.id,
      );
      _isLoading = true;
      _isOffline = false;
      _offlineReason = null;
    });
    _scrollToBottom();

    await _performAsk(failedUserMsg);
  }

  Future<void> _performAsk(CharakaChatMessage userMsg) async {
    final repo = ref.read(charakaChatRepositoryProvider);
    try {
      final CharakaAnswer result;
      if (userMsg.topic == CharakaTopic.treatments) {
        result = await repo.askTreatment(userMsg.text);
      } else {
        result = await repo.askPatient(userMsg.text);
      }

      if (!mounted) return;
      setState(() {
        _isLoading = false;
        _messages.add(
          CharakaChatMessage(
            id: DateTime.now().microsecondsSinceEpoch.toString(),
            text: result.answer,
            isUser: false,
            timestamp: DateTime.now(),
            topic: userMsg.topic,
            refused: result.refused,
            matchedTreatmentIds: result.matchedTreatmentIds,
          ),
        );
      });
      _scrollToBottom();
    } catch (e) {
      if (!mounted) return;
      final bool isNetErr = (e is ApiException &&
              (e.isNetworkError ||
                  e.statusCode == 502 ||
                  e.statusCode == 503 ||
                  e.statusCode == 504)) ||
          e.toString().toLowerCase().contains('connection') ||
          e.toString().toLowerCase().contains('offline') ||
          e.toString().toLowerCase().contains('socket') ||
          e.toString().toLowerCase().contains('timeout');

      final copy = FeatureLocalizations.of(context);
      final errorDisplay = e is ApiException
          ? (e.message ?? copy.askUnavailable)
          : copy.askUnavailable;

      setState(() {
        _isLoading = false;
        if (isNetErr) {
          _isOffline = true;
          _offlineReason = copy.charakaOfflineBanner;
        }
        _messages.add(
          CharakaChatMessage(
            id: DateTime.now().microsecondsSinceEpoch.toString(),
            text: errorDisplay,
            isUser: false,
            timestamp: DateTime.now(),
            topic: userMsg.topic,
            isError: true,
            errorMessage: userMsg.id, // Reference to original question
          ),
        );
      });
      _scrollToBottom();
    }
  }

  @override
  Widget build(BuildContext context) {
    final copy = FeatureLocalizations.of(context);
    final theme = Theme.of(context);
    final authState = ref.watch(authControllerProvider);
    final isPatientSignedIn = authState.isAuthenticated;

    return Scaffold(
      appBar: AppBar(
        title: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(6),
              decoration: BoxDecoration(
                color: AyurvedaColors.forest.withValues(alpha: 0.15),
                shape: BoxShape.circle,
              ),
              child: const Icon(
                Icons.spa,
                color: AyurvedaColors.forest,
                size: 20,
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    copy.charakaChatTitle,
                    style: theme.textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  Text(
                    copy.text('Hospital assistant', 'රෝහල් සහායක'),
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: AyurvedaColors.forest,
                      fontWeight: FontWeight.w500,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
      body: SafeArea(
        child: Column(
          children: [
            // Mode / Topic Selector
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 6, 16, 4),
              child: SegmentedButton<CharakaTopic>(
                segments: [
                  ButtonSegment(
                    value: CharakaTopic.treatments,
                    icon: const Icon(Icons.local_hospital_outlined, size: 18),
                    label: Text(copy.charakaTopicTreatments),
                  ),
                  ButtonSegment(
                    value: CharakaTopic.patientInfo,
                    icon: const Icon(Icons.person_pin_outlined, size: 18),
                    label: Text(copy.charakaTopicPatient),
                  ),
                ],
                selected: {_currentTopic},
                onSelectionChanged: (selected) {
                  setState(() {
                    _currentTopic = selected.first;
                  });
                },
                style: SegmentedButton.styleFrom(
                  selectedBackgroundColor: AyurvedaColors.forest.withValues(alpha: 0.15),
                  selectedForegroundColor: AyurvedaColors.forestDark,
                ),
              ),
            ),

            // Medical Disclaimer Notice Banner
            Container(
              margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
              decoration: BoxDecoration(
                color: AyurvedaColors.cream,
                borderRadius: BorderRadius.circular(10),
                border: Border.all(
                  color: AyurvedaColors.gold.withValues(alpha: 0.3),
                ),
              ),
              child: Row(
                children: [
                  const Icon(
                    Icons.info_outline,
                    color: AyurvedaColors.gold,
                    size: 18,
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      copy.charakaDisclaimer,
                      style: theme.textTheme.bodySmall?.copyWith(
                        fontSize: 11,
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                  ),
                ],
              ),
            ),

            // Offline / Unreachable Banner
            if (_isOffline)
              Container(
                key: CharakaChatKeys.offlineBanner,
                margin: const EdgeInsets.fromLTRB(16, 4, 16, 4),
                padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                decoration: BoxDecoration(
                  color: theme.colorScheme.errorContainer,
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: theme.colorScheme.error),
                ),
                child: Row(
                  children: [
                    Icon(
                      Icons.wifi_off_rounded,
                      color: theme.colorScheme.onErrorContainer,
                      size: 20,
                    ),
                    const SizedBox(width: 10),
                    Expanded(
                      child: Text(
                        _offlineReason ?? copy.charakaOfflineBanner,
                        style: TextStyle(
                          color: theme.colorScheme.onErrorContainer,
                          fontSize: 12,
                          fontWeight: FontWeight.w500,
                        ),
                      ),
                    ),
                    IconButton(
                      icon: const Icon(Icons.close, size: 18),
                      tooltip: 'Dismiss',
                      onPressed: () => setState(() => _isOffline = false),
                    ),
                  ],
                ),
              ),

            // Chat Messages Area
            Expanded(
              child: ListView.builder(
                controller: _scrollController,
                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                itemCount: _messages.length + (_isLoading ? 1 : 0),
                itemBuilder: (context, index) {
                  if (index == _messages.length && _isLoading) {
                    return _TypingBubble(copy: copy);
                  }

                  final msg = _messages[index];
                  // If it's the first welcome message, substitute localized text
                  final displayText = msg.id == 'welcome-initial'
                      ? (_currentTopic == CharakaTopic.treatments
                          ? copy.charakaWelcomeTreatments
                          : copy.charakaWelcomePatient)
                      : msg.text;

                  return _MessageBubble(
                    message: msg.copyWith(text: displayText),
                    onRetry: msg.isError
                        ? () {
                            final original = _messages.firstWhere(
                              (m) => m.id == msg.errorMessage,
                              orElse: () => msg,
                            );
                            _retryMessage(original);
                          }
                        : null,
                  );
                },
              ),
            ),

            // Suggested Prompts Chips
            SizedBox(
              height: 44,
              child: ListView(
                scrollDirection: Axis.horizontal,
                padding: const EdgeInsets.symmetric(horizontal: 12),
                children: _suggestedPromptsFor(_currentTopic).map((prompt) {
                  return Padding(
                    padding: const EdgeInsets.only(right: 8),
                    child: ActionChip(
                      avatar: const Icon(Icons.chat_bubble_outline, size: 14),
                      label: Text(
                        prompt,
                        style: const TextStyle(fontSize: 12),
                      ),
                      backgroundColor: theme.colorScheme.surface,
                      side: BorderSide(
                        color: AyurvedaColors.forest.withValues(alpha: 0.3),
                      ),
                      onPressed: _isLoading ? null : () => _sendMessage(prompt),
                    ),
                  );
                }).toList(),
              ),
            ),

            const Divider(height: 1),

            // Bottom Input Bar
            Padding(
              padding: const EdgeInsets.fromLTRB(14, 8, 14, 12),
              child: Row(
                children: [
                  Expanded(
                    child: TextField(
                      key: CharakaChatKeys.input,
                      controller: _textController,
                      enabled: !_isLoading && isPatientSignedIn,
                      textInputAction: TextInputAction.send,
                      onSubmitted: (_) => _sendMessage(),
                      decoration: InputDecoration(
                        hintText: isPatientSignedIn
                            ? copy.charakaInputHint
                            : copy.askSignInRequired,
                        contentPadding: const EdgeInsets.symmetric(
                          horizontal: 16,
                          vertical: 12,
                        ),
                        filled: true,
                        fillColor: theme.colorScheme.surface,
                        border: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(24),
                          borderSide: BorderSide(
                            color: theme.colorScheme.outline.withValues(alpha: 0.4),
                          ),
                        ),
                        enabledBorder: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(24),
                          borderSide: BorderSide(
                            color: theme.colorScheme.outline.withValues(alpha: 0.4),
                          ),
                        ),
                        focusedBorder: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(24),
                          borderSide: const BorderSide(
                            color: AyurvedaColors.forest,
                            width: 1.5,
                          ),
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  IconButton.filled(
                    key: CharakaChatKeys.sendButton,
                    icon: _isLoading
                        ? const SizedBox(
                            width: 18,
                            height: 18,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: Colors.white,
                            ),
                          )
                        : const Icon(Icons.send_rounded, size: 20),
                    style: IconButton.styleFrom(
                      backgroundColor: AyurvedaColors.forest,
                      foregroundColor: Colors.white,
                    ),
                    onPressed: (!_isLoading && isPatientSignedIn)
                        ? () => _sendMessage()
                        : null,
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

class _MessageBubble extends StatelessWidget {
  const _MessageBubble({
    required this.message,
    this.onRetry,
  });

  final CharakaChatMessage message;
  final VoidCallback? onRetry;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isUser = message.isUser;
    final timeStr = DateFormat('h:mm a').format(message.timestamp);
    final copy = FeatureLocalizations.of(context);

    if (message.isError) {
      return Padding(
        padding: const EdgeInsets.symmetric(vertical: 6),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              padding: const EdgeInsets.all(6),
              decoration: BoxDecoration(
                color: theme.colorScheme.errorContainer,
                shape: BoxShape.circle,
              ),
              child: Icon(
                Icons.error_outline,
                color: theme.colorScheme.error,
                size: 18,
              ),
            ),
            const SizedBox(width: 8),
            Expanded(
              child: Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: theme.colorScheme.errorContainer.withValues(alpha: 0.5),
                  borderRadius: BorderRadius.circular(16),
                  border: Border.all(
                    color: theme.colorScheme.error.withValues(alpha: 0.3),
                  ),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      message.text,
                      style: TextStyle(
                        color: theme.colorScheme.onErrorContainer,
                        fontSize: 13,
                      ),
                    ),
                    const SizedBox(height: 8),
                    if (onRetry != null)
                      FilledButton.tonalIcon(
                        key: CharakaChatKeys.retryButton,
                        onPressed: onRetry,
                        style: FilledButton.styleFrom(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 14,
                            vertical: 8,
                          ),
                          visualDensity: VisualDensity.compact,
                        ),
                        icon: const Icon(Icons.refresh, size: 16),
                        label: Text(copy.charakaRetry),
                      ),
                  ],
                ),
              ),
            ),
          ],
        ),
      );
    }

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        mainAxisAlignment:
            isUser ? MainAxisAlignment.end : MainAxisAlignment.start,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (!isUser) ...[
            Container(
              padding: const EdgeInsets.all(6),
              decoration: BoxDecoration(
                color: AyurvedaColors.forest.withValues(alpha: 0.12),
                shape: BoxShape.circle,
              ),
              child: const Icon(
                Icons.spa_outlined,
                color: AyurvedaColors.forest,
                size: 18,
              ),
            ),
            const SizedBox(width: 8),
          ],
          Flexible(
            child: Column(
              crossAxisAlignment:
                  isUser ? CrossAxisAlignment.end : CrossAxisAlignment.start,
              children: [
                // Medical advice refused badge if applicable
                if (message.refused)
                  Container(
                    key: CharakaChatKeys.refusalBadge,
                    margin: const EdgeInsets.only(bottom: 6),
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                    decoration: BoxDecoration(
                      color: AyurvedaColors.terracotta.withValues(alpha: 0.15),
                      borderRadius: BorderRadius.circular(8),
                      border: Border.all(
                        color: AyurvedaColors.terracotta.withValues(alpha: 0.4),
                      ),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        const Icon(
                          Icons.health_and_safety_outlined,
                          size: 14,
                          color: AyurvedaColors.terracotta,
                        ),
                        const SizedBox(width: 6),
                        Text(
                          copy.medicalAdviceRefused,
                          style: const TextStyle(
                            fontSize: 11,
                            fontWeight: FontWeight.bold,
                            color: AyurvedaColors.terracotta,
                          ),
                        ),
                      ],
                    ),
                  ),

                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 16,
                    vertical: 12,
                  ),
                  decoration: BoxDecoration(
                    color: isUser
                        ? AyurvedaColors.forest
                        : theme.colorScheme.surfaceContainerHighest,
                    borderRadius: BorderRadius.only(
                      topLeft: const Radius.circular(18),
                      topRight: const Radius.circular(18),
                      bottomLeft: isUser
                          ? const Radius.circular(18)
                          : const Radius.circular(4),
                      bottomRight: isUser
                          ? const Radius.circular(4)
                          : const Radius.circular(18),
                    ),
                  ),
                  child: Text(
                    message.text,
                    style: TextStyle(
                      color: isUser
                          ? Colors.white
                          : theme.colorScheme.onSurface,
                      fontSize: 14,
                      height: 1.4,
                    ),
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  timeStr,
                  style: theme.textTheme.bodySmall?.copyWith(
                    fontSize: 10,
                    color: theme.colorScheme.onSurfaceVariant,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _TypingBubble extends StatelessWidget {
  const _TypingBubble({required this.copy});

  final FeatureLocalizations copy;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      key: CharakaChatKeys.typingIndicator,
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.all(6),
            decoration: BoxDecoration(
              color: AyurvedaColors.forest.withValues(alpha: 0.12),
              shape: BoxShape.circle,
            ),
            child: const Icon(
              Icons.spa_outlined,
              color: AyurvedaColors.forest,
              size: 18,
            ),
          ),
          const SizedBox(width: 8),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            decoration: BoxDecoration(
              color: theme.colorScheme.surfaceContainerHighest,
              borderRadius: const BorderRadius.only(
                topLeft: Radius.circular(18),
                topRight: Radius.circular(18),
                bottomLeft: Radius.circular(4),
                bottomRight: Radius.circular(18),
              ),
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                const SizedBox(
                  width: 14,
                  height: 14,
                  child: CircularProgressIndicator(
                    strokeWidth: 2,
                    color: AyurvedaColors.forest,
                  ),
                ),
                const SizedBox(width: 10),
                Text(
                  copy.charakaTyping,
                  style: theme.textTheme.bodySmall?.copyWith(
                    fontStyle: FontStyle.italic,
                    color: theme.colorScheme.onSurfaceVariant,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../theme/app_theme.dart';
import '../../application/doctors_provider.dart';
import '../../domain/doctor.dart';

/// Initials, or the portrait uploaded for this physician.
///
/// Never substitutes a stock photo. A missing or failed portrait stays initials.
class DoctorAvatar extends ConsumerWidget {
  const DoctorAvatar({
    required this.doctor,
    this.radius = 24,
    super.key,
  });

  final Doctor doctor;
  final double radius;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (!doctor.hasPhoto) {
      return _InitialsAvatar(initials: doctor.initials, radius: radius);
    }

    final photo = ref.watch(doctorPhotoProvider(doctor.id));
    return photo.when(
      data: (bytes) {
        if (bytes == null || bytes.isEmpty) {
          return _InitialsAvatar(initials: doctor.initials, radius: radius);
        }
        return ClipOval(
          child: Image.memory(
            bytes,
            width: radius * 2,
            height: radius * 2,
            fit: BoxFit.cover,
            gaplessPlayback: true,
            errorBuilder: (context, error, stackTrace) =>
                _InitialsAvatar(initials: doctor.initials, radius: radius),
          ),
        );
      },
      loading: () => _AvatarBone(radius: radius),
      error: (error, stackTrace) =>
          _InitialsAvatar(initials: doctor.initials, radius: radius),
    );
  }
}

class _InitialsAvatar extends StatelessWidget {
  const _InitialsAvatar({required this.initials, required this.radius});

  final String initials;
  final double radius;

  @override
  Widget build(BuildContext context) {
    return ExcludeSemantics(
      child: CircleAvatar(
        radius: radius,
        backgroundColor: Theme.of(context).colorScheme.primaryContainer,
        child: Text(
          initials,
          style: TextStyle(
            color: AyurvedaThemeExtension.of(context).teal,
            fontWeight: FontWeight.w700,
            fontSize: radius * 0.72,
          ),
        ),
      ),
    );
  }
}

class _AvatarBone extends StatelessWidget {
  const _AvatarBone({required this.radius});

  final double radius;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: radius * 2,
      height: radius * 2,
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surfaceContainerHighest,
        shape: BoxShape.circle,
      ),
    );
  }
}

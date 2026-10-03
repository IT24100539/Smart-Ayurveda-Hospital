import 'package:flutter/material.dart';

abstract final class DoctorsScreenKeys {
  static const skeleton = ValueKey('doctors-list-skeleton');
  static const retry = ValueKey('doctors-retry');
  static const search = ValueKey('doctors-search');
}

abstract final class DoctorProfileKeys {
  static const skeleton = ValueKey('doctor-profile-skeleton');
  static const retry = ValueKey('doctor-profile-retry');
  static const rating = ValueKey('doctor-rating');
}

class DoctorListSkeleton extends StatelessWidget {
  const DoctorListSkeleton({super.key});

  @override
  Widget build(BuildContext context) {
    return ListView(
      key: DoctorsScreenKeys.skeleton,
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
      children: const [
        _Bone(height: 96, radius: 20),
        SizedBox(height: 16),
        _DoctorCardBone(),
        _DoctorCardBone(),
        _DoctorCardBone(),
        _DoctorCardBone(),
      ],
    );
  }
}

class DoctorProfileSkeleton extends StatelessWidget {
  const DoctorProfileSkeleton({super.key});

  @override
  Widget build(BuildContext context) {
    return ListView(
      key: DoctorProfileKeys.skeleton,
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(20, 24, 20, 32),
      children: const [
        Center(child: _Bone(height: 104, width: 104, radius: 52)),
        SizedBox(height: 16),
        Center(child: _Bone(height: 28, width: 180, radius: 8)),
        SizedBox(height: 10),
        Center(child: _Bone(height: 16, width: 120, radius: 8)),
        SizedBox(height: 28),
        _Bone(height: 18, width: 140, radius: 8),
        SizedBox(height: 10),
        _Bone(height: 72, radius: 12),
        SizedBox(height: 20),
        _Bone(height: 18, width: 80, radius: 8),
        SizedBox(height: 10),
        _Bone(height: 96, radius: 12),
      ],
    );
  }
}

class _DoctorCardBone extends StatelessWidget {
  const _DoctorCardBone();

  @override
  Widget build(BuildContext context) {
    return const Padding(
      padding: EdgeInsets.only(bottom: 12),
      child: _Bone(height: 76, radius: 16),
    );
  }
}

class _Bone extends StatelessWidget {
  const _Bone({required this.height, this.width, required this.radius});

  final double height;
  final double? width;
  final double radius;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: height,
      width: width,
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surfaceContainerHighest,
        borderRadius: BorderRadius.circular(radius),
      ),
    );
  }
}

import 'dart:typed_data';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/doctors_repository.dart';
import '../domain/doctor.dart';

final doctorsSearchQueryProvider = StateProvider<String>((ref) => '');

final doctorsListProvider = FutureProvider.autoDispose<List<Doctor>>((ref) {
  final query = ref.watch(doctorsSearchQueryProvider);
  return ref.watch(doctorsRepositoryProvider).listDoctors(query: query);
});

final doctorProvider = FutureProvider.autoDispose.family<Doctor, String>((
  ref,
  id,
) {
  return ref.watch(doctorsRepositoryProvider).getDoctor(id);
});

final doctorPhotoProvider = FutureProvider.autoDispose
    .family<Uint8List?, String>((ref, id) {
      return ref.watch(doctorsRepositoryProvider).loadPhoto(id);
    });

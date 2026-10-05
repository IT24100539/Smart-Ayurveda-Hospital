import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/features/doctors/domain/doctor.dart';

void main() {
  test('parses a physician and leaves a missing rating null', () {
    final doctor = Doctor.fromJson({
      'id': 'd1',
      'name': 'Anjali Perera',
      'specialty': 'Kayachikitsa',
      'qualifications': 'BAMS',
      'bio': '  Consults on vikriti.  ',
      'isActive': true,
      'isSample': false,
      'hasPhoto': false,
      'photoUrl': null,
    });

    expect(doctor.id, 'd1');
    expect(doctor.name, 'Anjali Perera');
    expect(doctor.bio, 'Consults on vikriti.');
    expect(doctor.rating, isNull);
    expect(doctor.ratingCount, isNull);
    expect(doctor.showsRating, isFalse);
    expect(doctor.initials, 'AP');
  });

  test('keeps a real feedback average and ignores a zero count', () {
    final rated = Doctor.fromJson({
      'id': 'd2',
      'name': 'Nimal',
      'specialty': 'Shalya Tantra',
      'qualifications': 'MD (Ayu)',
      'bio': null,
      'isActive': true,
      'isSample': true,
      'hasPhoto': true,
      'photoUrl': '/api/doctors/d2/photo',
      'rating': 4.5,
      'ratingCount': 2,
    });

    expect(rated.showsRating, isTrue);
    expect(rated.rating, 4.5);
    expect(rated.ratingCount, 2);
    expect(formatDoctorRating(rated.rating!), '4.5');
    expect(rated.initials, 'N');

    final emptyCount = Doctor.fromJson({
      'id': 'd3',
      'name': 'Sushruta',
      'specialty': 'Kayachikitsa',
      'qualifications': 'BAMS',
      'isActive': true,
      'isSample': false,
      'hasPhoto': false,
      'rating': 0,
      'ratingCount': 0,
    });
    expect(emptyCount.showsRating, isFalse);
  });

  test('builds initials from the first and last name', () {
    expect(initialsFor('Meera Nair'), 'MN');
    expect(initialsFor('  '), '?');
    expect(initialsFor('නිමල් පෙරේරා'), 'නප');
  });
}

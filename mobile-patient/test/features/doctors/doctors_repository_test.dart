import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:patient_app/src/features/doctors/data/doctors_repository.dart';

void main() {
  test('lists active physicians from /doctors and follows pages', () async {
    final adapter = _ScriptedAdapter((options) {
      expect(options.path, '/doctors');
      expect(options.queryParameters['activeOnly'], true);
      expect(options.queryParameters['pageSize'], 100);
      final page = options.queryParameters['page'];
      if (page == 1) {
        return jsonEncode({
          'items': [
            _doctorJson(id: 'd1', name: 'Anjali Perera', isActive: true),
            _doctorJson(id: 'd-inactive', name: 'Retired', isActive: false),
          ],
          'totalCount': 3,
          'page': 1,
          'pageSize': 100,
        });
      }
      expect(options.queryParameters['query'], 'kaya');
      return jsonEncode({
        'items': [
          _doctorJson(id: 'd2', name: 'Nimal Silva', isActive: true),
        ],
        'totalCount': 3,
        'page': 2,
        'pageSize': 100,
      });
    });
    final repository = DioDoctorsRepository(_dio(adapter));

    final doctors = await repository.listDoctors(query: '  kaya  ');

    expect(adapter.calls, hasLength(2));
    expect(doctors.map((doctor) => doctor.id), ['d1', 'd2']);
    expect(doctors.every((doctor) => doctor.isActive), isTrue);
  });

  test('loads one physician and an authorized portrait', () async {
    const png = <int>[1, 2, 3, 4];
    final adapter = _ScriptedAdapter((options) {
      if (options.path == '/doctors/d2/photo') {
        expect(options.responseType, ResponseType.bytes);
        return png;
      }
      expect(options.path, '/doctors/d2');
      return jsonEncode(
        _doctorJson(
          id: 'd2',
          name: 'Nimal Silva',
          isActive: true,
          hasPhoto: true,
          rating: 4.5,
          ratingCount: 2,
        ),
      );
    }, bytesPaths: {'/doctors/d2/photo'});
    final repository = DioDoctorsRepository(_dio(adapter));

    final doctor = await repository.getDoctor('d2');
    final photo = await repository.loadPhoto('d2');

    expect(doctor.showsRating, isTrue);
    expect(photo, Uint8List.fromList(png));
  });

  test('a missing portrait falls back to null instead of a placeholder', () async {
    final adapter = _FailingAdapter();
    final repository = DioDoctorsRepository(_dio(adapter));

    expect(await repository.loadPhoto('missing'), isNull);
  });
}

Dio _dio(_Adapter adapter) {
  final dio = Dio(BaseOptions(baseUrl: 'https://localhost:7443/api'));
  dio.httpClientAdapter = adapter;
  return dio;
}

Map<String, dynamic> _doctorJson({
  required String id,
  required String name,
  required bool isActive,
  bool hasPhoto = false,
  double? rating,
  int? ratingCount,
}) {
  return {
    'id': id,
    'name': name,
    'specialty': 'Kayachikitsa',
    'qualifications': 'BAMS',
    'bio': null,
    'isActive': isActive,
    'isSample': false,
    'hasPhoto': hasPhoto,
    'photoUrl': hasPhoto ? '/api/doctors/$id/photo' : null,
    'rating': ?rating,
    'ratingCount': ?ratingCount,
  };
}

typedef _Responder = Object Function(RequestOptions options);

class _ScriptedAdapter extends _Adapter {
  _ScriptedAdapter(this._respond, {this.bytesPaths = const {}});

  final _Responder _respond;
  final Set<String> bytesPaths;
  final calls = <RequestOptions>[];

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    calls.add(options);
    final body = _respond(options);
    if (bytesPaths.contains(options.path)) {
      return ResponseBody.fromBytes(
        body as List<int>,
        200,
        headers: {
          Headers.contentTypeHeader: ['image/jpeg'],
        },
      );
    }
    return ResponseBody.fromString(
      body as String,
      200,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );
  }
}

class _FailingAdapter extends _Adapter {
  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    return ResponseBody.fromString(
      '{"title":"Not found"}',
      404,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );
  }
}

abstract class _Adapter implements HttpClientAdapter {
  @override
  void close({bool force = false}) {}
}

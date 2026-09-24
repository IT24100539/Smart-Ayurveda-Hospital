class Ward {
  const Ward({
    required this.id,
    required this.name,
    required this.totalCapacity,
    required this.occupiedBeds,
  });

  final String id;
  final String name;
  final int totalCapacity;
  final int occupiedBeds;

  int get availableBeds =>
      (totalCapacity - occupiedBeds).clamp(0, totalCapacity).toInt();
  double get occupancy => totalCapacity == 0
      ? 0
      : (occupiedBeds / totalCapacity).clamp(0, 1).toDouble();

  factory Ward.fromJson(Map<String, dynamic> json) => Ward(
    id: json['id'].toString(),
    name: json['name']?.toString() ?? 'Ward',
    totalCapacity: (json['totalCapacity'] as num?)?.toInt() ?? 0,
    occupiedBeds: (json['occupiedBeds'] as num?)?.toInt() ?? 0,
  );
}

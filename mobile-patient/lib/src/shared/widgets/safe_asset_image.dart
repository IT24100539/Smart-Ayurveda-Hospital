import 'package:flutter/material.dart';

/// [Image.asset] with a muted icon panel when the file is missing or fails to decode.
class SafeAssetImage extends StatelessWidget {
  const SafeAssetImage({
    required this.asset,
    this.height,
    this.width,
    this.fit = BoxFit.cover,
    this.fallbackIcon = Icons.spa_outlined,
    super.key,
  });

  final String asset;
  final double? height;
  final double? width;
  final BoxFit fit;
  final IconData fallbackIcon;

  @override
  Widget build(BuildContext context) {
    return Image.asset(
      asset,
      height: height,
      width: width,
      fit: fit,
      errorBuilder: (context, error, stackTrace) => Container(
        height: height,
        width: width,
        color: Theme.of(context).colorScheme.primaryContainer,
        alignment: Alignment.center,
        child: Icon(
          fallbackIcon,
          size: 48,
          color: Theme.of(context).colorScheme.primary,
        ),
      ),
    );
  }
}

import 'package:flutter/material.dart';

/// Same vector mark as web/brand.svg and the Android launcher icon.
class AppLogo extends StatelessWidget {
  final double size;
  const AppLogo({super.key, this.size = 44});
  @override
  Widget build(BuildContext context) => Semantics(
    label: 'Logo de RutaSmart',
    image: true,
    child: CustomPaint(size: Size.square(size), painter: const _LogoPainter()),
  );
}

class _LogoPainter extends CustomPainter {
  const _LogoPainter();
  @override
  void paint(Canvas canvas, Size size) {
    canvas.scale(size.width / 96, size.height / 96);
    canvas.drawRRect(
      RRect.fromRectAndRadius(
        const Rect.fromLTWH(0, 0, 96, 96),
        const Radius.circular(24),
      ),
      Paint()..color = const Color(0xff126356),
    );
    final path =
        Path()
          ..moveTo(30, 27)
          ..lineTo(30, 63)
          ..cubicTo(30, 75, 48, 75, 48, 63)
          ..lineTo(48, 33)
          ..cubicTo(48, 21, 66, 21, 66, 33)
          ..lineTo(66, 69);
    canvas.drawPath(
      path,
      Paint()
        ..color = const Color(0xfff2f4ee)
        ..style = PaintingStyle.stroke
        ..strokeWidth = 6
        ..strokeCap = StrokeCap.round,
    );
    canvas.drawCircle(
      const Offset(30, 27),
      7,
      Paint()..color = const Color(0xffcbe4ad),
    );
    canvas.drawCircle(
      const Offset(66, 69),
      7,
      Paint()..color = const Color(0xffebbf76),
    );
  }

  @override
  bool shouldRepaint(covariant _LogoPainter oldDelegate) => false;
}

import 'package:flutter/material.dart';

/// Brand illustration, deliberately independent of route geometry.
class JourneyHero extends StatelessWidget {
  const JourneyHero({super.key});

  @override
  Widget build(BuildContext context) => Semantics(
    label:
        'Ilustración decorativa de un camino. Cada viaje, un poco más claro.',
    image: true,
    child: ExcludeSemantics(
      child: ClipRRect(
        borderRadius: BorderRadius.circular(24),
        child: Container(
          height: 310,
          width: double.infinity,
          decoration: const BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [Color(0xff123e36), Color(0xff24725d)],
            ),
          ),
          child: Stack(
            children: [
              Positioned.fill(child: CustomPaint(painter: _JourneyPainter())),
              const Padding(
                padding: EdgeInsets.all(28),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Icon(
                          Icons.explore_outlined,
                          color: Color(0xffcbe4ad),
                          size: 19,
                        ),
                        SizedBox(width: 8),
                        Text(
                          'ANTES DE SALIR',
                          style: TextStyle(
                            color: Color(0xffcbe4ad),
                            fontSize: 10,
                            letterSpacing: 2,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      ],
                    ),
                    SizedBox(height: 22),
                    Text(
                      'Cada viaje,\nun poco más claro.',
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: 30,
                        height: 1.12,
                        fontWeight: FontWeight.w700,
                        letterSpacing: -.8,
                      ),
                    ),
                    Spacer(),
                    Text(
                      'RECORRIDO · TIEMPO · COMBUSTIBLE',
                      style: TextStyle(
                        color: Color(0xffd5e8d6),
                        fontSize: 9,
                        letterSpacing: 1.3,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    SizedBox(height: 8),
                    Text(
                      'Tu próxima salida empieza con información.',
                      style: TextStyle(color: Color(0xffd5e8d6), fontSize: 12),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    ),
  );
}

class _JourneyPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final contour =
        Paint()
          ..color = const Color(0x187fe4b4)
          ..style = PaintingStyle.stroke
          ..strokeWidth = 1;
    for (var i = 0; i < 8; i++) {
      canvas.drawOval(
        Rect.fromCenter(
          center: Offset(size.width * .96, size.height * .55),
          width: 80 + i * 45,
          height: 110 + i * 42,
        ),
        contour,
      );
    }
    final path =
        Path()
          ..moveTo(size.width * .72, size.height + 20)
          ..cubicTo(
            size.width * .44,
            size.height * .65,
            size.width * 1.02,
            size.height * .8,
            size.width * .91,
            size.height * .39,
          )
          ..cubicTo(
            size.width * .86,
            size.height * .23,
            size.width * .78,
            size.height * .24,
            size.width * .91,
            -20,
          );
    canvas.drawPath(
      path,
      Paint()
        ..color = const Color(0x267fe4b4)
        ..style = PaintingStyle.stroke
        ..strokeWidth = 27,
    );
    canvas.drawPath(
      path,
      Paint()
        ..color = const Color(0xffb7d694)
        ..style = PaintingStyle.stroke
        ..strokeWidth = 2.5,
    );
    final point = Offset(size.width * .91, size.height * .39);
    canvas.drawCircle(point, 13, Paint()..color = const Color(0xff163f36));
    canvas.drawCircle(point, 8, Paint()..color = const Color(0xffebbf76));
    canvas.drawCircle(point, 3, Paint()..color = Colors.white);
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}

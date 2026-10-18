using System.Windows;
using System.Windows.Media;

namespace Agc.Wpf.Scene;

/// <summary>
/// 지형과 배경 소품을 그리는 유틸리티: 달 표면(분화구/바위/지평선), 우주인과 성조기, 바다(파도/함정), 발사 시설.
/// 모두 코드로 그린 벡터이며, 위치/크기는 호출 쪽이 정한다. 상태 없는 정적 유틸리티, UI 스레드 전용.
/// </summary>
internal static class TerrainArt
{
    private const int CraterCount = 90;
    private const int BoulderCount = 140;
    private const int TerrainSeed = 1107;

    private static readonly (double X, double Depth, double Size, double Dark)[] Craters = BuildCraters();
    private static readonly (double X, double Depth, double Size)[] Boulders = BuildBoulders();

    // ---- 달 표면 ----

    /// <summary>
    /// 달 표면을 그린다(옆에서 본 시점): 지평선 아래 전체를 회색 지면으로 채우고 원근에 따라 분화구와 바위를 배치한다.
    /// </summary>
    /// <param name="horizonY">지평선의 y 좌표.</param>
    /// <param name="scrollPixels">가로 스크롤(우주선이 지나가며 지형이 흘러가는 효과).</param>
    /// <param name="zoom">확대 배율(고도가 낮을수록 지형이 크게 보인다).</param>
    public static void DrawLunarGround(DrawingContext dc, Size size, double horizonY, double scrollPixels, double zoom)
    {
        var groundHeight = size.Height - horizonY;

        // 지평선: 양끝이 살짝 내려가는 완만한 곡선(달의 곡률)
        var ground = new StreamGeometry();
        using (var ctx = ground.Open())
        {
            ctx.BeginFigure(new Point(0, horizonY + 6), true, true);
            ctx.BezierTo(
                new Point(size.Width * 0.3, horizonY - 5), new Point(size.Width * 0.7, horizonY - 5),
                new Point(size.Width, horizonY + 6), true, false);
            ctx.LineTo(new Point(size.Width, size.Height), true, false);
            ctx.LineTo(new Point(0, size.Height), true, false);
        }

        ground.Freeze();
        var brush = new LinearGradientBrush(
            [
                new GradientStop(Color.FromRgb(0x2B, 0x2B, 0x2C), 0.0),
                new GradientStop(Color.FromRgb(0x55, 0x54, 0x52), 0.25),
                new GradientStop(Color.FromRgb(0x8A, 0x88, 0x84), 0.7),
                new GradientStop(Color.FromRgb(0xA9, 0xA6, 0xA0), 1.0),
            ],
            new Point(0, 0), new Point(0, 1));
        brush.Freeze();
        dc.DrawGeometry(brush, null, ground);

        // 멀리서부터(깊이 큰 것부터) 가까운 쪽 순서로: 분화구와 바위
        foreach (var crater in Craters.OrderByDescending(c => c.Depth))
        {
            DrawCrater(dc, size, horizonY, groundHeight, scrollPixels, zoom, crater);
        }

        foreach (var boulder in Boulders.OrderByDescending(b => b.Depth))
        {
            var depth = boulder.Depth;
            var screenY = horizonY + Math.Pow(depth, 1.9) * groundHeight;
            var scale = 0.25 + depth * 1.6;
            var x = Wrap(boulder.X * size.Width * 3 - scrollPixels * (0.25 + depth), size.Width * 3) - size.Width;
            var radius = boulder.Size * scale * zoom;
            if (x < -radius || x > size.Width + radius)
            {
                continue;
            }

            dc.DrawEllipse(Frozen(new SolidColorBrush(Color.FromArgb(0xB0, 0x24, 0x24, 0x26))), null,
                new Point(x + radius * 0.45, screenY + radius * 0.25), radius * 1.25, radius * 0.40);
            dc.DrawEllipse(Frozen(new SolidColorBrush(Color.FromRgb(0x6A, 0x69, 0x66))), null,
                new Point(x, screenY), radius, radius * 0.66);
            dc.DrawEllipse(Frozen(new SolidColorBrush(Color.FromArgb(0x90, 0xC8, 0xC6, 0xC0))), null,
                new Point(x - radius * 0.3, screenY - radius * 0.2), radius * 0.45, radius * 0.25);
        }

        // 지평선의 옅은 빛 번짐(먼지/원근 안개)
        var haze = new LinearGradientBrush(
            [
                new GradientStop(Color.FromArgb(0x50, 0xB0, 0xAE, 0xA8), 0.0),
                new GradientStop(Color.FromArgb(0x00, 0xB0, 0xAE, 0xA8), 1.0),
            ],
            new Point(0, 0), new Point(0, 1));
        haze.Freeze();
        dc.DrawRectangle(haze, null, new Rect(0, horizonY - 2, size.Width, groundHeight * 0.18));
    }

    private static void DrawCrater(
        DrawingContext dc, Size size, double horizonY, double groundHeight, double scroll, double zoom,
        (double X, double Depth, double Size, double Dark) crater)
    {
        var depth = crater.Depth;
        var screenY = horizonY + Math.Pow(depth, 1.9) * groundHeight + 4;
        var scale = 0.14 + depth * 0.90;
        var rx = crater.Size * scale * zoom;
        var ry = rx * (0.10 + 0.32 * depth);
        var x = Wrap(crater.X * size.Width * 3 - scroll * (0.25 + depth), size.Width * 3) - size.Width;
        if (x < -rx * 1.5 || x > size.Width + rx * 1.5)
        {
            return;
        }

        // 바깥 림(밝은 테두리) → 어두운 안쪽 → 그늘진 한쪽 벽
        var rim = Frozen(new SolidColorBrush(Color.FromArgb(0x42, 0xD8, 0xD6, 0xD0)));
        var floor = Frozen(new SolidColorBrush(Color.FromArgb((byte)(0x60 + 0x50 * crater.Dark), 0x20, 0x20, 0x22)));
        var wall = Frozen(new SolidColorBrush(Color.FromArgb(0x90, 0x10, 0x10, 0x12)));
        dc.DrawEllipse(rim, null, new Point(x, screenY), rx * 1.12, ry * 1.18);
        dc.DrawEllipse(floor, null, new Point(x, screenY + ry * 0.05), rx, ry);
        dc.DrawEllipse(wall, null, new Point(x - rx * 0.12, screenY - ry * 0.18), rx * 0.8, ry * 0.55);
    }

    // ---- 우주인과 성조기 ----

    /// <summary>우주복 입은 우주인(옆모습). 로컬 원점은 발 밑 중앙, 높이 height만큼 위로(−y) 그려진다.</summary>
    public static void DrawAstronaut(DrawingContext dc, double height)
    {
        var suit = Frozen(new LinearGradientBrush(
            [
                new GradientStop(Color.FromRgb(0xB5, 0xB9, 0xC0), 0.0),
                new GradientStop(Color.FromRgb(0xFF, 0xFF, 0xFF), 0.45),
                new GradientStop(Color.FromRgb(0xA0, 0xA5, 0xAE), 1.0),
            ],
            new Point(0, 0), new Point(1, 0)));
        var visor = Frozen(new RadialGradientBrush(
            [
                new GradientStop(Color.FromRgb(0xFF, 0xE9, 0xA8), 0.0),
                new GradientStop(Color.FromRgb(0xC8, 0x92, 0x24), 0.55),
                new GradientStop(Color.FromRgb(0x5A, 0x3A, 0x0C), 1.0),
            ])
        {
            GradientOrigin = new Point(0.3, 0.3),
        });
        var line = Frozen(new Pen(new SolidColorBrush(Color.FromArgb(0x70, 0, 0, 0)), 0.8));
        var h = height;

        // 다리와 장화
        dc.DrawRoundedRectangle(suit, line, new Rect(-h * 0.085, -h * 0.40, h * 0.075, h * 0.40), h * 0.03, h * 0.03);
        dc.DrawRoundedRectangle(suit, line, new Rect(h * 0.010, -h * 0.40, h * 0.075, h * 0.40), h * 0.03, h * 0.03);
        dc.DrawRoundedRectangle(Frozen(new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x58))), line,
            new Rect(-h * 0.10, -h * 0.04, h * 0.115, h * 0.04), h * 0.015, h * 0.015);
        dc.DrawRoundedRectangle(Frozen(new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x58))), line,
            new Rect(h * 0.0, -h * 0.04, h * 0.115, h * 0.04), h * 0.015, h * 0.015);
        // 몸통과 생명유지 배낭(PLSS)
        dc.DrawRoundedRectangle(suit, line, new Rect(-h * 0.13, -h * 0.72, h * 0.26, h * 0.34), h * 0.05, h * 0.05);
        dc.DrawRoundedRectangle(suit, line, new Rect(-h * 0.20, -h * 0.70, h * 0.10, h * 0.30), h * 0.03, h * 0.03);
        // 팔
        dc.DrawLine(Frozen(new Pen(suit, h * 0.075) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }),
            new Point(h * 0.10, -h * 0.64), new Point(h * 0.20, -h * 0.50));
        // 헬멧과 금빛 바이저
        dc.DrawEllipse(suit, line, new Point(h * 0.02, -h * 0.82), h * 0.125, h * 0.125);
        dc.DrawEllipse(visor, line, new Point(h * 0.07, -h * 0.82), h * 0.088, h * 0.095);
    }

    /// <summary>성조기: 깃대 + 가로대로 펴진 깃발(줄무늬와 파란 캔턴). 로컬 원점은 깃대 밑동.</summary>
    public static void DrawFlag(DrawingContext dc, double height)
    {
        var pole = Frozen(new Pen(new SolidColorBrush(Color.FromRgb(0xD8, 0xD8, 0xDA)), Math.Max(1.2, height * 0.012)));
        dc.DrawLine(pole, new Point(0, 0), new Point(0, -height));
        var flagWidth = height * 0.62;
        var flagHeight = height * 0.38;
        var top = -height + height * 0.02;
        // 줄무늬 13개(적/백)
        for (var i = 0; i < 13; i++)
        {
            var stripe = i % 2 == 0 ? Color.FromRgb(0xB2, 0x22, 0x34) : Color.FromRgb(0xF5, 0xF5, 0xF5);
            dc.DrawRectangle(Frozen(new SolidColorBrush(stripe)), null,
                new Rect(0, top + flagHeight * i / 13.0, flagWidth, flagHeight / 13.0 + 0.4));
        }

        // 파란 캔턴과 별(작은 점들)
        var cantonHeight = flagHeight * 7 / 13.0;
        var cantonWidth = flagWidth * 0.40;
        dc.DrawRectangle(Frozen(new SolidColorBrush(Color.FromRgb(0x3C, 0x3B, 0x6E))), null, new Rect(0, top, cantonWidth, cantonHeight));
        var star = Frozen(new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF)));
        for (var row = 0; row < 5; row++)
        {
            for (var col = 0; col < 6; col++)
            {
                dc.DrawEllipse(star, null,
                    new Point(cantonWidth * (col + 0.7) / 6.4, top + cantonHeight * (row + 0.7) / 5.4), height * 0.007, height * 0.007);
            }
        }

        dc.DrawRectangle(Frozen(new SolidColorBrush(Color.FromArgb(0x55, 0, 0, 0))), null, new Rect(0, top, flagWidth, flagHeight * 0.04));
    }

    // ---- 바다 ----

    /// <summary>바다: 수평선 아래를 청록 그라디언트로 채우고 햇빛 반사와 파도결을 그린다.</summary>
    public static void DrawOcean(DrawingContext dc, Size size, double horizonY, double time)
    {
        var sea = new LinearGradientBrush(
            [
                new GradientStop(Color.FromRgb(0x2E, 0x7F, 0xA8), 0.0),
                new GradientStop(Color.FromRgb(0x12, 0x5A, 0x86), 0.35),
                new GradientStop(Color.FromRgb(0x08, 0x2F, 0x56), 1.0),
            ],
            new Point(0, 0), new Point(0, 1));
        sea.Freeze();
        dc.DrawRectangle(sea, null, new Rect(0, horizonY, size.Width, size.Height - horizonY));

        // 수평선 가까이의 햇빛 반사: 타원형 방사 그라디언트(사각형 경계가 보이지 않게)
        var glint = new RadialGradientBrush(
            [
                new GradientStop(Color.FromArgb(0xA0, 0xFF, 0xFF, 0xF0), 0.0),
                new GradientStop(Color.FromArgb(0x40, 0xFF, 0xFF, 0xF0), 0.5),
                new GradientStop(Color.FromArgb(0x00, 0xFF, 0xFF, 0xF0), 1.0),
            ]);
        glint.Freeze();
        dc.DrawEllipse(glint, null, new Point(size.Width * 0.5, horizonY + (size.Height - horizonY) * 0.06), size.Width * 0.34, (size.Height - horizonY) * 0.16);

        // 파도결: 수평선에서 멀수록 굵고 간격이 넓은 사인 곡선
        var random = new Random(77);
        for (var row = 0; row < 16; row++)
        {
            var t = (row + 1) / 17.0;
            var y = horizonY + Math.Pow(t, 1.8) * (size.Height - horizonY);
            var amplitude = 1.0 + 6.0 * t;
            var wavelength = 30 + 150 * t;
            var phase = time * (0.6 + t) + random.NextDouble() * 10;
            var pen = Frozen(new Pen(new SolidColorBrush(Color.FromArgb((byte)(40 + 70 * t), 0xD8, 0xF0, 0xFF)), 0.6 + 1.4 * t));
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(new Point(0, y), false, false);
                for (var x = 0.0; x <= size.Width; x += 14)
                {
                    ctx.LineTo(new Point(x, y + amplitude * Math.Sin(x / wavelength * Math.PI * 2 + phase)), true, false);
                }
            }

            geometry.Freeze();
            dc.DrawGeometry(null, pen, geometry);
        }
    }

    /// <summary>회수 함정(USS Hornet 느낌의 항공모함 실루엣). 로컬 원점은 흘수선 중앙, length는 전체 길이.</summary>
    public static void DrawCarrier(DrawingContext dc, double length)
    {
        var hull = Frozen(new LinearGradientBrush(
            [
                new GradientStop(Color.FromRgb(0x8A, 0x92, 0x9E), 0.0),
                new GradientStop(Color.FromRgb(0x4A, 0x50, 0x5A), 1.0),
            ],
            new Point(0, 0), new Point(0, 1)));
        var h = length * 0.07;
        var body = new StreamGeometry();
        using (var ctx = body.Open())
        {
            ctx.BeginFigure(new Point(-length / 2, -h * 0.4), true, true);
            ctx.LineTo(new Point(length / 2, -h * 0.4), true, false);
            ctx.LineTo(new Point(length / 2 - length * 0.05, h * 0.2), true, false);
            ctx.LineTo(new Point(-length / 2 + length * 0.02, h * 0.2), true, false);
        }

        body.Freeze();
        dc.DrawGeometry(hull, null, body);
        // 비행갑판 + 함교
        dc.DrawRectangle(Frozen(new SolidColorBrush(Color.FromRgb(0x30, 0x34, 0x3B))), null,
            new Rect(-length / 2, -h * 0.55, length, h * 0.16));
        dc.DrawRectangle(Frozen(new SolidColorBrush(Color.FromRgb(0x5B, 0x62, 0x6E))), null,
            new Rect(length * 0.08, -h * 1.5, length * 0.10, h * 0.95));
        dc.DrawRectangle(Frozen(new SolidColorBrush(Color.FromRgb(0x3C, 0x41, 0x4A))), null,
            new Rect(length * 0.12, -h * 2.0, length * 0.015, h * 0.5));
    }

    // ---- 발사 시설 ----

    /// <summary>발사대 탑(서비스 구조물)의 격자 실루엣. 로컬 원점은 탑 밑 중앙, 높이 height.</summary>
    public static void DrawLaunchTower(DrawingContext dc, double height)
    {
        var width = height * 0.10;
        var pen = Frozen(new Pen(new SolidColorBrush(Color.FromRgb(0x8E, 0x3B, 0x33)), Math.Max(1.2, height * 0.006)));
        dc.DrawLine(pen, new Point(-width / 2, 0), new Point(-width / 2, -height));
        dc.DrawLine(pen, new Point(width / 2, 0), new Point(width / 2, -height));
        for (var i = 0; i < 18; i++)
        {
            var y0 = -height * i / 18.0;
            var y1 = -height * (i + 1) / 18.0;
            dc.DrawLine(pen, new Point(-width / 2, y0), new Point(width / 2, y1));
            dc.DrawLine(pen, new Point(width / 2, y0), new Point(-width / 2, y1));
        }

        // 가로 지지 암(로켓 쪽으로 뻗은 서비스 암)
        for (var i = 1; i <= 5; i++)
        {
            var y = -height * i / 6.0;
            dc.DrawLine(pen, new Point(width / 2, y), new Point(width / 2 + height * 0.07, y));
        }
    }

    /// <summary>구름처럼 퍼지는 연기/먼지 덩어리 여러 개를 한 점 주위에 그린다.</summary>
    public static void DrawSmokePuffs(DrawingContext dc, Point center, double spread, double radius, byte alpha, int seed)
    {
        var random = new Random(seed);
        for (var i = 0; i < 14; i++)
        {
            var angle = random.NextDouble() * Math.PI;
            var distance = random.NextDouble() * spread;
            var r = radius * (0.5 + random.NextDouble());
            var p = new Point(center.X + Math.Cos(angle) * distance * (random.Next(2) == 0 ? -1 : 1), center.Y - Math.Sin(angle) * distance * 0.4);
            var brush = new RadialGradientBrush(
                [
                    new GradientStop(Color.FromArgb(alpha, 0xF2, 0xF2, 0xF0), 0.0),
                    new GradientStop(Color.FromArgb((byte)(alpha / 2), 0xC8, 0xC8, 0xC6), 0.6),
                    new GradientStop(Color.FromArgb(0, 0xC8, 0xC8, 0xC6), 1.0),
                ]);
            brush.Freeze();
            dc.DrawEllipse(brush, null, p, r, r * 0.75);
        }
    }

    // ---- 내부 도우미 ----

    private static (double X, double Depth, double Size, double Dark)[] BuildCraters()
    {
        var random = new Random(TerrainSeed);
        var craters = new (double, double, double, double)[CraterCount];
        for (var i = 0; i < CraterCount; i++)
        {
            // 먼 곳(깊이 작음)에 더 많이 배치되도록 제곱 분포
            var depth = Math.Pow(random.NextDouble(), 1.5) * 0.98 + 0.02;
            craters[i] = (random.NextDouble(), depth, 14 + 70 * Math.Pow(random.NextDouble(), 2.2), random.NextDouble());
        }

        return craters;
    }

    private static (double X, double Depth, double Size)[] BuildBoulders()
    {
        var random = new Random(TerrainSeed + 1);
        var boulders = new (double, double, double)[BoulderCount];
        for (var i = 0; i < BoulderCount; i++)
        {
            boulders[i] = (random.NextDouble(), 0.05 + 0.95 * Math.Pow(random.NextDouble(), 1.2), 2.0 + 7.0 * random.NextDouble());
        }

        return boulders;
    }

    private static double Wrap(double value, double max)
    {
        var r = value % max;
        return r < 0 ? r + max : r;
    }

    private static T Frozen<T>(T freezable)
        where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}

using System.Windows;
using System.Windows.Media;

namespace Agc.Wpf.Scene;

/// <summary>
/// 결정적(고정 시드) 별밭 + 은하수 띠. 별의 위치/크기/색은 한 번만 만들어 두고 매 프레임 그리기만 한다.
/// - 위치는 0~1 정규화 좌표라 화면 크기가 바뀌어도 같은 하늘이 보인다.
/// - 색은 청백색~황색~적색의 항성 색 분포를 흉내 낸다.
/// - 상태 없는 정적 유틸리티, UI 스레드 전용.
/// </summary>
internal static class StarField
{
    private const int StarCount = 520;
    private const int Seed = 1969;
    private const int BrushLevels = 8;

    private static readonly (double X, double Y, double Radius, double Brightness, int Tint)[] Stars = BuildStars();
    private static readonly Brush[][] TintBrushes = BuildTintBrushes();

    /// <summary>
    /// 별밭을 그린다.
    /// </summary>
    /// <param name="visibility">0(안 보임: 낮 하늘)~1(완전히 보임).</param>
    /// <param name="offsetX">시차 효과를 위한 가로 이동(픽셀, 별은 천천히 흘러간다).</param>
    /// <param name="offsetY">세로 이동(픽셀).</param>
    public static void Draw(DrawingContext dc, Size size, double visibility, double offsetX = 0, double offsetY = 0)
    {
        if (visibility <= 0.01)
        {
            return;
        }

        DrawMilkyWay(dc, size, visibility);
        foreach (var star in Stars)
        {
            var x = Wrap(star.X * size.Width + offsetX * (0.3 + star.Radius * 0.3), size.Width);
            var y = Wrap(star.Y * size.Height + offsetY * (0.3 + star.Radius * 0.3), size.Height);
            var level = (int)Math.Clamp(star.Brightness * visibility * (BrushLevels - 1), 0, BrushLevels - 1);
            dc.DrawEllipse(TintBrushes[star.Tint][level], null, new Point(x, y), star.Radius, star.Radius);
        }
    }

    private static double Wrap(double value, double max)
    {
        var r = value % max;
        return r < 0 ? r + max : r;
    }

    /// <summary>은하수: 대각선 방향의 옅고 긴 빛 띠(방사형 그라디언트를 눌러 늘인 것).</summary>
    private static void DrawMilkyWay(DrawingContext dc, Size size, double visibility)
    {
        var brush = new RadialGradientBrush(
            [
                new GradientStop(Color.FromArgb((byte)(34 * visibility), 0xB8, 0xC4, 0xFF), 0.0),
                new GradientStop(Color.FromArgb((byte)(14 * visibility), 0x8A, 0x9C, 0xE8), 0.55),
                new GradientStop(Color.FromArgb(0, 0x60, 0x70, 0xC0), 1.0),
            ]);
        brush.Freeze();
        dc.PushTransform(new TransformGroup
        {
            Children =
            [
                new ScaleTransform(1.0, 0.16),
                new RotateTransform(-24),
                new TranslateTransform(size.Width * 0.52, size.Height * 0.40),
            ],
        });
        var radius = Math.Max(size.Width, size.Height) * 0.9;
        dc.DrawEllipse(brush, null, new Point(0, 0), radius, radius);
        dc.Pop();
    }

    private static (double X, double Y, double Radius, double Brightness, int Tint)[] BuildStars()
    {
        var random = new Random(Seed);
        var stars = new (double, double, double, double, int)[StarCount];
        for (var i = 0; i < StarCount; i++)
        {
            // 밝은 별은 드물고 어두운 별이 많다(제곱 분포).
            var b = Math.Pow(random.NextDouble(), 2.6);
            var radius = 0.45 + 1.35 * b;
            var tint = random.NextDouble() switch
            {
                < 0.55 => 0,
                < 0.80 => 1,
                < 0.93 => 2,
                _ => 3,
            };
            stars[i] = (random.NextDouble(), random.NextDouble(), radius, 0.25 + 0.75 * b, tint);
        }

        return stars;
    }

    /// <summary>색조(청백/백/황/적)별로 밝기 단계마다 동결된 브러시를 만들어 둔다.</summary>
    private static Brush[][] BuildTintBrushes()
    {
        var tints = new[]
        {
            Color.FromRgb(0xCF, 0xDC, 0xFF),
            Color.FromRgb(0xFF, 0xFF, 0xFF),
            Color.FromRgb(0xFF, 0xF0, 0xC8),
            Color.FromRgb(0xFF, 0xC8, 0xA0),
        };
        return tints.Select(t => Enumerable.Range(0, BrushLevels).Select(level =>
        {
            var alpha = (byte)(40 + 215.0 * level / (BrushLevels - 1));
            var brush = new SolidColorBrush(Color.FromArgb(alpha, t.R, t.G, t.B));
            brush.Freeze();
            return (Brush)brush;
        }).ToArray()).ToArray();
    }
}

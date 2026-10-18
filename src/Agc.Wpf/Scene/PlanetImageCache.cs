using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Agc.Wpf.Scene;

/// <summary>
/// 행성 이미지(PlanetRenderer 결과)를 매개변수 구간(버킷)별로 캐시한다. 구체 렌더링은 수십~수백 ms가 걸리므로
/// 화면 갱신마다 다시 만들지 않고, 각도/크기가 눈에 띄게 바뀔 때만 새로 만든다.
/// - 최근 사용한 일부만 유지한다(오래된 것부터 제거).
/// - UI 스레드 전용(잠금 없음). 렌더링 해상도는 상한을 두고, 더 크게 그려야 하면 확대해서 그린다.
/// </summary>
internal static class PlanetImageCache
{
    private const int MaxEntries = 10;
    private const int MaxRenderDiameter = 480;
    private const int MinRenderDiameter = 48;
    private const int DiameterStep = 16;
    private const double AngleStep = 3.0;
    private const double SunStep = 5.0;

    private static readonly Dictionary<(PlanetKind, int, int, int, int, int, int), BitmapSource> Entries = [];
    private static readonly Queue<(PlanetKind, int, int, int, int, int, int)> Order = new();

    /// <summary>
    /// 천체를 중심 center에 지름 diameter(화면 픽셀)로 그린다.
    /// 지름이 렌더 상한보다 크면 상한 해상도로 렌더링한 이미지를 확대해서 그린다.
    /// </summary>
    public static void Draw(
        DrawingContext dc, PlanetKind kind, Point center, double diameter,
        double lonDeg, double latDeg, double sunAzimuthDeg, double sunElevationDeg, double cloudShiftDeg = 0)
    {
        var renderDiameter = (int)Math.Clamp(Math.Round(diameter / DiameterStep) * DiameterStep, MinRenderDiameter, MaxRenderDiameter);
        var key = (
            kind,
            renderDiameter,
            Bucket(lonDeg, AngleStep),
            Bucket(latDeg, AngleStep),
            Bucket(sunAzimuthDeg, SunStep),
            Bucket(sunElevationDeg, SunStep),
            Bucket(cloudShiftDeg, AngleStep * 2));

        if (!Entries.TryGetValue(key, out var image))
        {
            image = PlanetRenderer.Render(
                kind, renderDiameter, key.Item3 * AngleStep, key.Item4 * AngleStep,
                key.Item5 * SunStep, key.Item6 * SunStep, key.Item7 * AngleStep * 2);
            Entries[key] = image;
            Order.Enqueue(key);
            while (Order.Count > MaxEntries)
            {
                Entries.Remove(Order.Dequeue());
            }
        }

        // 화면에 그릴 크기: 렌더 이미지의 한 변(원반 지름 × FrameScale)을 요청 지름에 맞춰 비례 확대
        var drawSide = diameter * PlanetRenderer.FrameScale;
        dc.DrawImage(image, new Rect(center.X - drawSide / 2, center.Y - drawSide / 2, drawSide, drawSide));
    }

    private static int Bucket(double value, double step)
    {
        return (int)Math.Round(value / step);
    }

    /// <summary>캐시를 비운다(테스트용).</summary>
    internal static void Clear()
    {
        Entries.Clear();
        Order.Clear();
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Agc.Mission;

namespace Agc.Wpf.Controls;

/// <summary>
/// 미션 단계를 단순한 벡터 도식으로 보여주는 캔버스: 별 배경, 기준 천체(지구/달), 우주선 마커.
/// 고도는 로그 스케일로 천체 표면 위 높이에 대응시키고, 가로 위치는 현재 단계 안의 진행률을 따른다.
/// 실제 NASA 사진 등을 쓰게 되면 이 컨트롤을 이미지 표시로 교체한다.
/// UI 스레드 전용이며 Owner는 MainWindow이다.
/// </summary>
public sealed class MissionIllustration : Canvas
{
    private const int StarCount = 90;
    private const int StarSeed = 11;
    private const double MaxAltitudeLogMeters = 9.0;
    private const double BodyRadiusRatio = 0.55;
    private const double BodyCenterYRatio = 1.15;
    private const double MarkerSize = 16;
    private const double MarkerMinX = 0.12;
    private const double MarkerXSpan = 0.76;

    private static Brush FrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static readonly Brush EarthBrush = FrozenBrush(Color.FromRgb(0x2E, 0x6F, 0xD8));
    private static readonly Brush MoonBrush = FrozenBrush(Color.FromRgb(0x9A, 0x9A, 0x9A));

    private readonly Ellipse _body = new();
    private readonly Polygon _marker = new()
    {
        Fill = Brushes.White,
        Points = [new Point(MarkerSize / 2, 0), new Point(MarkerSize, MarkerSize), new Point(0, MarkerSize)],
    };
    private readonly List<Ellipse> _stars = [];
    private MissionSnapshot? _last;

    public MissionIllustration()
    {
        ClipToBounds = true;
        Background = FrozenBrush(Color.FromRgb(0x05, 0x07, 0x10));
        SizeChanged += (_, _) => Rebuild();
    }

    /// <summary>현재 미션 상태에 맞춰 천체 색상과 마커 위치를 갱신한다.</summary>
    public void Update(MissionSnapshot snapshot, MissionTimeline timeline)
    {
        _last = snapshot;
        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        _body.Fill = snapshot.Body == CelestialBody.Earth ? EarthBrush : MoonBrush;

        var phase = snapshot.Phase;
        var nextMet = snapshot.PhaseIndex + 1 < timeline.Keyframes.Count
            ? timeline.Keyframes[snapshot.PhaseIndex + 1].Met
            : phase.Met;
        var span = (nextMet - phase.Met).TotalSeconds;
        var fraction = span > 0 ? (snapshot.Met - phase.Met).TotalSeconds / span : 1.0;

        var surfaceY = ActualHeight * (BodyCenterYRatio - BodyRadiusRatio);
        var altitudeRatio = Math.Clamp(Math.Log10(1 + snapshot.Telemetry.AltitudeMeters) / MaxAltitudeLogMeters, 0, 1);
        var markerY = surfaceY - altitudeRatio * surfaceY * 0.9 - MarkerSize;

        SetLeft(_marker, ActualWidth * (MarkerMinX + MarkerXSpan * fraction) - MarkerSize / 2);
        SetTop(_marker, Math.Max(markerY, 0));
    }

    /// <summary>크기가 바뀌면 별/천체/마커를 새로 배치한다.</summary>
    private void Rebuild()
    {
        Children.Clear();
        _stars.Clear();
        var random = new Random(StarSeed);
        for (var i = 0; i < StarCount; i++)
        {
            var star = new Ellipse { Width = 2, Height = 2, Fill = Brushes.White, Opacity = 0.3 + random.NextDouble() * 0.7 };
            SetLeft(star, random.NextDouble() * ActualWidth);
            SetTop(star, random.NextDouble() * ActualHeight);
            _stars.Add(star);
            Children.Add(star);
        }

        var radius = ActualHeight * BodyRadiusRatio;
        _body.Width = radius * 2;
        _body.Height = radius * 2;
        SetLeft(_body, ActualWidth / 2 - radius);
        SetTop(_body, ActualHeight * BodyCenterYRatio - radius);
        Children.Add(_body);
        Children.Add(_marker);

        if (_last is not null)
        {
            _body.Fill = _last.Body == CelestialBody.Earth ? EarthBrush : MoonBrush;
        }
        else
        {
            _body.Fill = EarthBrush;
        }
    }
}

using System.Windows;
using System.Windows.Media;

namespace Agc.Wpf.Controls;

/// <summary>
/// 7-세그먼트 숫자 표시기. Text의 각 글자를 한 칸씩 그린다(0-9, '-', '+', 공백 지원).
/// 꺼진 세그먼트는 희미하게 함께 그려 AGC의 전계발광 표시 느낌을 낸다.
/// - 상태 없는 단순 렌더링 컨트롤이며 UI 스레드에서만 사용한다.
/// - Text 또는 색상이 바뀌면 자동으로 다시 그린다.
/// </summary>
public sealed class SegmentDisplay : FrameworkElement
{
    private static Brush FrozenBrush(Color color)
    {
        // DP 기본값으로 쓰이는 브러시는 스레드 간 공유되므로 동결해야 한다.
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(SegmentDisplay),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty DigitCountProperty = DependencyProperty.Register(
        nameof(DigitCount), typeof(int), typeof(SegmentDisplay),
        new FrameworkPropertyMetadata(2, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty LitBrushProperty = DependencyProperty.Register(
        nameof(LitBrush), typeof(Brush), typeof(SegmentDisplay),
        new FrameworkPropertyMetadata(FrozenBrush(Color.FromRgb(0x7C, 0xFF, 0xB2)), FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty UnlitBrushProperty = DependencyProperty.Register(
        nameof(UnlitBrush), typeof(Brush), typeof(SegmentDisplay),
        new FrameworkPropertyMetadata(FrozenBrush(Color.FromRgb(0x10, 0x2A, 0x1A)), FrameworkPropertyMetadataOptions.AffectsRender));

    // 세그먼트 비트: a=위, b=오른쪽위, c=오른쪽아래, d=아래, e=왼쪽아래, f=왼쪽위, g=가운데
    private const int SegA = 1 << 0;
    private const int SegB = 1 << 1;
    private const int SegC = 1 << 2;
    private const int SegD = 1 << 3;
    private const int SegE = 1 << 4;
    private const int SegF = 1 << 5;
    private const int SegG = 1 << 6;
    private const int SegPlusVertical = 1 << 7;
    private const int SegmentCount = 8;

    private const double CellAspect = 0.6;
    private const double DigitWidthRatio = 0.76;
    private const double ThicknessRatio = 0.18;
    private const double GapRatio = 0.7;
    private const double CenterRatio = 0.5;
    private const double DefaultHeight = 40.0;

    // 칸 크기가 바뀌지 않는 한 재사용하는 세그먼트 도형(렌더링마다 새로 만들지 않기 위한 캐시).
    private StreamGeometry[]? _segmentGeometries;
    private Size _geometryCellSize;

    private static readonly int[] DigitMasks =
    [
        SegA | SegB | SegC | SegD | SegE | SegF,
        SegB | SegC,
        SegA | SegB | SegD | SegE | SegG,
        SegA | SegB | SegC | SegD | SegG,
        SegB | SegC | SegF | SegG,
        SegA | SegC | SegD | SegF | SegG,
        SegA | SegC | SegD | SegE | SegF | SegG,
        SegA | SegB | SegC,
        SegA | SegB | SegC | SegD | SegE | SegF | SegG,
        SegA | SegB | SegC | SegD | SegF | SegG,
    ];

    /// <summary>실제 DSKY 숫자는 오른쪽으로 기울어져 있다. 위쪽이 오른쪽으로 가도록 기울인다.</summary>
    private const double SlantDegrees = 8.0;

    public SegmentDisplay()
    {
        RenderTransform = new SkewTransform(-SlantDegrees, 0);
        RenderTransformOrigin = new Point(CenterRatio, CenterRatio);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public int DigitCount
    {
        get => (int)GetValue(DigitCountProperty);
        set => SetValue(DigitCountProperty, value);
    }

    public Brush LitBrush
    {
        get => (Brush)GetValue(LitBrushProperty);
        set => SetValue(LitBrushProperty, value);
    }

    public Brush UnlitBrush
    {
        get => (Brush)GetValue(UnlitBrushProperty);
        set => SetValue(UnlitBrushProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // 높이가 정해지면 그 높이에 맞는 칸 너비를 사용한다. 높이가 무한이면 기본 높이를 쓴다.
        var height = double.IsInfinity(availableSize.Height) ? DefaultHeight : availableSize.Height;
        return new Size(height * CellAspect * DigitCount, height);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var count = Math.Max(DigitCount, 1);
        var cellWidth = ActualWidth / count;
        var text = Text ?? string.Empty;
        EnsureSegmentGeometries(cellWidth, ActualHeight);
        for (var i = 0; i < count; i++)
        {
            var ch = i < text.Length ? text[i] : ' ';
            // 세그먼트 도형은 칸 크기에만 의존하므로 한 번 만든 것을 칸마다 평행 이동해서 재사용한다.
            dc.PushTransform(new TranslateTransform(i * cellWidth, 0));
            DrawCell(dc, MaskFor(ch));
            dc.Pop();
        }
    }

    /// <summary>글자에 해당하는 켜질 세그먼트 비트마스크(미지원 글자는 모두 꺼짐).</summary>
    private static int MaskFor(char ch)
    {
        if (ch is >= '0' and <= '9')
        {
            return DigitMasks[ch - '0'];
        }

        return ch switch
        {
            '-' => SegG,
            '+' => SegG | SegPlusVertical,
            _ => 0,
        };
    }

    /// <summary>첫 번째 칸(x=0) 기준의 세그먼트 도형 8개를 만든다. 칸 크기가 바뀔 때만 다시 만든다.</summary>
    private void EnsureSegmentGeometries(double cellWidth, double height)
    {
        if (_segmentGeometries is not null && _geometryCellSize == new Size(cellWidth, height))
        {
            return;
        }

        var width = cellWidth * DigitWidthRatio;
        var t = width * ThicknessRatio;
        var left = (cellWidth - width) / 2 + t / 2;
        var right = (cellWidth + width) / 2 - t / 2;
        var top = t;
        var bottom = height - t;
        var mid = height / 2;
        var gap = t * GapRatio;

        var geometries = new StreamGeometry[SegmentCount];
        for (var seg = 0; seg < SegmentCount; seg++)
        {
            geometries[seg] = PolygonGeometry(SegmentPoints(1 << seg, left, right, top, mid, bottom, t, gap));
        }

        _segmentGeometries = geometries;
        _geometryCellSize = new Size(cellWidth, height);
    }

    private void DrawCell(DrawingContext dc, int litMask)
    {
        for (var seg = 0; seg < SegmentCount; seg++)
        {
            var isLit = (litMask & (1 << seg)) != 0;
            if (!isLit && (1 << seg) == SegPlusVertical)
            {
                // '+' 전용 세로 획은 꺼진 상태로 그리지 않는다(모든 칸에 + 모양이 겹쳐 보이는 것을 방지).
                continue;
            }

            dc.DrawGeometry(isLit ? LitBrush : UnlitBrush, null, _segmentGeometries![seg]);
        }
    }

    private static Point[] SegmentPoints(
        int segment, double left, double right, double top, double mid, double bottom, double t, double gap)
    {
        return segment switch
        {
            SegA => Horizontal(top, left + gap, right - gap, t),
            SegG => Horizontal(mid, left + gap, right - gap, t),
            SegD => Horizontal(bottom, left + gap, right - gap, t),
            SegF => Vertical(left, top + gap, mid - gap, t),
            SegB => Vertical(right, top + gap, mid - gap, t),
            SegE => Vertical(left, mid + gap, bottom - gap, t),
            SegC => Vertical(right, mid + gap, bottom - gap, t),
            _ => Vertical((left + right) / 2, (top + mid) / 2 + gap, (mid + bottom) / 2 - gap, t),
        };
    }

    private static Point[] Horizontal(double y, double x0, double x1, double t)
    {
        var h = t / 2;
        return [new(x0, y), new(x0 + h, y - h), new(x1 - h, y - h), new(x1, y), new(x1 - h, y + h), new(x0 + h, y + h)];
    }

    private static Point[] Vertical(double x, double y0, double y1, double t)
    {
        var h = t / 2;
        return [new(x, y0), new(x + h, y0 + h), new(x + h, y1 - h), new(x, y1), new(x - h, y1 - h), new(x - h, y0 + h)];
    }

    private static StreamGeometry PolygonGeometry(Point[] points)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(points[0], isFilled: true, isClosed: true);
            ctx.PolyLineTo(points[1..], isStroked: true, isSmoothJoin: false);
        }

        geometry.Freeze();
        return geometry;
    }
}

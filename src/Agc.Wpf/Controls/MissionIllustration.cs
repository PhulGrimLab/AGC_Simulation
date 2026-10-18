using System.Windows;
using System.Windows.Media;
using Agc.Mission;
using Agc.Wpf.Scene;

namespace Agc.Wpf.Controls;

/// <summary>
/// 미션 장면 화면: 현재 단계(발사, 궤도, 달 전이, 하강, 표면, 재진입, 착수 …)에 맞는 그림을 그린다.
/// - 그림 자체는 <see cref="SceneRenderer"/>가 그리고, 이 컨트롤은 미션 상태를 받아 다시 그리게 하는 역할만 한다.
/// - 지구/달은 NASA 텍스처를 구 모양으로 렌더링한 이미지, 우주선/지형은 코드로 그린 벡터이다.
/// - Owner: MainWindow. Lifetime: 창과 동일. UI 스레드 전용.
/// </summary>
public sealed class MissionIllustration : FrameworkElement
{
    private const double MillisecondsPerSecond = 1000.0;

    private SceneContext? _context;

    public MissionIllustration()
    {
        ClipToBounds = true;
        SnapsToDevicePixels = false;
    }

    /// <summary>지금 그리고 있는 장면 종류(아직 상태를 받기 전이면 null).</summary>
    public MissionScene? CurrentScene => _context?.Scene;

    /// <summary>현재 장면의 설명 문구(화면 왼쪽 위에 표시되는 것과 같다).</summary>
    public string? CurrentCaption => _context is null ? null : SceneRenderer.Caption(_context);

    /// <summary>현재 미션 상태에 맞춰 장면을 갱신한다.</summary>
    public void Update(MissionSnapshot snapshot, MissionTimeline timeline)
    {
        _context = SceneContext.From(snapshot, timeline);
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var size = new Size(ActualWidth, ActualHeight);
        drawingContext.DrawRectangle(Brushes.Black, null, new Rect(size));
        if (_context is null)
        {
            return;
        }

        // 불꽃 일렁임 같은 짧은 애니메이션은 실시간 시계를 쓴다(미션 시간과 무관).
        var time = Environment.TickCount64 / MillisecondsPerSecond;
        SceneRenderer.Draw(drawingContext, size, _context, time, VisualTreeHelper.GetDpi(this).PixelsPerDip);
    }

    /// <summary>
    /// 장면을 비트맵으로 렌더링한다(테스트/스크린샷용). 컨트롤을 화면에 띄우지 않아도 지정한 크기로 그린다.
    /// </summary>
    internal static System.Windows.Media.Imaging.RenderTargetBitmap RenderOffscreen(
        MissionSnapshot snapshot, MissionTimeline timeline, int width, int height, double time = 0)
    {
        var context = SceneContext.From(snapshot, timeline);
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, width, height));
            SceneRenderer.Draw(dc, new Size(width, height), context, time, 1.0);
        }

        var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        return bitmap;
    }
}

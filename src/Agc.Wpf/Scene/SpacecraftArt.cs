using System.Windows;
using System.Windows.Media;

namespace Agc.Wpf.Scene;

/// <summary>
/// 아폴로 우주선을 벡터로 그리는 유틸리티(새턴 V, 사령/기계선 CSM, CSM+LM, 달착륙선 LM, 재진입 캡슐, 낙하산, 분사 불꽃).
/// - 모든 그림은 "기수가 위쪽(−y)"인 로컬 좌표에서 그리고, 호출 쪽이 이동/회전/크기를 변환으로 준다.
/// - 로컬 좌표의 원점은 그림의 중심, 높이 1.0 = 전체 길이(length 인자)이다.
/// - 상태 없는 정적 유틸리티이며 UI 스레드에서만 쓴다. 브러시/펜은 한 번만 만들어 동결해 공유한다.
/// </summary>
internal static class SpacecraftArt
{
    // ---- 공용 색/브러시 ----

    private static readonly Color WhiteHighlight = Color.FromRgb(0xFF, 0xFF, 0xFF);
    private static readonly Color WhiteBody = Color.FromRgb(0xE9, 0xEC, 0xF0);
    private static readonly Color WhiteShade = Color.FromRgb(0x9B, 0xA1, 0xAA);
    private static readonly Color WhiteDark = Color.FromRgb(0x6C, 0x72, 0x7D);
    private static readonly Color Black = Color.FromRgb(0x17, 0x19, 0x1D);

    /// <summary>원통형 흰색 외피: 왼쪽이 어둡고 가운데 밝고 오른쪽이 어두운 가로 그라디언트.</summary>
    private static readonly Brush WhiteCylinder = Frozen(new LinearGradientBrush(
        [
            new GradientStop(WhiteShade, 0.0),
            new GradientStop(WhiteBody, 0.20),
            new GradientStop(WhiteHighlight, 0.42),
            new GradientStop(Color.FromRgb(0xC9, 0xCE, 0xD5), 0.72),
            new GradientStop(WhiteDark, 1.0),
        ],
        new Point(0, 0), new Point(1, 0)));

    private static readonly Brush BlackCylinder = Frozen(new LinearGradientBrush(
        [
            new GradientStop(Color.FromRgb(0x0A, 0x0B, 0x0D), 0.0),
            new GradientStop(Color.FromRgb(0x3A, 0x3E, 0x46), 0.40),
            new GradientStop(Color.FromRgb(0x10, 0x11, 0x14), 1.0),
        ],
        new Point(0, 0), new Point(1, 0)));

    private static readonly Brush SilverCylinder = Frozen(new LinearGradientBrush(
        [
            new GradientStop(Color.FromRgb(0x6A, 0x70, 0x7A), 0.0),
            new GradientStop(Color.FromRgb(0xD5, 0xD9, 0xDF), 0.35),
            new GradientStop(Color.FromRgb(0xF4, 0xF6, 0xF8), 0.5),
            new GradientStop(Color.FromRgb(0x9C, 0xA2, 0xAC), 0.8),
            new GradientStop(Color.FromRgb(0x55, 0x5A, 0x64), 1.0),
        ],
        new Point(0, 0), new Point(1, 0)));

    /// <summary>금색 열차단 포일(착륙선 하강단): 가로 그라디언트 + 약한 세로 그라디언트 느낌.</summary>
    private static readonly Brush GoldFoil = Frozen(new LinearGradientBrush(
        [
            new GradientStop(Color.FromRgb(0x7A, 0x56, 0x14), 0.0),
            new GradientStop(Color.FromRgb(0xD9, 0xA8, 0x34), 0.25),
            new GradientStop(Color.FromRgb(0xFF, 0xE2, 0x8A), 0.5),
            new GradientStop(Color.FromRgb(0xC3, 0x8E, 0x24), 0.78),
            new GradientStop(Color.FromRgb(0x6B, 0x49, 0x10), 1.0),
        ],
        new Point(0, 0), new Point(1, 0.2)));

    private static readonly Brush GrayPanel = Frozen(new LinearGradientBrush(
        [
            new GradientStop(Color.FromRgb(0x5C, 0x61, 0x6B), 0.0),
            new GradientStop(Color.FromRgb(0xB9, 0xBE, 0xC6), 0.45),
            new GradientStop(Color.FromRgb(0x73, 0x79, 0x84), 1.0),
        ],
        new Point(0, 0), new Point(1, 1)));

    private static readonly Brush DarkMetal = Frozen(new LinearGradientBrush(
        [
            new GradientStop(Color.FromRgb(0x18, 0x1A, 0x1F), 0.0),
            new GradientStop(Color.FromRgb(0x55, 0x59, 0x62), 0.5),
            new GradientStop(Color.FromRgb(0x14, 0x15, 0x19), 1.0),
        ],
        new Point(0, 0), new Point(1, 0)));

    private static readonly Brush WindowGlass = Frozen(new SolidColorBrush(Color.FromRgb(0x1B, 0x25, 0x38)));
    private static readonly Brush HeatShieldBrush = Frozen(new LinearGradientBrush(
        [
            new GradientStop(Color.FromRgb(0x2A, 0x1D, 0x16), 0.0),
            new GradientStop(Color.FromRgb(0x6E, 0x4A, 0x30), 0.5),
            new GradientStop(Color.FromRgb(0x20, 0x16, 0x10), 1.0),
        ],
        new Point(0, 0), new Point(1, 0)));

    private static readonly Pen EdgePen = FrozenPen(Color.FromArgb(0x70, 0x00, 0x00, 0x00), 0.8);
    private static readonly Pen LegPen = FrozenPen(Color.FromRgb(0xC8, 0xCC, 0xD2), 1.6);

    // ---- 새턴 V ----

    /// <summary>새턴 V 단계 수준: 1=전체, 2=1단(S-IC) 분리 후, 3=2단(S-II) 분리 후.</summary>
    public enum SaturnStage
    {
        Full = 1,
        AfterFirstStage = 2,
        UpperStageOnly = 3,
    }

    // 새턴 V 전체 높이에 대한 각 구간의 비율(아래쪽=0, 위쪽=1)
    private const double FinBottom = 0.0;
    private const double FirstStageTop = 0.395;
    private const double SecondStageTop = 0.625;
    private const double ThirdStageBottom = 0.655;
    private const double ThirdStageTop = 0.820;
    private const double InstrumentUnitTop = 0.836;
    private const double AdapterTop = 0.880;
    private const double ServiceModuleTop = 0.935;
    private const double CommandModuleTop = 0.956;
    private const double FirstStageWidth = 0.092;
    private const double ThirdStageWidth = 0.060;
    private const double CapsuleWidth = 0.036;

    /// <summary>
    /// 새턴 V를 그린다. 로컬 원점은 전체 스택(1단 포함)의 중심, y 방향 높이는 length.
    /// stage에 따라 이미 분리된 단은 그리지 않는다(남은 단은 제자리에 둔다).
    /// </summary>
    public static void DrawSaturnV(DrawingContext dc, double length, SaturnStage stage, bool includeLaunchEscapeTower)
    {
        // 로컬 좌표: 아래(base) = +length/2, 위(top) = -length/2
        double Y(double fraction) => length * (0.5 - fraction);
        double W(double fraction) => length * fraction;
        var half = W(FirstStageWidth) / 2;
        var upperHalf = W(ThirdStageWidth) / 2;
        var capHalf = W(CapsuleWidth) / 2;

        if (stage == SaturnStage.Full)
        {
            DrawFirstStage(dc, length, Y, half);
        }

        if (stage <= SaturnStage.AfterFirstStage)
        {
            // S-II 단: 흰색 원통 + 검은 단간 띠
            FillRect(dc, WhiteCylinder, -half, Y(SecondStageTop), half, Y(FirstStageTop));
            FillRect(dc, BlackCylinder, -half, Y(FirstStageTop + 0.010), half, Y(FirstStageTop - 0.006));
            FillRect(dc, BlackCylinder, -half, Y(SecondStageTop + 0.006), half, Y(SecondStageTop - 0.010));
            // S-II 하단의 J-2 엔진(보이는 3개)
            DrawEngineBells(dc, Y(FirstStageTop), half * 0.55, length * 0.012, 3);
        }

        if (stage <= SaturnStage.UpperStageOnly)
        {
            // S-II/S-IVB 사이 원뿔형 단간부
            var interstage = new StreamGeometry();
            using (var ctx = interstage.Open())
            {
                ctx.BeginFigure(new Point(-half, Y(SecondStageTop)), true, true);
                ctx.LineTo(new Point(half, Y(SecondStageTop)), true, false);
                ctx.LineTo(new Point(upperHalf, Y(ThirdStageBottom)), true, false);
                ctx.LineTo(new Point(-upperHalf, Y(ThirdStageBottom)), true, false);
            }

            interstage.Freeze();
            dc.DrawGeometry(WhiteCylinder, EdgePen, interstage);
        }

        // S-IVB 3단(항상 보임): 흰색 원통 + 하단 J-2 엔진
        FillRect(dc, WhiteCylinder, -upperHalf, Y(ThirdStageTop), upperHalf, Y(ThirdStageBottom));
        FillRect(dc, BlackCylinder, -upperHalf, Y(ThirdStageBottom + 0.012), upperHalf, Y(ThirdStageBottom));
        DrawEngineBells(dc, Y(ThirdStageBottom), upperHalf * 0.5, length * 0.011, 1);
        // 계기부(IU): 검은 띠
        FillRect(dc, BlackCylinder, -upperHalf, Y(InstrumentUnitTop), upperHalf, Y(ThirdStageTop));
        DrawPayloadStack(dc, length, Y, upperHalf, capHalf, includeLaunchEscapeTower);
    }

    /// <summary>
    /// 새턴 V 윗부분(S-IVB 3단 + 아폴로 우주선)만 보이는 상태를 가운데 정렬해서 그린다. 지구 궤도/TLI 장면용.
    /// visibleLength는 보이는 부분(S-IVB 하단 ~ 우주선 끝)의 길이. 원점은 보이는 부분의 중심이다.
    /// </summary>
    public static void DrawUpperStackCentered(DrawingContext dc, double visibleLength)
    {
        var fullLength = visibleLength / (1.0 - ThirdStageBottom);
        var visibleCenterFraction = (ThirdStageBottom + 1.0) / 2.0;
        dc.PushTransform(new TranslateTransform(0, fullLength * (visibleCenterFraction - 0.5)));
        DrawSaturnV(dc, fullLength, SaturnStage.UpperStageOnly, includeLaunchEscapeTower: false);
        dc.Pop();
    }

    /// <summary>
    /// DrawUpperStackCentered로 그린 S-IVB의 엔진 노즐 끝(아래쪽) y 좌표. 분사 불꽃의 시작점으로 쓴다.
    /// </summary>
    public static double UpperStackExhaustY(double visibleLength)
    {
        return visibleLength / 2.0 + visibleLength * 0.04;
    }

    /// <summary>
    /// 새턴 V의 현재 가장 아래 단 엔진 끝 y 좌표(로컬, 아래쪽 +). 분사 불꽃의 시작점으로 쓴다.
    /// </summary>
    public static double ExhaustBaseY(double length, SaturnStage stage)
    {
        return stage switch
        {
            SaturnStage.Full => length * 0.5 + length * 0.002,
            SaturnStage.AfterFirstStage => length * (0.5 - FirstStageTop) + length * 0.014,
            _ => length * (0.5 - ThirdStageBottom) + length * 0.012,
        };
    }

    /// <summary>분리된 1단(S-IC)만 그린다(분리 후 떨어져 나가는 모습). 원점은 1단의 중심.</summary>
    public static void DrawSpentFirstStage(DrawingContext dc, double stackLength)
    {
        dc.PushTransform(new TranslateTransform(0, -stackLength * (0.5 - FirstStageTop / 2.0)));
        DrawFirstStage(dc, stackLength, f => stackLength * (0.5 - f), stackLength * FirstStageWidth / 2);
        dc.Pop();
    }

    private static void DrawFirstStage(DrawingContext dc, double length, Func<double, double> y, double half)
    {
        // 안정 날개 4장(옆에서 보이는 2장)
        var finWidth = length * 0.034;
        foreach (var side in new[] { -1.0, 1.0 })
        {
            var fin = new StreamGeometry();
            using (var ctx = fin.Open())
            {
                var xRoot = side * half;
                ctx.BeginFigure(new Point(xRoot, y(FinBottom + 0.075)), true, true);
                ctx.LineTo(new Point(xRoot + side * finWidth, y(FinBottom + 0.005)), true, false);
                ctx.LineTo(new Point(xRoot, y(FinBottom + 0.005)), true, false);
            }

            fin.Freeze();
            dc.DrawGeometry(DarkMetal, EdgePen, fin);
        }

        // F-1 엔진 노즐(보이는 3개)
        DrawEngineBells(dc, y(FinBottom + 0.02), half * 0.62, length * 0.030, 3);
        // S-IC 본체: 흰색 + 검은 띠(상단 큰 띠, 하단 띠, 가운데 가는 띠)
        FillRect(dc, WhiteCylinder, -half, y(FirstStageTop), half, y(FinBottom + 0.02));
        FillRect(dc, BlackCylinder, -half, y(FirstStageTop), half, y(FirstStageTop - 0.045));
        FillRect(dc, BlackCylinder, -half, y(FinBottom + 0.055), half, y(FinBottom + 0.02));
        FillRect(dc, BlackCylinder, -half, y(0.215), half, y(0.205));
    }

    private static void DrawPayloadStack(
        DrawingContext dc, double length, Func<double, double> y, double upperHalf, double capHalf, bool includeTower)
    {
        // 우주선/달착륙선 적응기(SLA): 원뿔대
        var sla = new StreamGeometry();
        using (var ctx = sla.Open())
        {
            ctx.BeginFigure(new Point(-upperHalf, y(InstrumentUnitTop)), true, true);
            ctx.LineTo(new Point(upperHalf, y(InstrumentUnitTop)), true, false);
            ctx.LineTo(new Point(capHalf, y(AdapterTop)), true, false);
            ctx.LineTo(new Point(-capHalf, y(AdapterTop)), true, false);
        }

        sla.Freeze();
        dc.DrawGeometry(WhiteCylinder, EdgePen, sla);
        // 기계선(SM) + 사령선(CM)
        FillRect(dc, SilverCylinder, -capHalf, y(ServiceModuleTop), capHalf, y(AdapterTop));
        var cm = new StreamGeometry();
        using (var ctx = cm.Open())
        {
            ctx.BeginFigure(new Point(-capHalf, y(ServiceModuleTop)), true, true);
            ctx.LineTo(new Point(capHalf, y(ServiceModuleTop)), true, false);
            ctx.LineTo(new Point(capHalf * 0.22, y(CommandModuleTop)), true, false);
            ctx.LineTo(new Point(-capHalf * 0.22, y(CommandModuleTop)), true, false);
        }

        cm.Freeze();
        dc.DrawGeometry(SilverCylinder, EdgePen, cm);

        if (!includeTower)
        {
            return;
        }

        // 발사 탈출 탑(LES): 가는 트러스 + 모터 + 뾰족한 노즈
        var towerHalf = length * 0.004;
        var pen = FrozenPen(Color.FromRgb(0xD8, 0xDB, 0xE0), 0.9);
        dc.DrawLine(pen, new Point(-towerHalf, y(CommandModuleTop)), new Point(-towerHalf, y(0.985)));
        dc.DrawLine(pen, new Point(towerHalf, y(CommandModuleTop)), new Point(towerHalf, y(0.985)));
        FillRect(dc, WhiteCylinder, -towerHalf * 1.6, y(0.998), towerHalf * 1.6, y(0.975));
        var nose = new StreamGeometry();
        using (var ctx = nose.Open())
        {
            ctx.BeginFigure(new Point(-towerHalf * 1.6, y(0.998)), true, true);
            ctx.LineTo(new Point(0, y(1.0) - length * 0.012), true, false);
            ctx.LineTo(new Point(towerHalf * 1.6, y(0.998)), true, false);
        }

        nose.Freeze();
        dc.DrawGeometry(WhiteCylinder, EdgePen, nose);
    }

    /// <summary>엔진 노즐 몇 개를 기준선 아래쪽으로 그린다(옆에서 보이는 개수만).</summary>
    private static void DrawEngineBells(DrawingContext dc, double baseY, double spread, double height, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var cx = count == 1 ? 0 : -spread + 2 * spread * i / (count - 1);
            var bell = new StreamGeometry();
            using (var ctx = bell.Open())
            {
                ctx.BeginFigure(new Point(cx - height * 0.28, baseY), true, true);
                ctx.LineTo(new Point(cx + height * 0.28, baseY), true, false);
                ctx.LineTo(new Point(cx + height * 0.52, baseY + height), true, false);
                ctx.LineTo(new Point(cx - height * 0.52, baseY + height), true, false);
            }

            bell.Freeze();
            dc.DrawGeometry(DarkMetal, EdgePen, bell);
        }
    }

    // ---- 사령/기계선 (CSM) ----

    /// <summary>
    /// CSM: 기계선 원통(방열판 패널), SPS 엔진 노즐, 사령선 원뿔, 고이득 안테나. 기수(CM)가 위. 로컬 원점은 중심, 높이 length.
    /// </summary>
    public static void DrawCsm(DrawingContext dc, double length)
    {
        var width = length * 0.36;
        var half = width / 2;
        var top = -length / 2;
        var bottom = length / 2;
        var cmHeight = length * 0.30;
        var smTop = top + cmHeight;
        var smBottom = bottom - length * 0.16;

        // SPS 엔진 노즐(아래쪽 원뿔)
        var bell = new StreamGeometry();
        using (var ctx = bell.Open())
        {
            ctx.BeginFigure(new Point(-half * 0.22, smBottom), true, true);
            ctx.LineTo(new Point(half * 0.22, smBottom), true, false);
            ctx.LineTo(new Point(half * 0.52, bottom), true, false);
            ctx.LineTo(new Point(-half * 0.52, bottom), true, false);
        }

        bell.Freeze();
        dc.DrawGeometry(DarkMetal, EdgePen, bell);

        // 기계선(SM): 은백색 원통 + 방열 패널 + 검은 띠
        FillRect(dc, SilverCylinder, -half, smTop, half, smBottom);
        FillRect(dc, BlackCylinder, -half, smBottom - length * 0.02, half, smBottom);
        FillRect(dc, GrayPanel, -half * 0.55, smTop + length * 0.10, half * 0.55, smBottom - length * 0.14);
        for (var i = 0; i < 3; i++)
        {
            var yy = smTop + length * (0.16 + 0.12 * i);
            dc.DrawLine(FrozenPen(Color.FromArgb(0x55, 0, 0, 0), 0.8), new Point(-half * 0.55, yy), new Point(half * 0.55, yy));
        }

        // RCS 사중 추력기 상자(양옆)
        FillRect(dc, BlackCylinder, -half - length * 0.018, smTop + length * 0.22, -half, smTop + length * 0.27);
        FillRect(dc, BlackCylinder, half, smTop + length * 0.22, half + length * 0.018, smTop + length * 0.27);

        // 사령선(CM): 원뿔 + 도킹 터널 + 창
        var cm = new StreamGeometry();
        using (var ctx = cm.Open())
        {
            ctx.BeginFigure(new Point(-half, smTop), true, true);
            ctx.LineTo(new Point(half, smTop), true, false);
            ctx.LineTo(new Point(half * 0.20, top), true, false);
            ctx.LineTo(new Point(-half * 0.20, top), true, false);
        }

        cm.Freeze();
        dc.DrawGeometry(SilverCylinder, EdgePen, cm);
        FillRect(dc, DarkMetal, -half * 0.20, top - length * 0.015, half * 0.20, top);
        dc.DrawEllipse(WindowGlass, null, new Point(-half * 0.26, top + cmHeight * 0.55), half * 0.10, cmHeight * 0.10);
        dc.DrawEllipse(WindowGlass, null, new Point(half * 0.26, top + cmHeight * 0.55), half * 0.10, cmHeight * 0.10);

        // 고이득 안테나(S-band): 작은 접시 + 지지대
        var dishCenter = new Point(half + length * 0.045, smTop + length * 0.45);
        dc.DrawLine(LegPen, new Point(half, smTop + length * 0.40), dishCenter);
        dc.DrawEllipse(GrayPanel, EdgePen, dishCenter, length * 0.025, length * 0.045);
    }

    // ---- 달착륙선 (LM) ----

    /// <summary>
    /// 달착륙선: 하강단(금색 포일, 다리 4개, DPS 엔진) + 상승단(회색 면체, 창, 안테나, RCS). 로컬 원점은 중심, 높이 length(다리 포함).
    /// descentStageAttached=false이면 하강단 없이 상승단만, ascentStageAttached=false이면 하강단만(이륙 후 남은 모습) 그린다.
    /// </summary>
    public static void DrawLm(DrawingContext dc, double length, bool descentStageAttached = true, bool ascentStageAttached = true)
    {
        var top = -length / 2;
        var bottom = length / 2;
        var bodyWidth = length * 0.62;
        var half = bodyWidth / 2;
        var legSpan = length * 0.50;
        var descentTop = bottom - length * 0.36;
        var descentBottom = bottom - length * 0.10;
        var ascentBottom = descentTop;
        var ascentTop = top + length * 0.10;

        if (descentStageAttached)
        {
            // 다리 4개(보이는 앞 2개 + 뒤 2개는 어둡게), 착륙 패드
            foreach (var sign in new[] { -1.0, 1.0 })
            {
                var hipTop = new Point(sign * half * 0.82, descentTop + length * 0.03);
                var foot = new Point(sign * (half + legSpan * 0.38), bottom);
                dc.DrawLine(LegPen, hipTop, foot);
                dc.DrawLine(LegPen, new Point(sign * half * 0.50, descentBottom), new Point(sign * (half + legSpan * 0.28), bottom - length * 0.04));
                dc.DrawEllipse(GrayPanel, EdgePen, new Point(foot.X, bottom), length * 0.07, length * 0.016);
            }

            // DPS 엔진 노즐
            var bell = new StreamGeometry();
            using (var ctx = bell.Open())
            {
                ctx.BeginFigure(new Point(-half * 0.20, descentBottom), true, true);
                ctx.LineTo(new Point(half * 0.20, descentBottom), true, false);
                ctx.LineTo(new Point(half * 0.33, descentBottom + length * 0.07), true, false);
                ctx.LineTo(new Point(-half * 0.33, descentBottom + length * 0.07), true, false);
            }

            bell.Freeze();
            dc.DrawGeometry(DarkMetal, EdgePen, bell);

            // 하강단: 금색 포일 팔각기둥(옆에서 보면 사다리꼴 느낌)
            var descent = new StreamGeometry();
            using (var ctx = descent.Open())
            {
                ctx.BeginFigure(new Point(-half, descentTop), true, true);
                ctx.LineTo(new Point(half, descentTop), true, false);
                ctx.LineTo(new Point(half * 0.92, descentBottom), true, false);
                ctx.LineTo(new Point(-half * 0.92, descentBottom), true, false);
            }

            descent.Freeze();
            dc.DrawGeometry(GoldFoil, EdgePen, descent);
            // 포일의 주름: 얇은 어두운 세로선
            for (var i = -2; i <= 2; i++)
            {
                var x = i * half * 0.36;
                dc.DrawLine(FrozenPen(Color.FromArgb(0x40, 0x30, 0x20, 0x00), 0.9), new Point(x, descentTop + 2), new Point(x * 0.93, descentBottom - 2));
            }

            // 사다리(앞다리 쪽)
            dc.DrawLine(FrozenPen(Color.FromRgb(0xE6, 0xE8, 0xEC), 1.2), new Point(-half * 0.45, descentBottom), new Point(-half * 0.45, descentTop + length * 0.02));
        }

        if (!ascentStageAttached)
        {
            // 이륙 후 하강단만 남은 모습
            return;
        }

        // 상승단: 회색 면체 + 앞 창 2개 + 위쪽 도킹창/해치
        var ascent = new StreamGeometry();
        using (var ctx = ascent.Open())
        {
            ctx.BeginFigure(new Point(-half * 0.85, ascentBottom), true, true);
            ctx.LineTo(new Point(half * 0.85, ascentBottom), true, false);
            ctx.LineTo(new Point(half * 0.74, ascentTop + length * 0.10), true, false);
            ctx.LineTo(new Point(half * 0.40, ascentTop), true, false);
            ctx.LineTo(new Point(-half * 0.40, ascentTop), true, false);
            ctx.LineTo(new Point(-half * 0.74, ascentTop + length * 0.10), true, false);
        }

        ascent.Freeze();
        dc.DrawGeometry(GrayPanel, EdgePen, ascent);
        dc.DrawGeometry(WindowGlass, null, Triangle(new Point(-half * 0.55, ascentTop + length * 0.16), length * 0.10, length * 0.08));
        dc.DrawGeometry(WindowGlass, null, Triangle(new Point(half * 0.20, ascentTop + length * 0.16), length * 0.10, length * 0.08));
        FillRect(dc, DarkMetal, -half * 0.18, ascentTop - length * 0.025, half * 0.18, ascentTop);

        // 랑데부 레이더 접시, S-band 안테나, RCS 사중 추력기
        var radar = new Point(half * 0.50, ascentTop - length * 0.03);
        dc.DrawLine(LegPen, new Point(half * 0.34, ascentTop + length * 0.02), radar);
        dc.DrawEllipse(GrayPanel, EdgePen, radar, length * 0.05, length * 0.022);
        FillRect(dc, BlackCylinder, half * 0.85, ascentBottom - length * 0.10, half * 0.85 + length * 0.05, ascentBottom - length * 0.05);
        FillRect(dc, BlackCylinder, -half * 0.85 - length * 0.05, ascentBottom - length * 0.10, -half * 0.85, ascentBottom - length * 0.05);
    }

    private static Geometry Triangle(Point topLeft, double width, double height)
    {
        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            ctx.BeginFigure(topLeft, true, true);
            ctx.LineTo(new Point(topLeft.X + width, topLeft.Y), true, false);
            ctx.LineTo(new Point(topLeft.X + width * 0.5, topLeft.Y + height), true, false);
        }

        g.Freeze();
        return g;
    }

    // ---- CSM + LM 도킹 스택 ----

    /// <summary>
    /// 달 전이 중 도킹 상태: 아래에서 위로 SPS 노즐, 기계선, 사령선, (도킹 터널) 달착륙선(다리가 위를 향함).
    /// 로컬 원점은 전체 스택의 중심.
    /// </summary>
    public static void DrawCsmWithLm(DrawingContext dc, double length)
    {
        var csmLength = length * 0.62;
        var lmLength = length * 0.46;
        var csmCenterY = length / 2 - csmLength / 2;
        var lmCenterY = -length / 2 + lmLength / 2;
        dc.PushTransform(new TranslateTransform(0, csmCenterY));
        DrawCsm(dc, csmLength);
        dc.Pop();

        // 달착륙선은 거꾸로(다리가 위) 붙어 있다.
        dc.PushTransform(new TransformGroup { Children = [new ScaleTransform(1, -1), new TranslateTransform(0, lmCenterY)] });
        DrawLm(dc, lmLength);
        dc.Pop();
    }

    // ---- 재진입 캡슐과 낙하산 ----

    /// <summary>
    /// 사령선 캡슐(재진입 자세): 열차폐가 아래, 뾰족한 쪽이 위. 로컬 원점은 중심, 높이 length.
    /// </summary>
    public static void DrawCommandModule(DrawingContext dc, double length)
    {
        var top = -length / 2;
        var bottom = length / 2;
        var halfBase = length * 0.50;
        var halfTop = length * 0.16;

        var body = new StreamGeometry();
        using (var ctx = body.Open())
        {
            ctx.BeginFigure(new Point(-halfBase, bottom - length * 0.18), true, true);
            ctx.LineTo(new Point(halfBase, bottom - length * 0.18), true, false);
            ctx.LineTo(new Point(halfTop, top), true, false);
            ctx.LineTo(new Point(-halfTop, top), true, false);
        }

        body.Freeze();
        dc.DrawGeometry(SilverCylinder, EdgePen, body);
        // 앞쪽(도킹) 해치 캡
        FillRect(dc, DarkMetal, -halfTop, top - length * 0.05, halfTop, top);
        // 창과 RCS
        dc.DrawEllipse(WindowGlass, null, new Point(-halfBase * 0.32, 0), length * 0.05, length * 0.075);
        dc.DrawEllipse(WindowGlass, null, new Point(halfBase * 0.32, 0), length * 0.05, length * 0.075);
        // 열차폐: 둥근 바닥(타 들어간 갈색)
        var shield = new StreamGeometry();
        using (var ctx = shield.Open())
        {
            ctx.BeginFigure(new Point(-halfBase, bottom - length * 0.18), true, true);
            ctx.BezierTo(new Point(-halfBase * 0.7, bottom + length * 0.04), new Point(halfBase * 0.7, bottom + length * 0.04), new Point(halfBase, bottom - length * 0.18), true, false);
        }

        shield.Freeze();
        dc.DrawGeometry(HeatShieldBrush, EdgePen, shield);
    }

    /// <summary>
    /// 주 낙하산 3개와 연결선. 로컬 원점은 사령선 연결점, 위쪽(−y)으로 spread×높이만큼 펼쳐진다.
    /// </summary>
    public static void DrawMainParachutes(DrawingContext dc, double canopyWidth, double lineLength)
    {
        var offsets = new[] { -canopyWidth * 0.62, 0.0, canopyWidth * 0.62 };
        var heights = new[] { lineLength * 0.92, lineLength, lineLength * 0.92 };
        var linePen = FrozenPen(Color.FromArgb(0xB0, 0xE8, 0xE8, 0xE8), 0.7);
        for (var i = 0; i < offsets.Length; i++)
        {
            var cx = offsets[i];
            var cy = -heights[i];
            // 연결선(캐노피 가장자리 → 연결점)
            foreach (var edge in new[] { -0.5, -0.17, 0.17, 0.5 })
            {
                dc.DrawLine(linePen, new Point(0, 0), new Point(cx + edge * canopyWidth, cy));
            }

            DrawCanopy(dc, new Point(cx, cy), canopyWidth);
        }
    }

    private static void DrawCanopy(DrawingContext dc, Point center, double width)
    {
        var height = width * 0.55;
        var half = width / 2;
        var dome = new StreamGeometry();
        using (var ctx = dome.Open())
        {
            ctx.BeginFigure(new Point(center.X - half, center.Y), true, true);
            ctx.BezierTo(
                new Point(center.X - half * 0.9, center.Y - height * 1.25),
                new Point(center.X + half * 0.9, center.Y - height * 1.25),
                new Point(center.X + half, center.Y), true, false);
            ctx.BezierTo(
                new Point(center.X + half * 0.5, center.Y + height * 0.18),
                new Point(center.X - half * 0.5, center.Y + height * 0.18),
                new Point(center.X - half, center.Y), true, false);
        }

        dome.Freeze();
        var gores = Frozen(new LinearGradientBrush(
            [
                new GradientStop(Color.FromRgb(0xE9, 0x5A, 0x2B), 0.0),
                new GradientStop(Color.FromRgb(0xF6, 0xF1, 0xE6), 0.2),
                new GradientStop(Color.FromRgb(0xE9, 0x5A, 0x2B), 0.4),
                new GradientStop(Color.FromRgb(0xF6, 0xF1, 0xE6), 0.6),
                new GradientStop(Color.FromRgb(0xE9, 0x5A, 0x2B), 0.8),
                new GradientStop(Color.FromRgb(0xF6, 0xF1, 0xE6), 1.0),
            ],
            new Point(0, 0), new Point(1, 0.15)));
        dc.DrawGeometry(gores, FrozenPen(Color.FromArgb(0x90, 0x40, 0x20, 0x10), 0.7), dome);
    }

    // ---- 분사 불꽃 / 플룸 ----

    /// <summary>
    /// 엔진 분사 불꽃: 원점에서 아래(+y)로 length만큼, 폭 width. intensity(0~1)와 flicker(시간)로 크기가 출렁인다.
    /// bright는 밝은 백황색(하강/상승 엔진), false이면 주황색(고체/케로신 느낌).
    /// </summary>
    public static void DrawExhaust(DrawingContext dc, double width, double length, double flicker, bool bright)
    {
        var wobble = 1.0 + 0.08 * Math.Sin(flicker * 37.0) + 0.05 * Math.Sin(flicker * 91.0);
        var len = length * wobble;
        var outer = new StreamGeometry();
        using (var ctx = outer.Open())
        {
            ctx.BeginFigure(new Point(-width / 2, 0), true, true);
            ctx.BezierTo(new Point(-width * 0.62, len * 0.45), new Point(-width * 0.18, len * 0.85), new Point(0, len), true, false);
            ctx.BezierTo(new Point(width * 0.18, len * 0.85), new Point(width * 0.62, len * 0.45), new Point(width / 2, 0), true, false);
        }

        outer.Freeze();
        var outerBrush = bright
            ? new LinearGradientBrush(
                [
                    new GradientStop(Color.FromArgb(0xF0, 0xFF, 0xFF, 0xF0), 0.0),
                    new GradientStop(Color.FromArgb(0xC0, 0xFF, 0xE2, 0x7A), 0.35),
                    new GradientStop(Color.FromArgb(0x70, 0xFF, 0x9A, 0x30), 0.70),
                    new GradientStop(Color.FromArgb(0x00, 0xFF, 0x6A, 0x10), 1.0),
                ],
                new Point(0, 0), new Point(0, 1))
            : new LinearGradientBrush(
                [
                    new GradientStop(Color.FromArgb(0xF0, 0xFF, 0xF0, 0xC0), 0.0),
                    new GradientStop(Color.FromArgb(0xD0, 0xFF, 0xA0, 0x30), 0.30),
                    new GradientStop(Color.FromArgb(0x80, 0xE8, 0x50, 0x10), 0.65),
                    new GradientStop(Color.FromArgb(0x00, 0xC0, 0x30, 0x08), 1.0),
                ],
                new Point(0, 0), new Point(0, 1));
        outerBrush.Freeze();
        dc.DrawGeometry(outerBrush, null, outer);

        // 중심 코어(더 뜨겁고 밝다)
        var core = new StreamGeometry();
        using (var ctx = core.Open())
        {
            ctx.BeginFigure(new Point(-width * 0.22, 0), true, true);
            ctx.BezierTo(new Point(-width * 0.26, len * 0.30), new Point(-width * 0.08, len * 0.50), new Point(0, len * 0.58), true, false);
            ctx.BezierTo(new Point(width * 0.08, len * 0.50), new Point(width * 0.26, len * 0.30), new Point(width * 0.22, 0), true, false);
        }

        core.Freeze();
        var coreBrush = new LinearGradientBrush(
            [
                new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 0.0),
                new GradientStop(Color.FromArgb(0x00, 0xFF, 0xF0, 0xB0), 1.0),
            ],
            new Point(0, 0), new Point(0, 1));
        coreBrush.Freeze();
        dc.DrawGeometry(coreBrush, null, core);
    }

    // ---- 도우미 ----

    private static void FillRect(DrawingContext dc, Brush brush, double left, double top, double right, double bottom)
    {
        dc.DrawRectangle(brush, EdgePen, new Rect(new Point(left, Math.Min(top, bottom)), new Point(right, Math.Max(top, bottom))));
    }

    private static T Frozen<T>(T freezable)
        where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }

    private static Pen FrozenPen(Color color, double thickness)
    {
        var pen = new Pen(new SolidColorBrush(color), thickness);
        pen.Freeze();
        return pen;
    }
}

using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Agc.Mission;

namespace Agc.Wpf.Scene;

/// <summary>
/// 미션 장면 렌더러: <see cref="SceneContext"/>(단계, 고도, 진행률 …)에 맞춰 11가지 장면을 그린다.
/// 발사 / 지구 궤도 / 달 전이 / 달 궤도 / 동력 하강 / 달 표면 / 상승 / 도킹 / 귀환 / 재진입 / 낙하산·착수.
/// 지구와 달은 NASA 텍스처로 렌더링한 구체(<see cref="PlanetImageCache"/>)이고,
/// 우주선·지형·바다·분사는 <see cref="SpacecraftArt"/>/<see cref="TerrainArt"/>가 코드로 그린 벡터이다.
/// 상태 없는 정적 클래스이며 UI 스레드에서만 호출한다. time(초)은 불꽃 일렁임 같은 짧은 애니메이션용 실시간이다.
/// </summary>
internal static class SceneRenderer
{
    // 지구/달 시점과 조명(화면 평면 각도, 태양 고도)
    private const double EarthSunAzimuth = 150;
    private const double EarthSunElevation = 28;
    private const double MoonSunAzimuth = 160;
    private const double MoonSunElevation = 32;
    private const double EarthDegreesPerHour = 15.0;
    private const double OrbitDegreesPerSecond = 360.0 / 5400.0;

    // 달 전이 도식의 구도
    private const double TranslunarEarthX = 0.17;
    private const double TranslunarMoonX = 0.85;
    private const double TranslunarEarthDiameter = 0.50;
    private const double TranslunarMoonDiameter = 0.15;

    private static readonly Typeface LabelFont = new("Malgun Gothic");

    private static readonly Color SpaceTop = Color.FromRgb(0x01, 0x02, 0x08);
    private static readonly Color SpaceBottom = Color.FromRgb(0x07, 0x0B, 0x1E);
    private static readonly Color SkyZenith = Color.FromRgb(0x1F, 0x56, 0xB0);
    private static readonly Color SkyHorizon = Color.FromRgb(0x9B, 0xCB, 0xF2);

    /// <summary>
    /// 장면을 그린다.
    /// </summary>
    /// <param name="pixelsPerDip">글자 렌더링용 화면 배율.</param>
    public static void Draw(DrawingContext dc, Size size, SceneContext c, double time, double pixelsPerDip)
    {
        if (size.Width < 2 || size.Height < 2)
        {
            return;
        }

        switch (c.Scene)
        {
            case MissionScene.Launch:
                DrawLaunch(dc, size, c, time);
                break;
            case MissionScene.EarthOrbit:
                DrawEarthOrbit(dc, size, c, time);
                break;
            case MissionScene.Translunar:
            case MissionScene.TransEarth:
                DrawJourney(dc, size, c, time, pixelsPerDip);
                break;
            case MissionScene.LunarOrbit:
                DrawLunarOrbit(dc, size, c, time);
                break;
            case MissionScene.Docking:
                DrawDocking(dc, size, c, time);
                break;
            case MissionScene.Descent:
                DrawDescent(dc, size, c, time, pixelsPerDip);
                break;
            case MissionScene.Surface:
                DrawSurface(dc, size, c);
                break;
            case MissionScene.Ascent:
                DrawAscent(dc, size, c, time);
                break;
            case MissionScene.Entry:
                DrawEntry(dc, size, c, time);
                break;
            case MissionScene.Splashdown:
                DrawSplashdown(dc, size, c, time);
                break;
            default:
                FillSpace(dc, size, 1.0, 0);
                break;
        }

        DrawCaption(dc, size, Caption(c), pixelsPerDip);
    }

    /// <summary>장면 왼쪽 위에 표시하는 짧은 설명(교육용: 지금 무엇을 보고 있는가).</summary>
    internal static string Caption(SceneContext c)
    {
        var met = c.Met.TotalSeconds;
        return c.Scene switch
        {
            MissionScene.Launch when met < 162 => "새턴 V 상승 · S-IC 1단 연소",
            MissionScene.Launch when met < 552 => "새턴 V 상승 · S-II 2단 연소",
            MissionScene.Launch => "새턴 V 상승 · S-IVB 3단 연소(궤도 진입)",
            MissionScene.EarthOrbit when c.EngineOn => "지구 궤도 · TLI: S-IVB 재점화로 달을 향해 가속",
            MissionScene.EarthOrbit => "지구 저궤도 · S-IVB + 아폴로 우주선",
            MissionScene.Translunar when c.Body == CelestialBody.Moon => $"달 중력권 · 달까지 {c.AltitudeMeters / 10_000_000:N1}만 km",
            MissionScene.Translunar => $"달 전이 비행 · 지구에서 {c.AltitudeMeters / 10_000_000:N1}만 km",
            MissionScene.TransEarth => "지구 귀환 비행",
            MissionScene.LunarOrbit when c.EngineOn => "달 궤도 · 서비스 추진계통(SPS) 연소",
            MissionScene.LunarOrbit => "달 궤도 · 사령선(컬럼비아)과 달착륙선(이글)",
            MissionScene.Docking => "달 궤도 · 랑데부와 도킹",
            MissionScene.Descent => $"달 하강 · P{c.Program:D2} · 고도 {c.AltitudeMeters * 3.28084:N0} ft",
            MissionScene.Surface when met >= new TimeSpan(109, 24, 15).TotalSeconds => "고요의 바다 · 달 표면 활동(EVA)",
            MissionScene.Surface => "고요의 바다 · 착륙 직후",
            MissionScene.Ascent => "달 표면 이륙 · 상승단(APS)",
            MissionScene.Entry => "대기권 재진입 · 열차폐 플라스마",
            MissionScene.Splashdown when c.AltitudeMeters > 5 => "낙하산 강하",
            _ => "태평양 착수 · 회수",
        };
    }

    // =====================================================================
    // 장면: 발사
    // =====================================================================

    private static void DrawLaunch(DrawingContext dc, Size s, SceneContext c, double time)
    {
        var met = c.Met.TotalSeconds;
        var alt = LaunchVisualAltitude(c);
        var atmosphere = 1.0 - Smooth(0, 100_000, alt);

        // 하늘: 낮 하늘 → 우주(고도에 따라)
        var top = Lerp(SpaceTop, SkyZenith, atmosphere);
        var horizon = Lerp(SpaceBottom, SkyHorizon, atmosphere);
        dc.DrawRectangle(Vertical(top, horizon), null, new Rect(s));
        StarField.Draw(dc, s, Math.Clamp((0.7 - atmosphere) / 0.7, 0, 1), met * 0.4, met * 0.1);

        var groundY = s.Height * 0.86 + Smooth(0, 14_000, alt) * s.Height * 0.6;
        var rocketLength = s.Height * 0.50;

        // 지구의 둥근 가장자리(고도 6 km 이상부터 서서히 나타난다)
        if (alt > 6_000)
        {
            DrawEarthLimb(dc, s, Lerp(s.Height * 1.14, s.Height * 0.80, Smooth(6_000, 185_000, alt)), atmosphere);
        }

        // 지면과 발사 시설
        var stage = met < 162 ? SpacecraftArt.SaturnStage.Full
            : met < 552 ? SpacecraftArt.SaturnStage.AfterFirstStage
            : SpacecraftArt.SaturnStage.UpperStageOnly;
        var centerX = s.Width * 0.46;
        if (groundY < s.Height)
        {
            DrawLaunchGround(dc, s, groundY, centerX, rocketLength, alt);
        }

        // 로켓: 점점 기울어지며(중력 선회) 오른쪽 위로 올라간다
        var tilt = 62.0 * Smooth(2_000, 140_000, alt);
        var centerY = Lerp(groundY - rocketLength * 0.5, s.Height * 0.47, Smooth(0, 900, alt));
        var x = centerX + Math.Sin(tilt * Math.PI / 180) * rocketLength * 0.12;
        var baseY = SpacecraftArt.ExhaustBaseY(rocketLength, stage);

        PushPose(dc, x, centerY, tilt);
        if (alt < 70_000)
        {
            DrawSmokeTrail(dc, baseY, rocketLength, (1.0 - Smooth(15_000, 70_000, alt)) * 0.55);
        }

        var plumeWidth = rocketLength * (stage == SpacecraftArt.SaturnStage.Full ? 0.17 : stage == SpacecraftArt.SaturnStage.AfterFirstStage ? 0.11 : 0.07);
        var plumeLength = rocketLength * (stage == SpacecraftArt.SaturnStage.Full ? 0.80 : stage == SpacecraftArt.SaturnStage.AfterFirstStage ? 0.46 : 0.28);
        dc.PushTransform(new TranslateTransform(0, baseY));
        SpacecraftArt.DrawExhaust(dc, plumeWidth, plumeLength, time, bright: false);
        dc.Pop();
        SpacecraftArt.DrawSaturnV(dc, rocketLength, stage, includeLaunchEscapeTower: met < 197);
        dc.Pop();

        // 분리된 1단이 뒤로 떨어져 나간다(분리 후 25초 동안)
        var sinceSeparation = met - 162;
        if (sinceSeparation is > 0 and < 25)
        {
            var f = sinceSeparation / 25.0;
            PushPose(dc, x - rocketLength * (0.10 + 0.25 * f), centerY + rocketLength * (0.30 + 0.9 * f), tilt - 40 * f);
            SpacecraftArt.DrawSpentFirstStage(dc, rocketLength);
            dc.Pop();
        }
    }

    /// <summary>
    /// 발사 장면용 고도. 타임라인은 키프레임 사이를 직선 보간하므로 이륙 직후가 비현실적으로 빠르다.
    /// 1단 연소(162초까지)는 가속하는 곡선(고도 ∝ 시간^1.9)으로 그려 이륙 직후 천천히 떠오르게 한다.
    /// 1단 분리 이후는 타임라인 고도를 그대로 쓴다.
    /// </summary>
    private static double LaunchVisualAltitude(SceneContext c)
    {
        const double firstStageBurnSeconds = 162.0;
        const double firstStageCutoffAltitude = 67_000.0;
        var met = c.Met.TotalSeconds;
        return met < firstStageBurnSeconds
            ? firstStageCutoffAltitude * Math.Pow(Math.Max(met, 0) / firstStageBurnSeconds, 1.9)
            : c.AltitudeMeters;
    }

    private static void DrawEarthLimb(DrawingContext dc, Size s, double limbTopY, double atmosphere)
    {
        var radius = s.Width * 2.6;
        var brush = new LinearGradientBrush(
            [
                new GradientStop(Color.FromRgb(0xA8, 0xD8, 0xFF), 0.0),
                new GradientStop(Color.FromRgb(0x3C, 0x84, 0xE0), 0.004),
                new GradientStop(Color.FromRgb(0x14, 0x4C, 0x9C), 0.02),
                new GradientStop(Color.FromRgb(0x0A, 0x2A, 0x62), 0.10),
                new GradientStop(Color.FromRgb(0x05, 0x14, 0x34), 1.0),
            ],
            new Point(0, 0), new Point(0, 1));
        brush.Freeze();
        dc.DrawEllipse(brush, null, new Point(s.Width * 0.5, limbTopY + radius), radius, radius);

        // 가장자리 위의 푸른 대기광
        var glow = new LinearGradientBrush(
            [
                new GradientStop(Color.FromArgb(0x00, 0x70, 0xB8, 0xFF), 0.0),
                new GradientStop(Color.FromArgb((byte)(0x70 * (1.0 - atmosphere * 0.5)), 0x70, 0xB8, 0xFF), 1.0),
            ],
            new Point(0, 0), new Point(0, 1));
        glow.Freeze();
        dc.DrawRectangle(glow, null, new Rect(0, limbTopY - s.Height * 0.06, s.Width, s.Height * 0.06));
    }

    private static void DrawLaunchGround(DrawingContext dc, Size s, double groundY, double rocketX, double rocketLength, double alt)
    {
        // 평지(잔디/해안 풍경)와 바다 한 줄, 발사대 콘크리트
        var land = Vertical(Color.FromRgb(0x4B, 0x6B, 0x3A), Color.FromRgb(0x22, 0x35, 0x1E));
        dc.DrawRectangle(land, null, new Rect(0, groundY, s.Width, s.Height - groundY));
        dc.DrawRectangle(Vertical(Color.FromRgb(0x2F, 0x6E, 0xA8), Color.FromRgb(0x1A, 0x4C, 0x7A)), null,
            new Rect(s.Width * 0.62, groundY, s.Width * 0.38, Math.Min(s.Height - groundY, s.Height * 0.03)));
        dc.DrawRectangle(Solid(Color.FromRgb(0x8E, 0x8E, 0x8A)), null,
            new Rect(rocketX - rocketLength * 0.20, groundY, rocketLength * 0.40, Math.Min(s.Height - groundY, rocketLength * 0.03)));

        if (alt < 4_000)
        {
            dc.PushTransform(new TranslateTransform(rocketX - rocketLength * 0.17, groundY));
            TerrainArt.DrawLaunchTower(dc, rocketLength * 0.86);
            dc.Pop();
            // 이륙 직후의 거대한 연기와 증기 구름
            var alpha = (byte)(200 * (1.0 - Smooth(300, 4_000, alt)));
            TerrainArt.DrawSmokePuffs(dc, new Point(rocketX, groundY - rocketLength * 0.01), rocketLength * 0.55, rocketLength * 0.17, alpha, 5);
        }
    }

    private static void DrawSmokeTrail(DrawingContext dc, double baseY, double length, double alpha)
    {
        if (alpha <= 0.02)
        {
            return;
        }

        var trail = new StreamGeometry();
        using (var ctx = trail.Open())
        {
            ctx.BeginFigure(new Point(-length * 0.05, baseY), true, true);
            ctx.LineTo(new Point(length * 0.05, baseY), true, false);
            ctx.LineTo(new Point(length * 0.17, baseY + length * 1.9), true, false);
            ctx.LineTo(new Point(-length * 0.17, baseY + length * 1.9), true, false);
        }

        trail.Freeze();
        var brush = new LinearGradientBrush(
            [
                new GradientStop(Color.FromArgb((byte)(220 * alpha), 0xF2, 0xF2, 0xF2), 0.0),
                new GradientStop(Color.FromArgb((byte)(120 * alpha), 0xD8, 0xDC, 0xE2), 0.5),
                new GradientStop(Color.FromArgb(0, 0xC8, 0xCC, 0xD4), 1.0),
            ],
            new Point(0, 0), new Point(0, 1));
        brush.Freeze();
        dc.DrawGeometry(brush, null, trail);
    }

    // =====================================================================
    // 장면: 지구 궤도 / TLI
    // =====================================================================

    private static void DrawEarthOrbit(DrawingContext dc, Size s, SceneContext c, double time)
    {
        var met = c.Met.TotalSeconds;
        FillSpace(dc, s, 1.0, met * 0.8);

        var diameter = s.Height * 1.65;
        var lon = -85 + met * OrbitDegreesPerSecond + met / 3600.0 * EarthDegreesPerHour;
        PlanetImageCache.Draw(dc, PlanetKind.Earth, new Point(s.Width * 0.30, s.Height * 1.03), diameter,
            lon, 16, EarthSunAzimuth, EarthSunElevation, met / 3600.0 * 2.0);

        // S-IVB + 아폴로 우주선(오른쪽 위, 기수가 오른쪽 위를 향함)
        var visible = s.Height * 0.34;
        PushPose(dc, s.Width * 0.70, s.Height * 0.30, 58);
        if (c.EngineOn)
        {
            dc.PushTransform(new TranslateTransform(0, SpacecraftArt.UpperStackExhaustY(visible)));
            SpacecraftArt.DrawExhaust(dc, visible * 0.19, visible * 0.62, time, bright: true);
            dc.Pop();
        }

        SpacecraftArt.DrawUpperStackCentered(dc, visible);
        dc.Pop();
    }

    // =====================================================================
    // 장면: 달 전이 / 귀환 (지구 ↔ 달 사이)
    // =====================================================================

    private static void DrawJourney(DrawingContext dc, Size s, SceneContext c, double time, double pixelsPerDip)
    {
        var met = c.Met.TotalSeconds;
        FillSpace(dc, s, 1.0, met * 0.05);

        var earthCenter = new Point(s.Width * TranslunarEarthX, s.Height * 0.50);
        var moonCenter = new Point(s.Width * TranslunarMoonX, s.Height * 0.46);
        var earthDiameter = s.Height * TranslunarEarthDiameter;
        var moonDiameter = s.Height * TranslunarMoonDiameter;

        PlanetImageCache.Draw(dc, PlanetKind.Earth, earthCenter, earthDiameter,
            -70 + met / 3600.0 * EarthDegreesPerHour, 14, EarthSunAzimuth, EarthSunElevation, met / 3600.0 * 2.0);
        PlanetImageCache.Draw(dc, PlanetKind.Moon, moonCenter, moonDiameter, 0, 4, MoonSunAzimuth, MoonSunElevation);

        // 자유 귀환 궤적: 지구 가장자리에서 달 가장자리까지의 곡선
        var p0 = new Point(earthCenter.X + earthDiameter * 0.52, earthCenter.Y - earthDiameter * 0.10);
        var p1 = new Point(s.Width * 0.50, s.Height * 0.12);
        var p2 = new Point(moonCenter.X - moonDiameter * 0.62, moonCenter.Y - moonDiameter * 0.18);
        DrawTrajectory(dc, p0, p1, p2, c.JourneyFraction);

        // 우주선: 곡선 위의 현재 위치, 진행 방향을 향함
        var f = c.JourneyFraction;
        var pos = Bezier(p0, p1, p2, f);
        var tangent = BezierTangent(p0, p1, p2, f);
        var heading = Math.Atan2(tangent.Y, tangent.X) * 180 / Math.PI + 90;
        if (c.Scene == MissionScene.TransEarth)
        {
            heading += 180;
        }

        var craftLength = s.Height * 0.085;
        DrawGlow(dc, pos, craftLength * 1.1);
        PushPose(dc, pos.X, pos.Y, heading);
        if (c.Scene == MissionScene.Translunar)
        {
            SpacecraftArt.DrawCsmWithLm(dc, craftLength);
        }
        else
        {
            SpacecraftArt.DrawCsm(dc, craftLength);
        }

        dc.Pop();

        DrawLabel(dc, "지구", new Point(earthCenter.X, earthCenter.Y + earthDiameter * 0.66), s.Height * 0.035, pixelsPerDip, TextAlignment.Center);
        DrawLabel(dc, "달", new Point(moonCenter.X, moonCenter.Y + moonDiameter * 0.80), s.Height * 0.035, pixelsPerDip, TextAlignment.Center);
    }

    private static void DrawTrajectory(DrawingContext dc, Point p0, Point p1, Point p2, double f)
    {
        var steps = 64;
        var past = new StreamGeometry();
        var future = new StreamGeometry();
        using (var pc = past.Open())
        using (var fc = future.Open())
        {
            var split = (int)(steps * f);
            pc.BeginFigure(p0, false, false);
            fc.BeginFigure(Bezier(p0, p1, p2, split / (double)steps), false, false);
            for (var i = 1; i <= steps; i++)
            {
                var pt = Bezier(p0, p1, p2, i / (double)steps);
                if (i <= split)
                {
                    pc.LineTo(pt, true, false);
                }
                else
                {
                    fc.LineTo(pt, true, false);
                }
            }
        }

        past.Freeze();
        future.Freeze();
        dc.DrawGeometry(null, new Pen(Solid(Color.FromArgb(0xE0, 0x78, 0xD8, 0xFF)), 2.4), past);
        var dashed = new Pen(Solid(Color.FromArgb(0x70, 0xB8, 0xC8, 0xE0)), 1.4) { DashStyle = new DashStyle([5, 5], 0) };
        dc.DrawGeometry(null, dashed, future);
    }

    // =====================================================================
    // 장면: 달 궤도 / 도킹
    // =====================================================================

    private static void DrawLunarOrbit(DrawingContext dc, Size s, SceneContext c, double time)
    {
        var met = c.Met.TotalSeconds;
        FillSpace(dc, s, 1.0, met * 0.1);
        DrawMoonLimbAndEarth(dc, s, met);

        var p = c.PhaseProgress;
        var csmLength = s.Height * 0.20;
        // 분리 전에는 달착륙선이 CSM에 붙은 채, 분리 단계에서는 점점 멀어진다
        var x = s.Width * (0.30 + 0.38 * Smooth(0, 1, Math.Min(1.0, met % 7200 / 7200.0 + 0.0)));
        var y = s.Height * 0.25 - Math.Sin(Math.PI * p) * s.Height * 0.02;
        var separation = c.PhaseName.StartsWith("LM", StringComparison.Ordinal);

        PushPose(dc, x, y, 90);
        if (c.EngineOn)
        {
            // 달 궤도 진입 연소(LOI)는 진행 방향으로 분사해 감속한다
            dc.PushTransform(new TranslateTransform(0, csmLength * 0.5));
            SpacecraftArt.DrawExhaust(dc, csmLength * 0.18, csmLength * 0.55, time, bright: true);
            dc.Pop();
        }

        if (separation)
        {
            SpacecraftArt.DrawCsm(dc, csmLength);
            dc.Pop();
            var gap = csmLength * (0.55 + 1.2 * p);
            PushPose(dc, x + csmLength * 0.1, y + gap, 0);
            SpacecraftArt.DrawLm(dc, csmLength * 0.62);
            dc.Pop();
        }
        else
        {
            SpacecraftArt.DrawCsmWithLm(dc, csmLength * 1.35);
            dc.Pop();
        }
    }

    private static void DrawMoonLimbAndEarth(DrawingContext dc, Size s, double met)
    {
        // 달: 아래쪽에 크게(윗부분만 보임). 시점 경도가 천천히 흐른다.
        var moonDiameter = s.Height * 1.55;
        PlanetImageCache.Draw(dc, PlanetKind.Moon, new Point(s.Width * 0.46, s.Height * 1.13), moonDiameter,
            -20 + met * 0.01, 6, MoonSunAzimuth - 130, MoonSunElevation + 14);
        // 멀리 떠 있는 지구(지구돋이 느낌)
        PlanetImageCache.Draw(dc, PlanetKind.Earth, new Point(s.Width * 0.16, s.Height * 0.20), s.Height * 0.17,
            -70 + met / 3600.0 * EarthDegreesPerHour, 12, EarthSunAzimuth + 25, EarthSunElevation + 14, met / 3600.0 * 2.0);
    }

    private static void DrawDocking(DrawingContext dc, Size s, SceneContext c, double time)
    {
        var met = c.Met.TotalSeconds;
        FillSpace(dc, s, 1.0, met * 0.1);
        DrawMoonLimbAndEarth(dc, s, met);

        var p = c.PhaseProgress;
        var csmLength = s.Height * 0.30;
        var center = new Point(s.Width * 0.38, s.Height * 0.30);
        PushPose(dc, center.X, center.Y, 90);
        SpacecraftArt.DrawCsm(dc, csmLength);
        dc.Pop();

        // 달착륙선 상승단이 오른쪽에서 다가와 사령선의 도킹 포트에 접근한다
        var distance = s.Width * (0.28 * (1.0 - Smooth(0, 1, p)) + 0.01);
        PushPose(dc, center.X + csmLength * 0.5 + csmLength * 0.30 + distance, center.Y, -90);
        SpacecraftArt.DrawLm(dc, csmLength * 0.55, descentStageAttached: false);
        dc.Pop();
        _ = time;
    }

    // =====================================================================
    // 장면: 동력 하강
    // =====================================================================

    private static void DrawDescent(DrawingContext dc, Size s, SceneContext c, double time, double pixelsPerDip)
    {
        var met = c.Met.TotalSeconds;
        var alt = Math.Max(0, c.AltitudeMeters);
        FillSpace(dc, s, 1.0, 0);

        // 하강 시작부터의 진행(0~1): 지형이 흘러가는 거리와 LM 위치를 정한다
        var start = new TimeSpan(102, 33, 5).TotalSeconds;
        var end = new TimeSpan(102, 45, 40).TotalSeconds;
        var progress = Math.Clamp((met - start) / (end - start), 0.0, 1.0);

        PlanetImageCache.Draw(dc, PlanetKind.Earth, new Point(s.Width * 0.80, s.Height * 0.18), s.Height * 0.13,
            -70, 10, EarthSunAzimuth + 30, EarthSunElevation + 20);

        var horizonY = s.Height * 0.74;
        var zoom = 1.0 + 1.6 * (1.0 - Smooth(0, 1, Math.Log10(alt + 10) / Math.Log10(15_010)));
        TerrainArt.DrawLunarGround(dc, s, horizonY, progress * s.Width * 6, zoom);

        // 착륙선 높이: 고도의 로그 눈금(높은 곳은 압축, 지표 근처는 크게)
        var altitudeFraction = Math.Clamp(Math.Log10(alt + 10) / Math.Log10(15_010), 0, 1);
        var lmLength = s.Height * 0.17 * (1.0 + 0.35 * (1 - altitudeFraction));
        var groundContactY = s.Height * 0.90;
        var lmY = Lerp(groundContactY - lmLength * 0.5, s.Height * 0.20, altitudeFraction);
        var lmX = s.Width * (0.36 + 0.06 * Math.Sin(progress * 5.0));
        // 제동(P63)에서는 엔진이 진행 방향을 향해 뒤로 기울고, 접근(P64)과 착륙(P66)으로 갈수록 수직이 된다
        var pitch = c.Program switch { 63 => 32.0, 64 => 14.0, _ => 3.0 };

        // 착륙 직전 분사 먼지
        if (alt < 60)
        {
            var dust = (byte)(210 * (1.0 - alt / 60.0));
            TerrainArt.DrawSmokePuffs(dc, new Point(lmX, groundContactY + lmLength * 0.02), lmLength * 1.1, lmLength * 0.22, dust, 21);
        }

        PushPose(dc, lmX, lmY, pitch);
        if (c.EngineOn)
        {
            dc.PushTransform(new TranslateTransform(0, lmLength * 0.5 + lmLength * 0.08));
            SpacecraftArt.DrawExhaust(dc, lmLength * 0.18, lmLength * 0.72, time, bright: true);
            dc.Pop();
        }

        SpacecraftArt.DrawLm(dc, lmLength);
        dc.Pop();

        DrawAltimeter(dc, s, alt, c.VerticalSpeedMetersPerSecond, pixelsPerDip);
    }

    /// <summary>오른쪽 고도계: 로그 눈금 막대와 현재 고도 표시.</summary>
    private static void DrawAltimeter(DrawingContext dc, Size s, double alt, double vertical, double pixelsPerDip)
    {
        var x = s.Width - s.Width * 0.06;
        var top = s.Height * 0.16;
        var bottom = s.Height * 0.90;
        var pen = new Pen(Solid(Color.FromArgb(0xB0, 0xCF, 0xE8, 0xFF)), 1.4);
        dc.DrawLine(pen, new Point(x, top), new Point(x, bottom));
        foreach (var tick in new[] { 10.0, 30, 100, 300, 1_000, 3_000, 10_000 })
        {
            var f = Math.Clamp(Math.Log10(tick + 10) / Math.Log10(15_010), 0, 1);
            var y = Lerp(bottom, top, f);
            dc.DrawLine(pen, new Point(x - 8, y), new Point(x, y));
            DrawLabel(dc, tick >= 1000 ? $"{tick / 1000:0.#}km" : $"{tick:0}m", new Point(x - 12, y - s.Height * 0.014),
                s.Height * 0.026, pixelsPerDip, TextAlignment.Right);
        }

        var markerY = Lerp(bottom, top, Math.Clamp(Math.Log10(alt + 10) / Math.Log10(15_010), 0, 1));
        var marker = new StreamGeometry();
        using (var ctx = marker.Open())
        {
            ctx.BeginFigure(new Point(x + 2, markerY), true, true);
            ctx.LineTo(new Point(x + 14, markerY - 7), true, false);
            ctx.LineTo(new Point(x + 14, markerY + 7), true, false);
        }

        marker.Freeze();
        dc.DrawGeometry(Solid(Color.FromRgb(0xFF, 0xC8, 0x3C)), null, marker);
        _ = vertical;
    }

    // =====================================================================
    // 장면: 달 표면 / 상승
    // =====================================================================

    private static void DrawSurface(DrawingContext dc, Size s, SceneContext c)
    {
        FillSpace(dc, s, 1.0, 0);
        PlanetImageCache.Draw(dc, PlanetKind.Earth, new Point(s.Width * 0.72, s.Height * 0.20), s.Height * 0.20,
            -70, 12, EarthSunAzimuth + 25, EarthSunElevation + 16);

        var horizonY = s.Height * 0.60;
        TerrainArt.DrawLunarGround(dc, s, horizonY, 0, 1.0);

        var groundY = s.Height * 0.86;
        var lmLength = s.Height * 0.30;
        DrawGroundShadow(dc, new Point(s.Width * 0.38, groundY + 3), lmLength * 1.1);
        dc.PushTransform(new TranslateTransform(s.Width * 0.38, groundY - lmLength * 0.5));
        SpacecraftArt.DrawLm(dc, lmLength);
        dc.Pop();

        // 착륙 지점 기념물: 성조기, 우주인(EVA 이후)
        var eva = c.Met >= new TimeSpan(109, 24, 15);
        if (eva)
        {
            dc.PushTransform(new TranslateTransform(s.Width * 0.70, groundY - s.Height * 0.02));
            TerrainArt.DrawFlag(dc, s.Height * 0.24);
            dc.Pop();
            DrawGroundShadow(dc, new Point(s.Width * 0.56, groundY + 2), s.Height * 0.12);
            dc.PushTransform(new TranslateTransform(s.Width * 0.56, groundY));
            TerrainArt.DrawAstronaut(dc, s.Height * 0.20);
            dc.Pop();
            // 발자국 몇 개
            for (var i = 0; i < 6; i++)
            {
                dc.DrawEllipse(Solid(Color.FromArgb(0x70, 0x30, 0x30, 0x32)), null,
                    new Point(s.Width * (0.46 + 0.015 * i), groundY + 8 + (i % 2) * 5), 5, 2.2);
            }
        }
    }

    private static void DrawGroundShadow(DrawingContext dc, Point center, double width)
    {
        var shadow = new RadialGradientBrush(
            [
                new GradientStop(Color.FromArgb(0x90, 0, 0, 0), 0.0),
                new GradientStop(Color.FromArgb(0x00, 0, 0, 0), 1.0),
            ]);
        shadow.Freeze();
        dc.DrawEllipse(shadow, null, new Point(center.X + width * 0.30, center.Y), width * 0.85, width * 0.11);
    }

    private static void DrawAscent(DrawingContext dc, Size s, SceneContext c, double time)
    {
        var alt = Math.Max(0, c.AltitudeMeters);
        if (alt >= 3_000 || c.PhaseName.Contains("궤도", StringComparison.Ordinal))
        {
            DrawAscentInOrbit(dc, s, c, time);
            return;
        }

        FillSpace(dc, s, 1.0, 0);
        PlanetImageCache.Draw(dc, PlanetKind.Earth, new Point(s.Width * 0.72, s.Height * 0.22), s.Height * 0.20,
            -70, 12, EarthSunAzimuth + 25, EarthSunElevation + 16);
        var horizonY = s.Height * 0.60 + Smooth(0, 3_000, alt) * s.Height * 0.20;
        TerrainArt.DrawLunarGround(dc, s, horizonY, alt * 0.6, 1.0 + 0.5 * (1.0 - Smooth(0, 3_000, alt)));

        var groundY = s.Height * 0.86 + Smooth(0, 3_000, alt) * s.Height * 0.4;
        var lmLength = s.Height * 0.30;
        // 하강단은 발사대 역할을 하며 남는다
        dc.PushTransform(new TranslateTransform(s.Width * 0.36, groundY - lmLength * 0.5));
        SpacecraftArt.DrawLm(dc, lmLength, descentStageAttached: true, ascentStageAttached: false);
        dc.Pop();
        // 상승단: 로그 눈금으로 높이 올라감
        var f = Math.Clamp(Math.Log10(alt + 10) / Math.Log10(3_010), 0, 1);
        var ascentY = Lerp(groundY - lmLength * 0.80, s.Height * 0.20, f);
        var ascentLength = lmLength * 0.62;
        PushPose(dc, s.Width * 0.36 + f * s.Width * 0.18, ascentY, 8 + 28 * f);
        dc.PushTransform(new TranslateTransform(0, AscentStageEngineY(ascentLength)));
        SpacecraftArt.DrawExhaust(dc, ascentLength * 0.20, ascentLength * 0.75, time, bright: true);
        dc.Pop();
        SpacecraftArt.DrawLm(dc, ascentLength, descentStageAttached: false);
        dc.Pop();
    }

    /// <summary>하강단 없이 상승단만 그렸을 때 상승 엔진(APS) 노즐 위치(로컬 y). 상승단 바닥에서 불꽃이 시작한다.</summary>
    private static double AscentStageEngineY(double length)
    {
        return length * 0.14;
    }

    private static void DrawAscentInOrbit(DrawingContext dc, Size s, SceneContext c, double time)
    {
        var met = c.Met.TotalSeconds;
        FillSpace(dc, s, 1.0, met * 0.1);
        DrawMoonLimbAndEarth(dc, s, met);
        var length = s.Height * 0.18;
        PushPose(dc, s.Width * 0.34 + c.PhaseProgress * s.Width * 0.22, s.Height * 0.32, 78);
        if (c.EngineOn)
        {
            dc.PushTransform(new TranslateTransform(0, AscentStageEngineY(length)));
            SpacecraftArt.DrawExhaust(dc, length * 0.20, length * 0.55, time, bright: true);
            dc.Pop();
        }

        SpacecraftArt.DrawLm(dc, length, descentStageAttached: false);
        dc.Pop();
        // 멀리 앞서가는 사령선(점 크기)
        PushPose(dc, s.Width * 0.82, s.Height * 0.26, 90);
        SpacecraftArt.DrawCsm(dc, s.Height * 0.07);
        dc.Pop();
    }

    // =====================================================================
    // 장면: 재진입 / 낙하산 · 착수
    // =====================================================================

    private static void DrawEntry(DrawingContext dc, Size s, SceneContext c, double time)
    {
        var met = c.Met.TotalSeconds;
        FillSpace(dc, s, 1.0, met * 0.03);
        PlanetImageCache.Draw(dc, PlanetKind.Earth, new Point(s.Width * 0.46, s.Height * 1.20), s.Height * 1.85,
            -150, 8, 205, 22, 20);

        // 플라스마: 122km(시작) → 75km(최대 감속)에서 가장 강하다
        var intensity = Math.Clamp(1.0 - (c.AltitudeMeters - 70_000) / 60_000.0, 0.25, 1.0);
        var capsuleLength = s.Height * 0.15;
        var center = new Point(s.Width * 0.60, s.Height * 0.36);
        var travel = 32 * Math.PI / 180; // 화면에서 오른쪽 아래로 향하는 진행 방향
        var direction = new Vector(Math.Cos(travel), Math.Sin(travel));

        DrawPlasmaTrail(dc, center, direction, capsuleLength, intensity, time);
        PushPose(dc, center.X, center.Y, travel * 180 / Math.PI - 90);
        SpacecraftArt.DrawCommandModule(dc, capsuleLength);
        dc.Pop();
    }

    private static void DrawPlasmaTrail(DrawingContext dc, Point head, Vector direction, double size, double intensity, double time)
    {
        // 열차폐 앞쪽의 충격파 광휘 + 뒤로 길게 끌리는 플라스마 꼬리
        var angle = Math.Atan2(direction.Y, direction.X) * 180 / Math.PI;
        dc.PushTransform(new TransformGroup { Children = [new RotateTransform(angle), new TranslateTransform(head.X, head.Y)] });
        var tailLength = size * (3.2 + 4.2 * intensity) * (1.0 + 0.06 * Math.Sin(time * 23));
        var layers = new[]
        {
            (Width: size * 2.2, Length: tailLength, Color: Color.FromArgb((byte)(120 * intensity), 0xFF, 0x6A, 0x14)),
            (Width: size * 1.6, Length: tailLength * 0.78, Color: Color.FromArgb((byte)(170 * intensity), 0xFF, 0x9C, 0x2C)),
            (Width: size * 1.05, Length: tailLength * 0.52, Color: Color.FromArgb((byte)(215 * intensity), 0xFF, 0xD2, 0x6A)),
            (Width: size * 0.55, Length: tailLength * 0.30, Color: Color.FromArgb((byte)(250 * intensity), 0xFF, 0xF8, 0xE4)),
        };
        foreach (var layer in layers)
        {
            var g = new StreamGeometry();
            using (var ctx = g.Open())
            {
                ctx.BeginFigure(new Point(size * 0.55, -layer.Width * 0.35), true, true);
                ctx.BezierTo(new Point(size * 0.9, -layer.Width * 0.6), new Point(size * 0.9, layer.Width * 0.6), new Point(size * 0.55, layer.Width * 0.35), true, false);
                ctx.BezierTo(new Point(-layer.Length * 0.4, layer.Width * 0.7), new Point(-layer.Length * 0.8, layer.Width * 0.3), new Point(-layer.Length, 0), true, false);
                ctx.BezierTo(new Point(-layer.Length * 0.8, -layer.Width * 0.3), new Point(-layer.Length * 0.4, -layer.Width * 0.7), new Point(size * 0.55, -layer.Width * 0.35), true, false);
            }

            g.Freeze();
            var brush = new SolidColorBrush(layer.Color);
            brush.Freeze();
            dc.DrawGeometry(brush, null, g);
        }

        // 열차폐 앞의 충격파 광휘(가장 밝은 부분)
        var shock = new RadialGradientBrush(
            [
                new GradientStop(Color.FromArgb((byte)(240 * intensity), 0xFF, 0xFA, 0xE6), 0.0),
                new GradientStop(Color.FromArgb((byte)(170 * intensity), 0xFF, 0xB8, 0x4A), 0.45),
                new GradientStop(Color.FromArgb(0, 0xFF, 0x70, 0x20), 1.0),
            ]);
        shock.Freeze();
        dc.DrawEllipse(shock, null, new Point(size * 0.45, 0), size * 1.15, size * 0.95);

        // 불똥(아블레이션 입자)
        var random = new Random(31);
        for (var i = 0; i < 26; i++)
        {
            var along = random.NextDouble();
            var x = -along * tailLength * 0.9 - 8 - Math.Abs(Math.Sin(time * 3 + i)) * 10;
            var y = (random.NextDouble() - 0.5) * size * (0.4 + along * 1.6);
            var r = 0.8 + random.NextDouble() * 1.6;
            dc.DrawEllipse(Solid(Color.FromArgb((byte)(220 * (1 - along) * intensity), 0xFF, 0xE0, 0x90)), null, new Point(x, y), r, r);
        }

        dc.Pop();
    }

    private static void DrawSplashdown(DrawingContext dc, Size s, SceneContext c, double time)
    {
        var alt = Math.Max(0, c.AltitudeMeters);
        var horizonY = s.Height * 0.62;
        // 맑은 하늘(수평선 쪽이 연함) + 구름 몇 점
        dc.DrawRectangle(Vertical(Color.FromRgb(0x5A, 0x9E, 0xE0), Color.FromRgb(0xCF, 0xE6, 0xF7)), null, new Rect(0, 0, s.Width, horizonY));
        var random = new Random(8);
        for (var i = 0; i < 6; i++)
        {
            var cloud = new RadialGradientBrush(
                [
                    new GradientStop(Color.FromArgb(0xB0, 0xFF, 0xFF, 0xFF), 0.0),
                    new GradientStop(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), 1.0),
                ]);
            cloud.Freeze();
            var w = s.Width * (0.10 + 0.12 * random.NextDouble());
            dc.DrawEllipse(cloud, null, new Point(s.Width * random.NextDouble(), horizonY * (0.2 + 0.6 * random.NextDouble())), w, w * 0.30);
        }

        TerrainArt.DrawOcean(dc, s, horizonY, time);

        var splashed = alt < 5;
        // 수평선의 회수 함정
        dc.PushTransform(new TranslateTransform(s.Width * 0.80, horizonY + 2));
        TerrainArt.DrawCarrier(dc, s.Width * 0.16);
        dc.Pop();

        var capsuleLength = s.Height * 0.12;
        var altitudeFraction = Math.Clamp(alt / 8_000.0, 0, 1);
        if (splashed)
        {
            // 물 위에 뜬 캡슐과 펼쳐진 낙하산이 바다에 누운 모습
            var cx = s.Width * 0.42;
            var cy = s.Height * 0.78;
            dc.DrawEllipse(Solid(Color.FromArgb(0x70, 0xFF, 0xFF, 0xFF)), null, new Point(cx, cy + capsuleLength * 0.30), capsuleLength * 0.9, capsuleLength * 0.12);
            PushPose(dc, cx, cy, 14 + 4 * Math.Sin(time * 1.4));
            SpacecraftArt.DrawCommandModule(dc, capsuleLength);
            dc.Pop();
            dc.DrawEllipse(Solid(Color.FromArgb(0xC8, 0xE9, 0x5A, 0x2B)), null, new Point(cx - capsuleLength * 1.9, cy + capsuleLength * 0.25), capsuleLength * 0.9, capsuleLength * 0.16);
            dc.DrawEllipse(Solid(Color.FromArgb(0xC8, 0xF6, 0xF1, 0xE6)), null, new Point(cx - capsuleLength * 2.9, cy + capsuleLength * 0.45), capsuleLength * 0.7, capsuleLength * 0.13);
        }
        else
        {
            // 낙하산 강하: 고도에 따라 위에서 아래로 내려온다(약간 흔들림)
            var cy = Lerp(horizonY + s.Height * 0.12, s.Height * 0.40, Math.Pow(altitudeFraction, 0.55));
            var sway = Math.Sin(time * 0.8) * 5;
            PushPose(dc, s.Width * 0.46, cy, sway);
            SpacecraftArt.DrawMainParachutes(dc, s.Height * 0.17, s.Height * 0.30);
            SpacecraftArt.DrawCommandModule(dc, capsuleLength);
            dc.Pop();
        }
    }

    // =====================================================================
    // 공통 도우미
    // =====================================================================

    private static void FillSpace(DrawingContext dc, Size s, double starVisibility, double drift)
    {
        dc.DrawRectangle(Vertical(SpaceTop, SpaceBottom), null, new Rect(s));
        StarField.Draw(dc, s, starVisibility, drift, drift * 0.2);
    }

    private static void DrawGlow(DrawingContext dc, Point center, double radius)
    {
        var glow = new RadialGradientBrush(
            [
                new GradientStop(Color.FromArgb(0x60, 0xA8, 0xD8, 0xFF), 0.0),
                new GradientStop(Color.FromArgb(0x00, 0xA8, 0xD8, 0xFF), 1.0),
            ]);
        glow.Freeze();
        dc.DrawEllipse(glow, null, center, radius, radius);
    }

    private static void DrawCaption(DrawingContext dc, Size s, string text, double pixelsPerDip)
    {
        var fontSize = Math.Clamp(s.Height * 0.034, 11, 18);
        var formatted = new FormattedText(
            text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, LabelFont, fontSize, Brushes.White, pixelsPerDip);
        var padding = fontSize * 0.55;
        var rect = new Rect(12, 12, formatted.Width + padding * 2, formatted.Height + padding);
        dc.DrawRoundedRectangle(Solid(Color.FromArgb(0x99, 0x08, 0x10, 0x20)), null, rect, 5, 5);
        dc.DrawText(formatted, new Point(rect.X + padding, rect.Y + padding * 0.5));
    }

    private static void DrawLabel(DrawingContext dc, string text, Point at, double fontSize, double pixelsPerDip, TextAlignment alignment)
    {
        var formatted = new FormattedText(
            text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, LabelFont, Math.Max(fontSize, 9),
            Solid(Color.FromArgb(0xC8, 0xDD, 0xE8, 0xF8)), pixelsPerDip);
        var x = alignment switch
        {
            TextAlignment.Center => at.X - formatted.Width / 2,
            TextAlignment.Right => at.X - formatted.Width,
            _ => at.X,
        };
        dc.DrawText(formatted, new Point(x, at.Y));
    }

    private static void PushPose(DrawingContext dc, double x, double y, double angleDegrees)
    {
        dc.PushTransform(new TransformGroup { Children = [new RotateTransform(angleDegrees), new TranslateTransform(x, y)] });
    }

    private static Point Bezier(Point p0, Point p1, Point p2, double t)
    {
        var u = 1 - t;
        return new Point(
            u * u * p0.X + 2 * u * t * p1.X + t * t * p2.X,
            u * u * p0.Y + 2 * u * t * p1.Y + t * t * p2.Y);
    }

    private static Vector BezierTangent(Point p0, Point p1, Point p2, double t)
    {
        var u = 1 - t;
        return new Vector(
            2 * u * (p1.X - p0.X) + 2 * t * (p2.X - p1.X),
            2 * u * (p1.Y - p0.Y) + 2 * t * (p2.Y - p1.Y));
    }

    private static double Smooth(double edge0, double edge1, double x)
    {
        var t = Math.Clamp((x - edge0) / (edge1 - edge0), 0.0, 1.0);
        return t * t * (3 - 2 * t);
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;

    private static Color Lerp(Color a, Color b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return Color.FromRgb(
            (byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t));
    }

    private static Brush Vertical(Color top, Color bottom)
    {
        var brush = new LinearGradientBrush(top, bottom, new Point(0, 0), new Point(0, 1));
        brush.Freeze();
        return brush;
    }

    private static Brush Solid(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}

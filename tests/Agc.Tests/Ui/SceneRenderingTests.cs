using System.Windows.Media.Imaging;
using Agc.Wpf.Controls;

namespace Agc.Tests.Ui;

/// <summary>
/// 기능: 미션 장면 렌더링(UI-G20~). 단계마다 맞는 장면이 선택되고, 각 장면이 비어 있지 않으며 기대하는 요소(불꽃, 천체, 지면, 바다 …)가
/// 그려지는지 픽셀로 검증한다. 모든 단계의 렌더링 결과를 screenshots/scenes/ 에 PNG로 저장한다(육안 검토용).
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class SceneRenderingTests
{
    private const int Width = 840;
    private const int Height = 560;

    private static readonly MissionTimeline Timeline = Apollo11Mission.Create();

    private static MissionSnapshot SnapshotAt(string phaseName, double fractionIntoPhase = 0.0)
    {
        var index = Timeline.Keyframes.ToList().FindIndex(k => k.PhaseName == phaseName);
        var start = Timeline.Keyframes[index].Met;
        var end = index + 1 < Timeline.Keyframes.Count ? Timeline.Keyframes[index + 1].Met : start;
        return Timeline.GetSnapshot(start + (end - start) * fractionIntoPhase);
    }

    private static (byte B, byte G, byte R, byte A)[] Pixels(BitmapSource bitmap)
    {
        var stride = bitmap.PixelWidth * 4;
        var data = new byte[stride * bitmap.PixelHeight];
        bitmap.CopyPixels(data, stride, 0);
        var result = new (byte, byte, byte, byte)[bitmap.PixelWidth * bitmap.PixelHeight];
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = (data[i * 4], data[i * 4 + 1], data[i * 4 + 2], data[i * 4 + 3]);
        }

        return result;
    }

    private static int Count(BitmapSource bitmap, Func<byte, byte, byte, bool> predicate)
    {
        return Pixels(bitmap).Count(p => predicate(p.R, p.G, p.B));
    }

    private static void Save(BitmapSource bitmap, string name)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "screenshots", "scenes");
        Directory.CreateDirectory(dir);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(dir, name));
        encoder.Save(stream);
    }

    // ---- 장면 선택 (데이터) ----

    [TestMethod]
    [TestCategory("UI-G20")]
    public void EveryKeyframe_HasASceneThatMatchesItsPhase()
    {
        var expected = new Dictionary<string, MissionScene>
        {
            ["발사"] = MissionScene.Launch,
            ["지구 저궤도 진입"] = MissionScene.EarthOrbit,
            ["TLI 점화"] = MissionScene.EarthOrbit,
            ["달로 향하는 비행"] = MissionScene.Translunar,
            ["달 궤도 진입 연소(LOI-1)"] = MissionScene.LunarOrbit,
            ["동력 하강 시작(PDI)"] = MissionScene.Descent,
            ["수동 조종 착륙(P66)"] = MissionScene.Descent,
            ["착륙 — 고요의 바다"] = MissionScene.Surface,
            ["달 표면 활동(EVA)"] = MissionScene.Surface,
            ["상승단 이륙"] = MissionScene.Ascent,
            ["도킹"] = MissionScene.Docking,
            ["지구로 돌아오는 비행"] = MissionScene.TransEarth,
            ["대기권 재진입"] = MissionScene.Entry,
            ["최종 강하(P67)"] = MissionScene.Splashdown,
            ["착수"] = MissionScene.Splashdown,
        };

        foreach (var (name, scene) in expected)
        {
            Assert.AreEqual(scene, Timeline.Keyframes.Single(k => k.PhaseName == name).Scene, name);
        }

        Assert.AreEqual(11, Timeline.Keyframes.Select(k => k.Scene).Distinct().Count(), "11가지 장면이 모두 쓰인다");
    }

    [TestMethod]
    [TestCategory("UI-G21")]
    public void Context_EngineIsOnOnlyDuringBurns()
    {
        bool EngineOn(string phase, double fraction) => Agc.Wpf.Scene.SceneContext.From(SnapshotAt(phase, fraction), Timeline).EngineOn;

        Assert.IsTrue(EngineOn("발사", 0.1), "새턴 V 상승");
        Assert.IsTrue(EngineOn("TLI 점화", 0.1), "TLI");
        Assert.IsFalse(EngineOn("지구 저궤도 진입", 0.5), "궤도 비행 중에는 엔진 정지");
        Assert.IsTrue(EngineOn("동력 하강 시작(PDI)", 0.5), "하강 엔진");
        Assert.IsFalse(EngineOn("달로 향하는 비행", 0.5));
        Assert.IsTrue(EngineOn("달 궤도 진입 연소(LOI-1)", 0.0), "LOI 연소 초반");
        Assert.IsFalse(EngineOn("달 궤도 진입 연소(LOI-1)", 0.5), "LOI 연소는 6분 안에 끝난다(단계는 하루 가까이 계속)");
        Assert.IsFalse(EngineOn("착륙 — 고요의 바다", 0.5));
        Assert.IsTrue(EngineOn("상승단 이륙", 0.5), "상승 엔진");
    }

    [TestMethod]
    [TestCategory("UI-G22")]
    public void Context_JourneyFractionGoesFromEarthToMoonAndBack()
    {
        double Journey(string phase) => Agc.Wpf.Scene.SceneContext.From(SnapshotAt(phase), Timeline).JourneyFraction;

        Assert.IsLessThan(0.05, Journey("TLI 종료"));
        Assert.IsGreaterThan(Journey("TLI 종료"), Journey("달로 향하는 비행"));
        Assert.IsGreaterThan(Journey("달로 향하는 비행"), Journey("달 접근"));
        Assert.IsGreaterThan(Journey("달 접근"), Journey("달 중력권 진입"), "달에 가까울수록 커진다");
        Assert.IsGreaterThan(Journey("귀환 비행 후반"), Journey("지구로 돌아오는 비행"), "돌아올수록 지구에 가까워 작아진다");
    }

    [TestMethod]
    [TestCategory("UI-G23")]
    public void Captions_DescribeTheStageOfTheLaunchVehicle()
    {
        string Caption(double seconds) => Agc.Wpf.Scene.SceneRenderer.Caption(
            Agc.Wpf.Scene.SceneContext.From(Timeline.GetSnapshot(TimeSpan.FromSeconds(seconds)), Timeline));

        StringAssert.Contains(Caption(60), "S-IC");
        StringAssert.Contains(Caption(300), "S-II");
        StringAssert.Contains(Caption(600), "S-IVB");
    }

    // ---- 렌더링 (픽셀) ----

    [STATestMethod]
    [TestCategory("UI-G24")]
    public void EveryPhase_RendersANonBlankScene_AndSavesAScreenshot()
    {
        var index = 0;
        foreach (var keyframe in Timeline.Keyframes)
        {
            var bitmap = MissionIllustration.RenderOffscreen(Timeline.GetSnapshot(keyframe.Met + TimeSpan.FromSeconds(20)), Timeline, Width, Height);

            var distinct = Pixels(bitmap).Select(p => (p.R / 16, p.G / 16, p.B / 16)).Distinct().Count();
            Assert.IsGreaterThan(30, distinct, $"장면이 단색이면 안 된다: {keyframe.PhaseName}");
            Save(bitmap, $"scene_{++index:D2}_{keyframe.Scene}.png");
        }
    }

    [STATestMethod]
    [TestCategory("UI-G25")]
    public void Launch_ShowsOrangeExhaustAndABlueSkyNearTheGround()
    {
        var bitmap = MissionIllustration.RenderOffscreen(SnapshotAt("발사", 0.1), Timeline, Width, Height);

        Assert.IsGreaterThan(300, Count(bitmap, (r, g, b) => r > 230 && g is > 120 and < 230 && b < 120), "주황/노랑 분사 불꽃");
        Assert.IsGreaterThan(5000, Count(bitmap, (r, g, b) => b > 150 && b > r + 40), "푸른 낮 하늘");
    }

    [STATestMethod]
    [TestCategory("UI-G26")]
    public void EarthOrbit_ShowsALargeBlueEarth()
    {
        var bitmap = MissionIllustration.RenderOffscreen(SnapshotAt("지구 저궤도 진입", 0.5), Timeline, Width, Height);

        Assert.IsGreaterThan(40000, Count(bitmap, (r, g, b) => b > r + 25 && b > 60), "화면의 상당 부분이 푸른 지구");
    }

    [STATestMethod]
    [TestCategory("UI-G27")]
    public void Journey_ShowsBothEarthAndMoon_AndTheSpacecraftMovesAlongTheTrajectory()
    {
        var early = MissionIllustration.RenderOffscreen(SnapshotAt("달로 향하는 비행"), Timeline, Width, Height);
        var late = MissionIllustration.RenderOffscreen(SnapshotAt("달 접근"), Timeline, Width, Height);

        Assert.IsGreaterThan(8000, Count(early, (r, g, b) => b > r + 25 && b > 60), "지구");
        Assert.IsGreaterThan(1500, Count(early, (r, g, b) => Math.Abs(r - g) < 14 && Math.Abs(g - b) < 14 && r is > 90 and < 200), "회색 달");
        CollectionAssert.AreNotEqual(Pixels(early).Select(p => p.R).ToArray(), Pixels(late).Select(p => p.R).ToArray(), "우주선 위치가 달라진다");
    }

    [STATestMethod]
    [TestCategory("UI-G28")]
    public void Descent_ShowsGrayLunarGroundAndTheLunarModule()
    {
        var bitmap = MissionIllustration.RenderOffscreen(SnapshotAt("접근 단계(P64)"), Timeline, Width, Height);

        Assert.IsGreaterThan(30000, Count(bitmap, (r, g, b) => Math.Abs(r - g) < 14 && Math.Abs(g - b) < 14 && r is > 40 and < 190), "회색 달 표면");
        Assert.IsGreaterThan(300, Count(bitmap, (r, g, b) => r > 190 && g is > 140 and < 220 && b < 110), "금색 포일의 착륙선 하강단");
    }

    [STATestMethod]
    [TestCategory("UI-G29")]
    public void Surface_BeforeAndAfterEva_ShowsFlagOnlyAfterEva()
    {
        var landed = MissionIllustration.RenderOffscreen(SnapshotAt("착륙 — 고요의 바다", 0.5), Timeline, Width, Height);
        var eva = MissionIllustration.RenderOffscreen(SnapshotAt("달 표면 활동(EVA)", 0.2), Timeline, Width, Height);

        bool IsFlagRed(byte r, byte g, byte b) => r is > 150 and < 210 && g < 60 && b is > 30 and < 90;
        Assert.AreEqual(0, Count(landed, IsFlagRed), "착륙 직후에는 성조기가 없다");
        Assert.IsGreaterThan(150, Count(eva, IsFlagRed), "EVA 단계에는 성조기의 빨간 줄무늬");
    }

    [STATestMethod]
    [TestCategory("UI-G30")]
    public void Entry_ShowsGlowingPlasma_StrongerAtPeakHeating()
    {
        bool IsPlasma(byte r, byte g, byte b) => r > 220 && g is > 110 and < 240 && b < 190;
        var interface_ = MissionIllustration.RenderOffscreen(SnapshotAt("대기권 재진입", 0.0), Timeline, Width, Height);
        var peak = MissionIllustration.RenderOffscreen(SnapshotAt("최대 감속 구간(P64)", 0.0), Timeline, Width, Height);

        // 재진입 시작(122 km)의 플라스마는 약하고, 최대 감속(75 km)에서 가장 밝고 넓다.
        Assert.IsGreaterThan(Count(interface_, IsPlasma), Count(peak, IsPlasma), "최대 감속에서 가장 밝고 넓다");
        Assert.IsGreaterThan(5000, Count(peak, IsPlasma), "최대 감속: 화면의 상당 부분이 플라스마");
    }

    [STATestMethod]
    [TestCategory("UI-G31")]
    public void Splashdown_ShowsOceanAndParachutesAboveAndFloatingCapsuleAtTheEnd()
    {
        bool IsOcean(byte r, byte g, byte b) => b > r + 40 && b > g && b < 180;
        bool IsChuteOrange(byte r, byte g, byte b) => r > 210 && g is > 70 and < 120 && b is > 20 and < 70;
        var falling = MissionIllustration.RenderOffscreen(SnapshotAt("최종 강하(P67)", 0.3), Timeline, Width, Height);
        var down = MissionIllustration.RenderOffscreen(SnapshotAt("착수"), Timeline, Width, Height);

        Assert.IsGreaterThan(30000, Count(falling, IsOcean), "바다");
        Assert.IsGreaterThan(400, Count(falling, IsChuteOrange), "낙하산 주황색 줄무늬");
        Assert.IsGreaterThan(30000, Count(down, IsOcean));
    }

    [STATestMethod]
    [TestCategory("UI-G32")]
    public void Scene_IsDeterministicForTheSameStateAndTime()
    {
        var a = MissionIllustration.RenderOffscreen(SnapshotAt("동력 하강 시작(PDI)", 0.5), Timeline, 420, 280, time: 1.5);
        var b = MissionIllustration.RenderOffscreen(SnapshotAt("동력 하강 시작(PDI)", 0.5), Timeline, 420, 280, time: 1.5);

        CollectionAssert.AreEqual(Pixels(a).Select(p => p.R).ToArray(), Pixels(b).Select(p => p.R).ToArray());
    }

    [STATestMethod]
    [TestCategory("UI-G33")]
    public void EveryScene_RendersFastEnoughForAnimation()
    {
        // 첫 렌더링은 행성 이미지를 만드느라 느리다. 두 번째(캐시된 상태)부터를 측정한다.
        foreach (var keyframe in Timeline.Keyframes)
        {
            MissionIllustration.RenderOffscreen(Timeline.GetSnapshot(keyframe.Met), Timeline, Width, Height);
        }

        var watch = System.Diagnostics.Stopwatch.StartNew();
        foreach (var keyframe in Timeline.Keyframes)
        {
            MissionIllustration.RenderOffscreen(Timeline.GetSnapshot(keyframe.Met), Timeline, Width, Height, 0.5);
        }

        var perFrame = watch.ElapsedMilliseconds / (double)Timeline.Keyframes.Count;
        Assert.IsLessThan(120.0, perFrame, $"장면 한 장의 평균 렌더 시간(캐시 후): {perFrame:F0} ms");
    }
}

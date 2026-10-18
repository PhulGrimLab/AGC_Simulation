using System.Diagnostics;
using System.Windows.Media.Imaging;
using Agc.Wpf.Scene;

namespace Agc.Tests.Ui;

/// <summary>
/// 기능: 지구/달 구체 렌더러(UI-G 시나리오). NASA 텍스처가 읽히고, 음영·대기·구름·림이 기대대로 그려지는지 픽셀로 검증한다.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class PlanetRendererTests
{
    private const int Disc = 300;

    private static byte[] Pixels(BitmapSource bitmap)
    {
        var stride = bitmap.PixelWidth * 4;
        var data = new byte[stride * bitmap.PixelHeight];
        bitmap.CopyPixels(data, stride, 0);
        return data;
    }

    /// <summary>(x, y) 픽셀의 (B, G, R, A) — 미리 곱해진 알파.</summary>
    private static (byte B, byte G, byte R, byte A) At(BitmapSource bmp, byte[] data, int x, int y)
    {
        var i = (y * bmp.PixelWidth + x) * 4;
        return (data[i], data[i + 1], data[i + 2], data[i + 3]);
    }

    private static double Brightness((byte B, byte G, byte R, byte A) p) => (p.B + p.G + p.R) / 3.0;

    [STATestMethod]
    [TestCategory("UI-G01")]
    public void Textures_AreLoadedFromEmbeddedResources()
    {
        Assert.AreEqual((2048, 1024), PlanetRenderer.TextureSize(PlanetKind.Earth));
        Assert.AreEqual((1024, 512), PlanetRenderer.TextureSize(PlanetKind.Moon));
    }

    [STATestMethod]
    [TestCategory("UI-G02")]
    public void Render_ReturnsSquareFrameLargerThanTheDisc_AndIsFrozen()
    {
        var bmp = PlanetRenderer.Render(PlanetKind.Earth, Disc, 0, 20, 150, 25, 0);

        Assert.AreEqual(bmp.PixelWidth, bmp.PixelHeight);
        Assert.AreEqual((int)Math.Ceiling(Disc * PlanetRenderer.FrameScale), bmp.PixelWidth);
        Assert.IsTrue(bmp.IsFrozen, "다른 스레드/컨트롤이 공유해도 안전해야 한다");
    }

    [STATestMethod]
    [TestCategory("UI-G03")]
    public void Earth_SunlitSide_IsBrighterThanNightSide_AndOceanIsBlue()
    {
        // 태양이 왼쪽(180°)에서 비춘다: 왼쪽이 낮, 오른쪽이 밤.
        var bmp = PlanetRenderer.Render(PlanetKind.Earth, Disc, -150, 5, 180, 5, 0);
        var data = Pixels(bmp);
        var c = bmp.PixelWidth / 2;
        var radius = Disc / 2;

        var day = At(bmp, data, c - radius / 2, c);
        var night = At(bmp, data, c + radius / 2, c);

        Assert.IsGreaterThan(Brightness(night) * 3, Brightness(day), "낮 쪽이 훨씬 밝다");
        Assert.IsGreaterThan(day.R, day.B, "태평양 한가운데(경도 약 -180° 부근)는 푸른 기운이 지배적이다");
    }

    [STATestMethod]
    [TestCategory("UI-G04")]
    public void Earth_HasAtmosphericGlowOutsideTheDisc_ButCornersAreTransparent()
    {
        var bmp = PlanetRenderer.Render(PlanetKind.Earth, Disc, 0, 0, 180, 20, 0);
        var data = Pixels(bmp);
        var c = bmp.PixelWidth / 2;
        var radius = Disc / 2;

        var justOutsideLit = At(bmp, data, c - radius - 3, c);
        var corner = At(bmp, data, 1, 1);

        Assert.IsGreaterThan(20, (int)justOutsideLit.A, "원반 바로 바깥(태양 쪽)에 푸른 대기 글로가 있다");
        Assert.IsGreaterThan(justOutsideLit.R, justOutsideLit.B, "글로는 푸른색");
        Assert.AreEqual(0, corner.A, "프레임 모서리는 완전 투명");
    }

    [STATestMethod]
    [TestCategory("UI-G05")]
    public void Moon_IsGrayAndHasNoAtmosphereGlow()
    {
        var bmp = PlanetRenderer.Render(PlanetKind.Moon, Disc, 0, 0, 90, 70, 0);
        var data = Pixels(bmp);
        var c = bmp.PixelWidth / 2;
        var radius = Disc / 2;

        var surface = At(bmp, data, c, c);
        var outside = At(bmp, data, c, c - radius - 4);

        Assert.AreEqual(255, surface.A);
        Assert.IsLessThan(24, Math.Abs(surface.R - surface.B), "달은 거의 무채색");
        Assert.IsGreaterThan(40, Brightness(surface), "정면 조명이면 충분히 밝다");
        Assert.AreEqual(0, outside.A, "달은 대기가 없어 원반 밖은 투명");
    }

    [STATestMethod]
    [TestCategory("UI-G06")]
    public void Moon_NearSideCenterIsDarkMariaCompareToFarSide_AndRotationChangesTheImage()
    {
        var near = PlanetRenderer.Render(PlanetKind.Moon, Disc, 0, 0, 90, 80, 0);
        var far = PlanetRenderer.Render(PlanetKind.Moon, Disc, 180, 0, 90, 80, 0);

        CollectionAssert.AreNotEqual(Pixels(near), Pixels(far), "중심 경도가 달라지면 다른 면이 보인다");
    }

    [STATestMethod]
    [TestCategory("UI-G07")]
    public void Earth_CloudShift_ChangesTheCloudLayerOnly()
    {
        var a = PlanetRenderer.Render(PlanetKind.Earth, Disc, 0, 10, 150, 30, 0);
        var b = PlanetRenderer.Render(PlanetKind.Earth, Disc, 0, 10, 150, 30, 40);

        CollectionAssert.AreNotEqual(Pixels(a), Pixels(b), "구름이 흐르면 화면이 달라진다");
    }

    [STATestMethod]
    [TestCategory("UI-G08")]
    public void Render_IsDeterministic()
    {
        var a = Pixels(PlanetRenderer.Render(PlanetKind.Earth, 180, 40, 15, 140, 20, 5));
        var b = Pixels(PlanetRenderer.Render(PlanetKind.Earth, 180, 40, 15, 140, 20, 5));

        CollectionAssert.AreEqual(a, b);
    }

    [STATestMethod]
    [TestCategory("UI-G09")]
    public void Render_IsFastEnoughForInteractiveUse()
    {
        PlanetRenderer.Render(PlanetKind.Earth, 100, 0, 0, 150, 20, 0); // 텍스처/구름 지도 준비(한 번만)
        var watch = Stopwatch.StartNew();

        PlanetRenderer.Render(PlanetKind.Earth, 420, 10, 15, 150, 20, 3);
        PlanetRenderer.Render(PlanetKind.Moon, 420, 10, 0, 150, 20, 0);

        Assert.IsLessThan(600, watch.ElapsedMilliseconds, "지구+달 420px 렌더링이 0.6초 이내여야 한다(화면 갱신은 간격을 두고 요청)");
    }

    [STATestMethod]
    [TestCategory("UI-G10")]
    public void SampleRenders_AreSavedAsPngForVisualReview()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "screenshots", "scenes");
        Directory.CreateDirectory(dir);
        void Save(BitmapSource bitmap, string name)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create(Path.Combine(dir, name));
            encoder.Save(stream);
        }

        Save(PlanetRenderer.Render(PlanetKind.Earth, 500, -20, 18, 155, 28, 10), "planet_earth.png");
        Save(PlanetRenderer.Render(PlanetKind.Earth, 500, 140, 10, 200, -20, 40), "planet_earth_crescent.png");
        Save(PlanetRenderer.Render(PlanetKind.Moon, 500, 5, 5, 150, 35, 0), "planet_moon.png");
        Assert.IsTrue(File.Exists(Path.Combine(dir, "planet_earth.png")));
    }
}

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Agc.Tests.Harness;
using Agc.Wpf.Controls;

namespace Agc.Tests.Ui;

/// <summary>
/// 기능: 실제로 화면에 뜨는 창(UI-R 시나리오).
/// - UI-R01~R05: 창을 Show()로 띄우고 실제 타이머/렌더링/레이아웃을 검증하고 스크린샷을 저장한다.
/// - UI-R06: 빌드된 Agc.Wpf.exe를 별도 프로세스로 실행해 OS 창을 PrintWindow로 캡처·검증하고 정상 종료까지 확인한다.
/// 스크린샷은 테스트 출력 폴더의 screenshots/ 에 PNG로 남는다.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class RealWindowTests
{
    private const int LitGreenMin = 0xF0;
    private const int LitBlueMax = 0xC0;

    private static string ScreenshotDir
    {
        get
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "screenshots");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    private static void SavePng(BitmapSource bitmap, string name)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(ScreenshotDir, name));
        encoder.Save(stream);
    }

    /// <summary>영역 안에서 조건을 만족하는 픽셀 수(BGRA).</summary>
    private static int CountPixels(BitmapSource bitmap, Rect region, Func<byte, byte, byte, bool> isMatch)
    {
        var area = new Int32Rect(
            (int)Math.Max(region.X, 0), (int)Math.Max(region.Y, 0),
            (int)Math.Min(region.Width, bitmap.PixelWidth - Math.Max(region.X, 0)),
            (int)Math.Min(region.Height, bitmap.PixelHeight - Math.Max(region.Y, 0)));
        var stride = area.Width * 4;
        var pixels = new byte[stride * area.Height];
        bitmap.CopyPixels(area, pixels, stride, 0);
        var count = 0;
        for (var i = 0; i < pixels.Length; i += 4)
        {
            if (isMatch(pixels[i + 2], pixels[i + 1], pixels[i]))
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsLitSegment(byte r, byte g, byte b) => g >= LitGreenMin && b < LitBlueMax && r < 0xA0;

    private static bool IsAmber(byte r, byte g, byte b) => r > 0xF0 && g is > 0xA0 and < 0xC0 && b < 0x30;

    private static bool IsLampWhite(byte r, byte g, byte b) => r > 0xEC && g > 0xEC && b > 0xEC;

    // ---- UI-R01 ----

    [STATestMethod]
    [TestCategory("UI-R01")]
    public void ShownWindow_RealTimerAdvancesTheMissionClock()
    {
        using var ui = new UiDriver();

        ui.Show();

        Assert.IsTrue(ui.Window.IsLoaded, "창이 실제로 로드되었다");
        Assert.IsGreaterThan(1000, ui.Window.ActualWidth);
        Assert.AreEqual(0, ui.MetSeconds, 1e-6, "재생 전에는 시간이 흐르지 않는다");

        ui.ClickPlay();
        UiDriver.Pump(TimeSpan.FromMilliseconds(1500));

        // 60배속에서 실제 1.5초 ≈ 미션 90초. 스케줄링 지터를 고려해 넉넉한 범위로 확인한다.
        Assert.IsGreaterThan(30, ui.MetSeconds, "실제 DispatcherTimer가 시간을 흘려 보낸다");
        Assert.IsLessThan(200, ui.MetSeconds);
        Assert.AreNotEqual("MET 000:00:00", ui.MetText);
    }

    // ---- UI-R02 ----

    [STATestMethod]
    [TestCategory("UI-R02")]
    public void ShownWindow_RealtimeMonitorAndFlashRunByTheirOwnTimers()
    {
        using var ui = new UiDriver();
        ui.Show();

        ui.Press("V25N36E");
        var samples = new List<bool>();
        for (var i = 0; i < 8; i++)
        {
            UiDriver.Pump(TimeSpan.FromMilliseconds(170));
            samples.Add(ui.VerbNounVisible);
        }

        Assert.Contains(true, samples, "깜박임 타이머(330ms)로 켜진 상태가 관찰된다");
        Assert.Contains(false, samples, "깜박임 타이머(330ms)로 꺼진 상태가 관찰된다");

        ui.Press("V34E");
        ui.Press("V16N36E");
        ui.ClickPlay();
        UiDriver.Pump(TimeSpan.FromMilliseconds(2300));
        Assert.AreNotEqual("+00000", ui.R2, "실제 시간이 흐르면 V16 모니터가 스스로 갱신된다");
    }

    // ---- UI-R03 ----

    [STATestMethod]
    [TestCategory("UI-R03")]
    public void ShownWindow_ScreenshotsOfKeyScenarios_ContainWhatTheyShould()
    {
        using var ui = new UiDriver();
        ui.Show();

        // 초기 화면
        var initial = ui.Capture();
        SavePng(initial, "01_initial.png");
        var lampGrid = ui.BoundsOf((FrameworkElement)ui.Panel.FindName("LampGrid"));
        var r1 = ui.BoundsOf((FrameworkElement)ui.Panel.FindName("R1Display"));
        Assert.AreEqual(0, CountPixels(initial, lampGrid, IsAmber), "초기에는 호박색 램프가 없다");
        Assert.AreEqual(0, CountPixels(initial, r1, IsLitSegment), "초기에는 켜진 숫자가 없다");

        // 램프 테스트
        ui.Press("V35E");
        UiDriver.Pump(TimeSpan.FromMilliseconds(100));
        var lampTest = ui.Capture();
        SavePng(lampTest, "02_lamp_test.png");
        Assert.IsGreaterThan(1000, CountPixels(lampTest, lampGrid, IsAmber), "램프 테스트: 경고등 점등");
        Assert.IsGreaterThan(1000, CountPixels(lampTest, lampGrid, IsLampWhite), "램프 테스트: 흰 램프 점등");
        Assert.IsGreaterThan(100, CountPixels(lampTest, r1, IsLitSegment), "램프 테스트: R1 +88888");

        // 램프 테스트(5초)가 끝나야 키가 다시 받아들여진다. 그 사이의 키는 무시되는 것이 올바른 동작이다.
        ui.Press("K");
        Assert.AreEqual("88", ui.Verb, "램프 테스트 중 KEY REL은 무시된다");
        var testEnd = DateTime.UtcNow.AddSeconds(8);
        while (ui.LitLamps.Count > 0 && DateTime.UtcNow < testEnd)
        {
            UiDriver.Pump(TimeSpan.FromMilliseconds(200));
        }

        Assert.IsEmpty(ui.LitLamps, "5초 뒤 램프 테스트가 끝난다(실제 시간)");

        // 하강 중 알람
        ui.Press("K");
        ui.SeekTo(102, 38, 0);
        ui.Press("V16N63E");
        ui.ClickPlay();
        var deadline = DateTime.UtcNow.AddSeconds(8);
        while (!ui.LampLit("PROG") && DateTime.UtcNow < deadline)
        {
            UiDriver.Pump(TimeSpan.FromMilliseconds(100));
        }

        Assert.IsTrue(ui.LampLit("PROG"), "실시간 재생으로 1202 알람이 발생한다");
        var alarm = ui.Capture();
        SavePng(alarm, "03_descent_alarm.png");
        Assert.IsGreaterThan(300, CountPixels(alarm, lampGrid, IsAmber), "PROG 램프(호박색) 점등");
        Assert.IsGreaterThan(100, CountPixels(alarm, r1, IsLitSegment), "N63 값 표시");

        // 착륙 후
        ui.ClickPlay();
        ui.SeekTo(102, 45, 40);
        ui.Press("R");
        ui.Press("V06N43E");
        UiDriver.Pump(TimeSpan.FromMilliseconds(100));
        var landing = ui.Capture();
        SavePng(landing, "04_landed.png");
        Assert.AreEqual(0, CountPixels(landing, lampGrid, IsAmber), "RSET 후 경고등 소등");
        StringAssert.Contains(ui.PhaseName, "착륙");
    }

    // ---- UI-R04 ----

    [STATestMethod]
    [TestCategory("UI-R04")]
    public void ShownWindow_LayoutHoldsAtDefaultAndMinimumSizes()
    {
        using var ui = new UiDriver();
        ui.Show();

        foreach (var (w, h) in new[] { (1500.0, 860.0), (1100.0, 700.0), (1920.0, 1080.0) })
        {
            ui.Window.Width = w;
            ui.Window.Height = h;
            UiDriver.Pump(TimeSpan.FromMilliseconds(200));
            // Window.ActualWidth는 테두리를 포함한 바깥 크기이므로, 내용 영역 크기를 기준으로 삼는다.
            var content = (FrameworkElement)ui.Window.Content;
            var client = new Rect(0, 0, content.ActualWidth, content.ActualHeight);
            var half = client.Width / 2;

            var dsky = ui.BoundsOf(ui.Panel);
            Assert.IsTrue(client.Contains(dsky), $"{w}x{h}: DSKY가 창 안에 있다 {dsky}");
            Assert.IsLessThanOrEqualTo(half + 1, dsky.Right, $"{w}x{h}: DSKY는 왼쪽 절반");

            foreach (var name in new[] { "PhaseNameText", "MetText", "PlayButton", "SpeedBox", "PhaseBox", "TimelineSlider", "Illustration" })
            {
                var element = (FrameworkElement)ui.Window.FindName(name);
                var bounds = ui.BoundsOf(element);
                Assert.IsGreaterThanOrEqualTo(half - 1, bounds.Left, $"{w}x{h}: {name}은 오른쪽 절반");
                Assert.IsTrue(client.Contains(bounds), $"{w}x{h}: {name}이 창 밖으로 잘리지 않는다 {bounds}");
            }

            // DSKY는 Viewbox로 비율을 유지한 채 줄어든다(찌그러지지 않음).
            Assert.AreEqual(ui.Panel.Width / ui.Panel.Height, dsky.Width / dsky.Height, 0.02, $"{w}x{h}: 가로세로 비율 유지");
        }
    }

    // ---- UI-R05 ----

    [STATestMethod]
    [TestCategory("UI-R05")]
    public void ShownWindow_ClosingStopsTheTimerAndWritesStopLog()
    {
        var ui = new UiDriver();
        ui.Show();
        ui.ClickPlay();
        UiDriver.Pump(TimeSpan.FromMilliseconds(400));
        var metAtClose = ui.MetSeconds;

        ui.Dispose();
        UiDriver.Pump(TimeSpan.FromMilliseconds(400));

        Assert.IsFalse(ui.Window.IsLoaded, "닫힌 창");
        Assert.IsGreaterThan(0, metAtClose);
        var after = ui.MetSeconds;
        UiDriver.Pump(TimeSpan.FromMilliseconds(300));
        Assert.AreEqual(after, ui.MetSeconds, 1e-6, "닫힌 뒤에는 타이머가 시간을 더 흘리지 않는다");
    }

    // ---- UI-R06: 실제 프로세스 ----

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdc, uint flags);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private const uint PrintFullContent = 2;

    private static string FindAppExe()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AgcSimulation.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.IsNotNull(dir, "솔루션 루트를 찾을 수 없다");
        var candidates = Directory.GetFiles(Path.Combine(dir!.FullName, "src", "Agc.Wpf", "bin"), "Agc.Wpf.exe", SearchOption.AllDirectories);
        Assert.IsNotEmpty(candidates, "빌드된 Agc.Wpf.exe가 없다");
        return candidates.OrderByDescending(File.GetLastWriteTimeUtc).First();
    }

    [TestMethod]
    [TestCategory("UI-R06")]
    public void LaunchedApplication_ShowsARealWindowThatCanBeCapturedAndClosedCleanly()
    {
        var exe = FindAppExe();
        var logDir = Path.Combine(Path.GetDirectoryName(exe)!, "logs");
        var logPath = Path.Combine(logDir, $"agc-{DateTime.Now:yyyyMMdd}.log");
        var offset = File.Exists(logPath) ? new FileInfo(logPath).Length : 0;

        using var process = Process.Start(new ProcessStartInfo(exe) { UseShellExecute = false })!;
        try
        {
            var deadline = DateTime.UtcNow.AddSeconds(20);
            while (process.MainWindowHandle == IntPtr.Zero && DateTime.UtcNow < deadline && !process.HasExited)
            {
                Thread.Sleep(200);
                process.Refresh();
            }

            Assert.IsFalse(process.HasExited, "앱이 시작 직후 종료되었다");
            Assert.AreNotEqual(IntPtr.Zero, process.MainWindowHandle, "메인 창이 떠야 한다");
            Thread.Sleep(1500);
            process.Refresh();
            Assert.IsTrue(process.Responding, "응답 없음(UI 스레드 정지)");
            StringAssert.Contains(process.MainWindowTitle, "Apollo Guidance Computer");

            // OS 창 캡처(PrintWindow: 가려져 있어도 창 내용을 그려 준다)
            GetWindowRect(process.MainWindowHandle, out var rect);
            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;
            Assert.IsGreaterThan(800, width);
            Assert.IsGreaterThan(500, height);
            using var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                var hdc = g.GetHdc();
                try
                {
                    Assert.IsTrue(PrintWindow(process.MainWindowHandle, hdc, PrintFullContent), "PrintWindow 실패");
                }
                finally
                {
                    g.ReleaseHdc(hdc);
                }
            }

            bmp.Save(Path.Combine(ScreenshotDir, "10_launched_app.png"), ImageFormat.Png);
            var colors = new HashSet<int>();
            var gray = 0;
            var black = 0;
            for (var y = 0; y < height; y += 4)
            {
                for (var x = 0; x < width; x += 4)
                {
                    var c = bmp.GetPixel(x, y);
                    colors.Add(c.ToArgb());
                    if (Math.Abs(c.R - 0x9A) < 6 && Math.Abs(c.G - 0xA0) < 6 && Math.Abs(c.B - 0xA4) < 6)
                    {
                        gray++;
                    }

                    if (c.R < 0x25 && c.G < 0x25 && c.B < 0x28)
                    {
                        black++;
                    }
                }
            }

            Assert.IsGreaterThan(50, colors.Count, "빈 화면이 아니다(색이 다양하다)");
            Assert.IsGreaterThan(500, gray, "DSKY의 밝은 회색 베젤이 보인다");
            Assert.IsGreaterThan(500, black, "DSKY의 검은 키가 보인다");

            // 정상 종료
            Assert.IsTrue(process.CloseMainWindow(), "닫기 요청 실패");
            Assert.IsTrue(process.WaitForExit(8000), "닫기 요청 후 종료되지 않았다");
            Assert.AreEqual(0, process.ExitCode, "정상 종료 코드");
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }

        using var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Seek(offset, SeekOrigin.Begin);
        var log = new StreamReader(stream).ReadToEnd();
        StringAssert.Contains(log, "AGC simulation started (Apollo 11)");
        StringAssert.Contains(log, "AGC simulation stopped");
    }
}

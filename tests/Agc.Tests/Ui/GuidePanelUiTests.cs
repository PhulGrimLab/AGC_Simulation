using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Agc.Tests.Harness;

namespace Agc.Tests.Ui;

/// <summary>
/// 기능: 오른쪽 단계 가이드 패널(UI-U 시나리오). 단계마다 미션 설명, AGC 역할, 조작 순서(조작/목적/영향)가 보이고,
/// 지원하는 DSKY 키 입력은 [실행] 버튼으로 DSKY에 눌러진다.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class GuidePanelUiTests
{
    private static int Occurrences(string text, string token)
    {
        var count = 0;
        for (var i = text.IndexOf(token, StringComparison.Ordinal); i >= 0; i = text.IndexOf(token, i + token.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }

    private static string Head(string text, int length) => text.Length <= length ? text : text[..length];

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

    [STATestMethod]
    [TestCategory("UI-U01")]
    public void EveryPhase_ShowsItsGuide_WithSummaryRoleAndAllSteps()
    {
        using var ui = new UiDriver();

        for (var i = 0; i < ui.Timeline.Keyframes.Count; i++)
        {
            var keyframe = ui.Timeline.Keyframes[i];
            var guide = Apollo11Guides.For(keyframe.PhaseName)!;
            ui.SelectPhase(i);

            var text = ui.GuidePanel.AllText;
            StringAssert.Contains(text, keyframe.PhaseName, "헤더에 단계 이름");
            StringAssert.Contains(text, "무슨 일이 일어나나");
            StringAssert.Contains(text, Head(guide.Summary, 12), $"미션 설명: {keyframe.PhaseName}");
            StringAssert.Contains(text, "AGC는 무엇을 하나");
            Assert.AreEqual(guide.Steps.Count, ui.GuidePanel.StepCount, $"조작 단계 수: {keyframe.PhaseName}");
            foreach (var role in guide.AgcRole)
            {
                StringAssert.Contains(text, Head(role, 10), $"AGC 역할: {keyframe.PhaseName}");
            }
        }
    }

    [STATestMethod]
    [TestCategory("UI-U02")]
    public void EveryStep_ShowsActionPurposeAndEffect()
    {
        using var ui = new UiDriver();

        for (var i = 0; i < ui.Timeline.Keyframes.Count; i++)
        {
            ui.SelectPhase(i);
            var guide = Apollo11Guides.For(ui.Timeline.Keyframes[i].PhaseName)!;
            var text = ui.GuidePanel.AllText;

            Assert.AreEqual(guide.Steps.Count, Occurrences(text, "조작  "), "조작 라벨 수");
            Assert.AreEqual(guide.Steps.Count, Occurrences(text, "목적  "), "목적 라벨 수");
            Assert.AreEqual(guide.Steps.Count, Occurrences(text, "영향  "), "영향 라벨 수");
            foreach (var step in guide.Steps)
            {
                StringAssert.Contains(text, step.Title);
                StringAssert.Contains(text, Head(step.Purpose, 10));
                StringAssert.Contains(text, Head(step.Effect, 10));
            }
        }
    }

    [STATestMethod]
    [TestCategory("UI-U03")]
    public void RunButtons_ExistExactlyForRunnableSteps_AndOthersSayUnsupported()
    {
        using var ui = new UiDriver();

        for (var i = 0; i < ui.Timeline.Keyframes.Count; i++)
        {
            ui.SelectPhase(i);
            var guide = Apollo11Guides.For(ui.Timeline.Keyframes[i].PhaseName)!;
            var runnable = guide.Steps.Count(s => s.Runnable);
            var unsupportedDsky = guide.Steps.Count(s => s.Kind == GuideStepKind.Dsky && !s.Runnable);

            Assert.AreEqual(runnable, ui.GuidePanel.RunButtons.Count, $"[실행] 버튼 수: {guide.PhaseName}");
            Assert.AreEqual(unsupportedDsky, Occurrences(ui.GuidePanel.AllText, "시뮬레이터 미지원"), $"미지원 표시: {guide.PhaseName}");
        }
    }

    [STATestMethod]
    [TestCategory("UI-U04")]
    public void RunButton_PressesTheKeysOnTheDsky_ForEveryRunnableStepWithoutError()
    {
        using var ui = new UiDriver();

        for (var i = 0; i < ui.Timeline.Keyframes.Count; i++)
        {
            ui.SelectPhase(i);
            var guide = Apollo11Guides.For(ui.Timeline.Keyframes[i].PhaseName)!;
            var runnable = guide.Steps.Where(s => s.Runnable).ToList();
            for (var k = 0; k < runnable.Count; k++)
            {
                ui.Press("RK");
                Click(ui.GuidePanel.RunButtons[k]);

                Assert.IsFalse(ui.LampLit("OPR ERR"), $"{guide.PhaseName} / {runnable[k].Title} / {runnable[k].Keys}");
                ui.AssertDisplayWellFormed();
                ui.Run(DskyController.LampTestDuration.TotalSeconds + 0.4);
            }
        }
    }

    [STATestMethod]
    [TestCategory("UI-U05")]
    public void RunButton_OfTheN63Step_ShowsTheDescentMonitorOnTheDsky()
    {
        using var ui = new UiDriver();
        ui.SelectPhase(ui.PhaseIndexOf("동력 하강 시작(PDI)"));
        var guide = Apollo11Guides.For("동력 하강 시작(PDI)")!;
        var steps = guide.Steps.Where(s => s.Runnable).ToList();

        var n63 = steps.FindIndex(s => s.Keys!.Contains("V16N63E") || s.Keys.Contains("V06N63E"));
        Assert.IsGreaterThanOrEqualTo(0, n63, "PDI 가이드에 N63 실행 단계가 있어야 한다");
        ui.Press("K");
        Click(ui.GuidePanel.RunButtons[n63]);

        Assert.AreEqual("63", ui.Noun);
        Assert.AreEqual("63", ui.Prog);
        Assert.IsFalse(ui.LampLit("OPR ERR"));
    }

    [STATestMethod]
    [TestCategory("UI-U06")]
    public void Guide_IsRebuiltOnlyWhenThePhaseChanges_NotEveryTick()
    {
        using var ui = new UiDriver();
        ui.SeekTo(102, 33, 5);
        var before = ui.GuidePanel.AllText;
        var firstButton = ui.GuidePanel.RunButtons.FirstOrDefault();
        ui.ClickPlay();

        ui.Run(2.0);

        Assert.AreEqual(before, ui.GuidePanel.AllText);
        Assert.AreSame(firstButton, ui.GuidePanel.RunButtons.FirstOrDefault(), "같은 단계에서는 버튼이 다시 만들어지지 않는다");

        ui.RunUntil(() => ui.PhaseName.Contains("P64"), 20);
        Assert.AreNotEqual(before, ui.GuidePanel.AllText, "단계가 바뀌면 가이드도 바뀐다");
        StringAssert.Contains(ui.GuidePanel.AllText, "접근 단계(P64)");
    }

    [STATestMethod]
    [TestCategory("UI-U07")]
    public void PoweredDescentGuide_ExplainsTheProgramAlarms()
    {
        using var ui = new UiDriver();

        ui.SelectPhase(ui.PhaseIndexOf("동력 하강 시작(PDI)"));
        var combined = ui.GuidePanel.AllText;
        ui.SelectPhase(ui.PhaseIndexOf("접근 단계(P64)"));
        combined += ui.GuidePanel.AllText;

        StringAssert.Contains(combined, "1202");
        StringAssert.Contains(combined, "1201");
        StringAssert.Contains(combined, "VERB 0 5 NOUN 0 9 ENTR", "알람 코드 읽기 키(V05 N09)");
        StringAssert.Contains(combined, "PROG");
    }

    [STATestMethod]
    [TestCategory("UI-U08")]
    public void Layout_GuideSitsBelowTheScene_InTheRightHalf_AndKeepsItsMinimumSizes()
    {
        using var ui = new UiDriver();
        ui.Show();

        foreach (var (w, h) in new[] { (1600.0, 980.0), (1100.0, 700.0), (1920.0, 1080.0) })
        {
            ui.Window.Width = w;
            ui.Window.Height = h;
            UiDriver.Pump(TimeSpan.FromMilliseconds(250));
            var content = (FrameworkElement)ui.Window.Content;
            var client = new Rect(0, 0, content.ActualWidth, content.ActualHeight);
            var guide = ui.BoundsOf(ui.GuidePanel);
            var scene = ui.BoundsOf((FrameworkElement)ui.Window.FindName("Illustration"));

            Assert.IsTrue(client.Contains(guide), $"{w}x{h}: 가이드가 창 안에 있다 {guide}");
            Assert.IsGreaterThanOrEqualTo(client.Width / 2 - 1, guide.Left, $"{w}x{h}: 오른쪽 절반");
            Assert.IsGreaterThanOrEqualTo(scene.Bottom - 1, guide.Top, $"{w}x{h}: 장면 아래");
            Assert.IsGreaterThanOrEqualTo(139.0, guide.Height, $"{w}x{h}: 가이드 최소 높이");
            Assert.IsGreaterThanOrEqualTo(199.0, scene.Height, $"{w}x{h}: 장면 최소 높이");
            Assert.IsGreaterThan(300.0, guide.Width, $"{w}x{h}: 가이드 폭");
        }
    }

    [STATestMethod]
    [TestCategory("UI-U09")]
    public void GuideCanBeScrolled_AndStartsAtTheTopWhenThePhaseChanges()
    {
        using var ui = new UiDriver();
        ui.Show();
        ui.SelectPhase(ui.PhaseIndexOf("동력 하강 시작(PDI)"));
        UiDriver.Pump(TimeSpan.FromMilliseconds(250));
        var scroll = FindScrollViewer(ui.GuidePanel)!;

        Assert.IsGreaterThan(0.0, scroll.ScrollableHeight, "PDI 가이드는 길어서 스크롤이 필요하다");
        scroll.ScrollToEnd();
        UiDriver.Pump(TimeSpan.FromMilliseconds(100));
        Assert.IsGreaterThan(0.0, scroll.VerticalOffset);

        ui.SelectPhase(ui.PhaseIndexOf("접근 단계(P64)"));
        UiDriver.Pump(TimeSpan.FromMilliseconds(250));

        Assert.AreEqual(0.0, scroll.VerticalOffset, 1e-6, "새 가이드는 맨 위부터 보인다");
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject root)
    {
        if (root is ScrollViewer sv)
        {
            return sv;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var found = FindScrollViewer(VisualTreeHelper.GetChild(root, i));
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }
}

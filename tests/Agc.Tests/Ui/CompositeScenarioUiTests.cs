using Agc.Tests.Harness;

namespace Agc.Tests.Ui;

/// <summary>
/// 기능: 여러 기능을 엮은 복합 UI 시나리오(UI-C). 미션 재생 + DSKY 조작 + 알람 + 시계 로드 + 단계 이동 + 로그를 한 흐름에서 검증한다.
/// 실제 사용자가 하는 순서(예: 하강 감시, 알람 읽고 해제)를 그대로 재현한다.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class CompositeScenarioUiTests
{
    private static int Val(string register)
    {
        return int.Parse(register.Trim());
    }

    [STATestMethod]
    [TestCategory("UI-C01")]
    public void FullDescent_P63ToP68_MonitorAltitudeProgramsAndFiveAlarms()
    {
        using var ui = new UiDriver();
        var touchdown = new TimeSpan(102, 45, 40).TotalSeconds;
        ui.SeekTo(102, 33, 5);
        ui.Press("V16N63E");
        Assert.AreEqual("63", ui.Prog);
        ui.SetSpeed(60);
        ui.ClickPlay();

        var programs = new List<string> { ui.Prog };
        var altitudes = new List<int> { Val(ui.R3) };
        var alarmDetections = 0;
        var guard = 0;
        while (ui.MetSeconds < touchdown && guard++ < 5000)
        {
            ui.Run(0.2);
            ui.AssertDisplayWellFormed();
            if (ui.Prog != programs[^1])
            {
                programs.Add(ui.Prog);
            }

            altitudes.Add(Val(ui.R3));
            if (ui.LampLit("PROG"))
            {
                alarmDetections++;
                ui.Press("R");
            }
        }

        ui.ClickPlay();
        ui.Run(1.0);

        CollectionAssert.AreEqual(new[] { "63", "64", "66", "68" }, programs, "P63 → P64 → P66 → P68");
        Assert.AreEqual(5, alarmDetections, "1202 ×4 + 1201 ×1을 한 번씩 감지");
        for (var i = 1; i < altitudes.Count; i++)
        {
            Assert.IsLessThanOrEqualTo(altitudes[i - 1], altitudes[i], $"계산 고도는 감소만 해야 한다 (index {i})");
        }

        Assert.AreEqual("+00000", ui.R3, "착륙 시 고도 0");
        Assert.AreEqual("68", ui.Prog);
    }

    [STATestMethod]
    [TestCategory("UI-C02")]
    public void AlarmReadAndClear_V05N09E_ThenRsetThenNextAlarmsAccumulate()
    {
        using var ui = new UiDriver();
        ui.SeekTo(102, 38, 0);
        ui.ClickPlay();

        Assert.IsTrue(ui.RunUntil(() => ui.LampLit("PROG"), 5), "첫 알람");
        ui.Press("V05N09E");
        Assert.AreEqual(" 01202", ui.R1, "알람 코드는 8진 표시(부호 칸 공백)");
        Assert.AreEqual("      ", ui.R2);

        ui.Press("R");
        Assert.IsFalse(ui.LampLit("PROG"), "RSET으로 PROG 램프 소등");
        Assert.AreEqual(" 01202", ui.R1, "코드는 남아 있다");

        Assert.IsTrue(ui.RunUntil(() => ui.R2 != "      ", 10), "두 번째 알람이 표시 중인 알람 창을 갱신");
        Assert.AreEqual(" 01202", ui.R1);
        Assert.AreEqual(" 01202", ui.R2);

        Assert.IsTrue(ui.RunUntil(() => ui.R1 == " 01201", 10), "102:42:17의 1201");
        Assert.AreEqual(" 01202", ui.R2);
        Assert.AreEqual(" 01202", ui.R3);

        // 1201 직후 60 미션초를 더 진행하면 102:42:41, 102:42:57의 1202 두 건이 지나간다.
        ui.Run(1.0);
        ui.ClickPlay();
        Assert.AreEqual(" 01202", ui.R1, "최신: 1202@42:57");
        Assert.AreEqual(" 01202", ui.R2, "그 전: 1202@42:41");
        Assert.AreEqual(" 01201", ui.R3, "그 전: 1201@42:17 (오래된 1202 둘은 밀려났다)");
    }

    [STATestMethod]
    [TestCategory("UI-C03")]
    public void ProgramOverride_V37E00E_RevertsAtNextMissionPhase()
    {
        using var ui = new UiDriver();
        ui.SeekTo(102, 33, 5);
        ui.Press("V37E00E");
        Assert.AreEqual("00", ui.Prog);
        ui.ClickPlay();

        var changed = ui.RunUntil(() => ui.Prog != "00", 15);

        Assert.IsTrue(changed, "제동 단계가 끝나는 102:41:32에 PROG가 바뀐다");
        Assert.AreEqual("64", ui.Prog);
        StringAssert.Contains(ui.MetText, "102:41:");
    }

    [STATestMethod]
    [TestCategory("UI-C04")]
    public void ClockLoad_ThenRunning_ClockKeepsAdvancingFromLoadedValue()
    {
        using var ui = new UiDriver();
        ui.Press("V25N36E+00200E+00000E+00000E");
        ui.Press("V16N36E");
        Assert.AreEqual("+00200", ui.R1);

        ui.ClickPlay();
        ui.Run(10.0);

        Assert.AreEqual("+00200", ui.R1);
        Assert.AreEqual("+00010", ui.R2, "60배속 10초 = 10분 경과");
        StringAssert.Contains(ui.MetText, "000:10:00", "미션 시간은 별개로 흐른다");
    }

    [STATestMethod]
    [TestCategory("UI-C05")]
    public void MonitorFollowsSeek_ToTouchdownAndBack()
    {
        using var ui = new UiDriver();
        ui.SeekTo(102, 33, 5);
        ui.Press("V16N63E");
        var atPdi = Val(ui.R3);

        ui.SeekTo(102, 45, 40);
        ui.Run(1.0);
        Assert.AreEqual("+00000", ui.R1, "착륙 시 속도 0");
        Assert.AreEqual("+00000", ui.R3, "착륙 시 고도 0");

        ui.SeekTo(102, 33, 5);
        ui.Run(1.0);
        Assert.AreEqual(atPdi, Val(ui.R3), "되돌아가면 처음 값과 같다");
    }

    [STATestMethod]
    [TestCategory("UI-C06")]
    public void LampTestWhileMissionRuns_IgnoresKeysThenResumes()
    {
        using var ui = new UiDriver();
        ui.SeekTo(102, 30, 0);
        ui.ClickPlay();
        var before = ui.MetSeconds;

        ui.Press("V35E");
        ui.Press("V16N36E");
        Assert.AreEqual("88", ui.Verb, "램프 테스트 중에는 키가 무시된다");
        ui.Run(5.2);

        Assert.IsEmpty(ui.LitLamps.Where(l => l != "PROG"), "램프 테스트 종료");
        Assert.AreEqual(before + 5.2 * 60, ui.MetSeconds, 1e-6, "그 동안에도 미션은 계속 진행된다");

        ui.Press("V16N36E");
        Assert.AreEqual("16", ui.Verb);
        Assert.IsFalse(ui.LampLit("OPR ERR"));
    }

    [STATestMethod]
    [TestCategory("UI-C07")]
    public void LoadInProgress_WhileMissionRuns_StillCompletes()
    {
        using var ui = new UiDriver();
        ui.SeekTo(100, 0, 0);
        ui.ClickPlay();
        ui.Press("V25N36E+00001E");
        ui.Run(3.0);
        ui.Press("+00002E");
        ui.Run(2.0);
        ui.Press("+00003E");

        ui.Press("V06N36E");

        Assert.AreEqual("+00001", ui.R1);
        Assert.AreEqual("+00002", ui.R2);
        Assert.IsFalse(ui.LampLit("OPR ERR"));
        Assert.IsTrue(ui.IsPlaying, "로드하는 동안에도 재생은 계속된다");
    }

    [STATestMethod]
    [TestCategory("UI-C08")]
    public void WholeMissionAt3600x_VisitsMajorProgramsAndKeepsLastThreeAlarms()
    {
        using var ui = new UiDriver();
        ui.SetSpeed(3600);
        ui.Press("V16N36E");
        var programs = new HashSet<string> { ui.Prog };
        var lastMet = 0.0;
        ui.ClickPlay();

        var guard = 0;
        while (ui.IsPlaying && guard++ < 3000)
        {
            ui.Run(0.2);
            programs.Add(ui.Prog);
            Assert.IsGreaterThanOrEqualTo(lastMet, ui.MetSeconds, "미션 시간은 되돌아가지 않는다");
            lastMet = ui.MetSeconds;
            ui.AssertDisplayWellFormed();
        }

        Assert.IsFalse(ui.IsPlaying, "미션 끝에서 정지");
        Assert.AreEqual("MET 195:18:35", ui.MetText);
        foreach (var program in new[] { "11", "00", "40", "68" })
        {
            CollectionAssert.Contains(programs.ToList(), program, $"P{program}");
        }

        Assert.IsTrue(ui.LampLit("PROG"), "지나온 알람으로 PROG 램프가 켜져 있다");
        ui.Press("V05N09E");
        Assert.AreEqual(" 01202", ui.R1, "마지막 알람 1202@102:42:57");
        Assert.AreEqual(" 01202", ui.R2, "그 전 1202@102:42:41");
        Assert.AreEqual(" 01201", ui.R3, "그 전 1201@102:42:17");
    }

    [STATestMethod]
    [TestCategory("UI-C09")]
    public void PositionNounAcrossEveryPhase_MatchesTheMissionDataExactly()
    {
        using var ui = new UiDriver();
        ui.Press("V16N43E");

        for (var i = 0; i < ui.Timeline.Keyframes.Count; i++)
        {
            ui.SelectPhase(i);
            ui.Run(1.0);

            var telemetry = ui.Timeline.GetSnapshot(ui.Timeline.Keyframes[i].Met).Telemetry;
            Assert.IsTrue(DskyNouns.TryFormat(DskyNouns.LatLongAltitude, telemetry, out var expected));
            var name = ui.Timeline.Keyframes[i].PhaseName;
            Assert.AreEqual(expected[0], ui.R1, $"위도 {name}");
            Assert.AreEqual(expected[1], ui.R2, $"경도 {name}");
            Assert.AreEqual(expected[2], ui.R3, $"고도 {name}");
        }
    }

    [STATestMethod]
    [TestCategory("UI-C10")]
    public void RangeAndTimeToGo_CountDownDuringBraking()
    {
        using var ui = new UiDriver();
        ui.SeekTo(102, 33, 5);
        ui.Press("V16N68E");
        var rangeAtStart = Val(ui.R1);
        Assert.AreEqual("+08 27", ui.R2);

        ui.ClickPlay();
        ui.Run(3.0);

        Assert.AreEqual("+05 27", ui.R2, "180초 경과: 8:27 → 5:27");
        Assert.IsLessThan(rangeAtStart, Val(ui.R1), "착륙 지점에 가까워진다");
    }

    [STATestMethod]
    [TestCategory("UI-C11")]
    public void PlayPauseResume_KeepsTimeContinuity()
    {
        using var ui = new UiDriver();

        ui.ClickPlay();
        ui.Run(1.0);
        ui.ClickPlay();
        ui.Run(2.0);
        Assert.AreEqual(60, ui.MetSeconds, 1e-6);

        ui.ClickPlay();
        ui.Run(1.0);

        Assert.AreEqual(120, ui.MetSeconds, 1e-6);
    }

    [STATestMethod]
    [TestCategory("UI-C12")]
    public void RandomKeyAndTimeStress_NeverBreaksTheDisplay()
    {
        using var ui = new UiDriver();
        var random = new Random(20260101);
        var labels = new[]
        {
            "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "+", "−",
            "VERB", "NOUN", "ENTR", "CLR", "PRO", "KEY REL", "RSET",
        };
        ui.SeekTo(102, 30, 0);
        ui.ClickPlay();

        for (var i = 0; i < 600; i++)
        {
            ui.Click(labels[random.Next(labels.Length)]);
            if (i % 7 == 0)
            {
                ui.Run(0.2);
            }

            if (i % 97 == 0)
            {
                ui.SetSpeed(new[] { 1, 10, 60, 600, 3600 }[random.Next(5)]);
            }

            ui.AssertDisplayWellFormed();
            StringAssert.StartsWith(ui.MetText, "MET ");
        }
    }

    [STATestMethod]
    [TestCategory("UI-C13")]
    public void SeekIntoMiddleOfAlarms_OnlyRaisesTheOnesAhead()
    {
        using var ui = new UiDriver();
        ui.SeekTo(102, 42, 0);
        ui.ClickPlay();
        var detections = 0;

        for (var i = 0; i < 300 && ui.MetSeconds < new TimeSpan(102, 44, 0).TotalSeconds; i++)
        {
            ui.Run(0.2);
            if (ui.LampLit("PROG"))
            {
                detections++;
                ui.Press("R");
            }
        }

        Assert.AreEqual(3, detections, "102:42:17, 102:42:41, 102:42:57 세 개만 남아 있다");
    }

    [STATestMethod]
    [TestCategory("UI-C14")]
    public void KeyReleaseDuringMonitorThenNewCommand_WorksWithoutResidue()
    {
        using var ui = new UiDriver();
        ui.SeekTo(102, 36, 0);
        ui.Press("V16N63E");
        ui.ClickPlay();
        ui.Run(2.0);

        ui.Press("K");
        Assert.AreEqual("      ", ui.R1);
        Assert.IsFalse(ui.LampLit("COMP ACTY"));
        ui.Run(2.0);
        Assert.AreEqual("      ", ui.R1, "KEY REL 이후에는 모니터가 갱신하지 않는다");

        ui.Press("V06N68E");
        Assert.IsGreaterThan(0, Val(ui.R1));
        Assert.IsFalse(ui.LampLit("OPR ERR"));
    }

    [STATestMethod]
    [TestCategory("UI-C15")]
    public void ProgramChangeAndPhaseJump_OverrideIsDroppedWhenJumpingToAnotherPhase()
    {
        using var ui = new UiDriver();
        ui.SeekTo(102, 33, 5);
        ui.Press("V37E00E");
        Assert.AreEqual("00", ui.Prog);

        ui.SelectPhase(ui.PhaseIndexOf("접근 단계(P64)"));
        Assert.AreEqual("64", ui.Prog, "다른 단계로 이동하면 그 단계의 프로그램");

        ui.SelectPhase(ui.PhaseIndexOf("동력 하강 시작(PDI)"));
        Assert.AreEqual("63", ui.Prog, "이전 단계로 돌아가도 이전 요청은 사라졌다");
    }

    [TestMethod]
    [TestCategory("UI-C16")]
    public void LogFile_RecordsPhasesAlarmsAndCommandsOfARun()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "logs", $"agc-{DateTime.Now:yyyyMMdd}.log");
        var offset = File.Exists(path) ? new FileInfo(path).Length : 0;
        RunLoggedScenarioOnSta();

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Seek(offset, SeekOrigin.Begin);
        var text = new StreamReader(stream).ReadToEnd();

        StringAssert.Contains(text, "Phase -> 동력 하강 시작(PDI) (P63)");
        StringAssert.Contains(text, "V16 N63 executed");
        StringAssert.Contains(text, "Program alarm 1202");
        StringAssert.Contains(text, "OPR ERR: digit without VERB/NOUN");
        StringAssert.Contains(text, "V37 P00 requested");
        StringAssert.Contains(text, "AGC simulation stopped", "창을 닫으면 종료 로그가 남는다");
    }

    private static void RunLoggedScenarioOnSta()
    {
        var thread = new Thread(() =>
        {
            using var ui = new UiDriver();
            ui.SeekTo(102, 33, 5);
            ui.Press("V16N63E");
            ui.Press("5");
            ui.Press("R");
            ui.Press("V37E00E");
            ui.ClickPlay();
            ui.RunUntil(() => ui.LampLit("PROG"), 10);
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }

    [STATestMethod]
    [TestCategory("UI-C17")]
    public void DisposingTheWindowTwice_IsHarmless()
    {
        var ui = new UiDriver();
        ui.Press("V35E");

        ui.Dispose();
        ui.Dispose();
    }
}

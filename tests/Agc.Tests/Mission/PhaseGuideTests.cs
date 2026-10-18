using Agc.Tests.Harness;

namespace Agc.Tests.Mission;

/// <summary>
/// 기능: 단계별 설명서(PhaseGuide) 데이터의 완전성과 정확성.
/// 모든 키프레임에 설명서가 있고, 화면에서 [실행]할 수 있는 키 입력(Runnable)은 실제로 그 단계의 시뮬레이터에서
/// 오류 없이 실행되어야 한다(설명서가 시뮬레이터가 못 하는 조작을 약속하면 안 된다).
/// </summary>
[TestClass]
public sealed class PhaseGuideTests
{
    private const string AllowedKeyChars = "0123456789VNECRKP+-";

    private static readonly MissionTimeline Timeline = Apollo11Mission.Create();

    [TestMethod]
    [TestCategory("GD-01")]
    public void EveryKeyframe_HasExactlyOneGuide_AndNoGuideIsOrphaned()
    {
        var names = Timeline.Keyframes.Select(k => k.PhaseName).ToList();

        foreach (var name in names)
        {
            Assert.AreEqual(1, Apollo11Guides.All.Count(g => g.PhaseName == name), $"설명서가 정확히 1개 필요: {name}");
        }

        foreach (var guide in Apollo11Guides.All)
        {
            CollectionAssert.Contains(names, guide.PhaseName, $"키프레임에 없는 설명서: {guide.PhaseName}");
        }
    }

    [TestMethod]
    [TestCategory("GD-02")]
    public void EveryGuide_HasSummaryRoleStepsAndReference()
    {
        foreach (var g in Apollo11Guides.All)
        {
            Assert.IsGreaterThan(40, g.Summary.Length, $"미션 설명이 충분해야 한다: {g.PhaseName}");
            Assert.IsNotEmpty(g.AgcRole, $"AGC 역할: {g.PhaseName}");
            Assert.IsTrue(g.AgcRole.All(r => r.Length > 8), $"AGC 역할 항목: {g.PhaseName}");
            Assert.IsNotEmpty(g.Steps, $"조작 순서: {g.PhaseName}");
            StringAssert.StartsWith(g.Reference, "doc/", $"참조 위치: {g.PhaseName}");
        }
    }

    [TestMethod]
    [TestCategory("GD-03")]
    public void EveryStep_ExplainsActionPurposeAndEffect()
    {
        foreach (var g in Apollo11Guides.All)
        {
            foreach (var s in g.Steps)
            {
                var where = $"{g.PhaseName} / {s.Title}";
                Assert.IsFalse(string.IsNullOrWhiteSpace(s.Title), where);
                Assert.IsGreaterThan(6, s.Action.Length, $"조작 설명: {where}");
                Assert.IsGreaterThan(6, s.Purpose.Length, $"목적: {where}");
                Assert.IsGreaterThan(6, s.Effect.Length, $"영향: {where}");
            }
        }
    }

    [TestMethod]
    [TestCategory("GD-04")]
    public void DskyKeys_UseOnlyTheKnownNotation_AndNonDskyStepsHaveNoKeys()
    {
        foreach (var g in Apollo11Guides.All)
        {
            foreach (var s in g.Steps)
            {
                var where = $"{g.PhaseName} / {s.Title}";
                if (s.Kind == GuideStepKind.Dsky)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(s.Keys), $"DSKY 단계는 키 입력이 필요: {where}");
                    Assert.IsTrue(s.Keys!.All(c => AllowedKeyChars.Contains(c)), $"알 수 없는 키 표기: {s.Keys} ({where})");
                }
                else
                {
                    Assert.IsNull(s.Keys, $"DSKY가 아닌 단계는 키 입력이 없어야 한다: {where}");
                    Assert.IsFalse(s.Runnable, $"DSKY가 아닌 단계는 실행할 수 없다: {where}");
                }
            }
        }
    }

    [TestMethod]
    [TestCategory("GD-05")]
    public void RunnableSteps_ExecuteWithoutOperatorErrorAtTheirPhase()
    {
        foreach (var guide in Apollo11Guides.All)
        {
            var keyframe = Timeline.Keyframes.Single(k => k.PhaseName == guide.PhaseName);
            var sim = new MissionSimulator(Timeline, NullLog.Instance);
            var dsky = new DskyController(sim, NullLog.Instance, sim);
            _ = new AgcMissionLink(sim, dsky);
            sim.Seek(keyframe.Met);

            foreach (var step in guide.Steps.Where(s => s.Runnable))
            {
                dsky.Press("RK");
                dsky.Press(step.Keys!);
                var lamps = dsky.Display.Lamps;
                Assert.IsFalse(lamps.HasFlag(DskyLamps.OprErr), $"실행 불가: {guide.PhaseName} / {step.Title} / {step.Keys}");
                dsky.Update(DskyController.LampTestDuration + TimeSpan.FromSeconds(1));
            }
        }
    }

    [TestMethod]
    [TestCategory("GD-06")]
    public void NonRunnableDskySteps_AreNotAlsoMarkedConfirmedWithoutASource()
    {
        // 일반/미확인 확인수준이 아닌데 실행 불가인 DSKY 단계는 "이 시뮬레이터가 지원하지 않는 확인된 조작"이다.
        // 그런 단계가 있다면 Action에 시뮬레이터 미지원임을 밝혀야 한다.
        foreach (var g in Apollo11Guides.All)
        {
            foreach (var s in g.Steps.Where(s => s.Kind == GuideStepKind.Dsky && !s.Runnable))
            {
                StringAssert.Contains(s.Action + s.Effect, "시뮬레이터", $"미지원 사유를 밝혀야 한다: {g.PhaseName} / {s.Title}");
            }
        }
    }

    [TestMethod]
    [TestCategory("GD-07")]
    public void SomeSteps_AreRunnable_AndPoweredDescentHasTheKeySequences()
    {
        Assert.IsGreaterThan(8, Apollo11Guides.All.SelectMany(g => g.Steps).Count(s => s.Runnable), "따라 해 볼 수 있는 단계가 충분해야 한다");
        var descent = Apollo11Guides.For("동력 하강 시작(PDI)")!;
        var keys = descent.Steps.Select(s => s.Keys).Where(k => k is not null).ToList();
        Assert.IsTrue(keys.Any(k => k!.Contains("V16N63E")), "N63 모니터");
        Assert.IsTrue(keys.Any(k => k!.Contains("V16N68E") || k!.Contains("V06N68E")), "N68 확인");
    }
}

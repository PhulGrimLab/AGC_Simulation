namespace Agc.Tests.Dsky;

/// <summary>기능: DSKY 키 표기법 변환(DskyKeyNotation) — 단계 가이드의 키 순서를 파싱/설명한다.</summary>
[TestClass]
public sealed class DskyKeyNotationTests
{
    [TestMethod]
    public void Parse_ConvertsEveryNotationCharacter()
    {
        var keys = DskyKeyNotation.Parse("0123456789VNECRKP+-");

        CollectionAssert.AreEqual(
            new[]
            {
                DskyKey.Digit0, DskyKey.Digit1, DskyKey.Digit2, DskyKey.Digit3, DskyKey.Digit4, DskyKey.Digit5,
                DskyKey.Digit6, DskyKey.Digit7, DskyKey.Digit8, DskyKey.Digit9, DskyKey.Verb, DskyKey.Noun,
                DskyKey.Enter, DskyKey.Clear, DskyKey.Reset, DskyKey.KeyRelease, DskyKey.Proceed, DskyKey.Plus, DskyKey.Minus,
            },
            keys.ToArray());
    }

    [TestMethod]
    public void Parse_IgnoresSpaces_AndRejectsUnknownCharacters()
    {
        Assert.HasCount(7, DskyKeyNotation.Parse("V 1 6 N 3 6 E"));
        Assert.ThrowsExactly<ArgumentException>(() => DskyKeyNotation.Parse("V16X"));
    }

    [TestMethod]
    public void IsValid_AcceptsNotationOnly()
    {
        Assert.IsTrue(DskyKeyNotation.IsValid("V37E63E"));
        Assert.IsTrue(DskyKeyNotation.IsValid("V25N36E+00102E"));
        Assert.IsFalse(DskyKeyNotation.IsValid(""));
        Assert.IsFalse(DskyKeyNotation.IsValid("V16N63e"), "소문자는 표기법이 아니다");
        Assert.IsFalse(DskyKeyNotation.IsValid("V 16"), "공백은 IsValid에서는 허용하지 않는다");
    }

    [TestMethod]
    public void Describe_UsesTheKeypadLabels()
    {
        Assert.AreEqual("VERB 1 6 NOUN 6 3 ENTR", DskyKeyNotation.Describe("V16N63E"));
        Assert.AreEqual("VERB 3 5 ENTR", DskyKeyNotation.Describe("V35E"));
        Assert.AreEqual("KEY REL RSET PRO CLR", DskyKeyNotation.Describe("KRPC"));
        Assert.AreEqual("+ 0 0 1 0 2 ENTR", DskyKeyNotation.Describe("+00102E"));
        Assert.AreEqual("− 5", DskyKeyNotation.Describe("-5"));
    }

    [TestMethod]
    public void ParsedKeys_DriveTheController_SameAsTypingThem()
    {
        var telemetry = new Harness.FakeTelemetry { Current = Harness.FakeTelemetry.Make(program: 63) };
        var dsky = new DskyController(telemetry, NullLog.Instance);

        foreach (var key in DskyKeyNotation.Parse("V16N36E"))
        {
            dsky.PressKey(key);
        }

        Assert.AreEqual("16", dsky.Display.Verb);
        Assert.AreEqual("36", dsky.Display.Noun);
    }
}

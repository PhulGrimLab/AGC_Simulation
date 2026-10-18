using System.Windows.Input;
using System.Windows.Interop;
using Agc.Tests.Harness;
using Agc.Wpf;

namespace Agc.Tests.Ui;

/// <summary>
/// 기능: PC 키보드 → DSKY 키 매핑(UI-K 시나리오). 매핑 표 자체와, 창에서 실제로 DSKY에 반영되는 흐름을 모두 검증한다.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class KeyboardMappingTests
{
    // ---- 매핑 표 (UI-K01 ~ K09) ----

    [TestMethod]
    [TestCategory("UI-K01")]
    public void TopRowDigits_MapToDigitKeys()
    {
        for (var d = 0; d <= 9; d++)
        {
            var key = Key.D0 + d;
            Assert.AreEqual(DskyKey.Digit0 + d, DskyKeyboardMap.TryMap(key, ModifierKeys.None), $"{key}");
        }
    }

    [TestMethod]
    [TestCategory("UI-K02")]
    public void NumpadDigits_MapToDigitKeys()
    {
        for (var d = 0; d <= 9; d++)
        {
            var key = Key.NumPad0 + d;
            Assert.AreEqual(DskyKey.Digit0 + d, DskyKeyboardMap.TryMap(key, ModifierKeys.None), $"{key}");
        }
    }

    [TestMethod]
    [TestCategory("UI-K03")]
    public void ShiftedTopRowDigits_AreIgnored()
    {
        // Shift+1 = '!' 같은 기호이므로 숫자 키로 보지 않는다.
        for (var d = 0; d <= 9; d++)
        {
            Assert.IsNull(DskyKeyboardMap.TryMap(Key.D0 + d, ModifierKeys.Shift), $"Shift+D{d}");
        }
    }

    [TestMethod]
    [TestCategory("UI-K04")]
    public void PlusKeys_NumpadAddAndShiftEquals()
    {
        Assert.AreEqual(DskyKey.Plus, DskyKeyboardMap.TryMap(Key.Add, ModifierKeys.None));
        Assert.AreEqual(DskyKey.Plus, DskyKeyboardMap.TryMap(Key.OemPlus, ModifierKeys.Shift));
        Assert.IsNull(DskyKeyboardMap.TryMap(Key.OemPlus, ModifierKeys.None), "Shift 없는 '='는 + 가 아니다");
    }

    [TestMethod]
    [TestCategory("UI-K05")]
    public void MinusKeys_NumpadSubtractAndHyphen()
    {
        Assert.AreEqual(DskyKey.Minus, DskyKeyboardMap.TryMap(Key.Subtract, ModifierKeys.None));
        Assert.AreEqual(DskyKey.Minus, DskyKeyboardMap.TryMap(Key.OemMinus, ModifierKeys.None));
    }

    [TestMethod]
    [TestCategory("UI-K06")]
    public void LetterAndControlKeys_MapToInstructionKeys()
    {
        var expected = new Dictionary<Key, DskyKey>
        {
            [Key.V] = DskyKey.Verb,
            [Key.N] = DskyKey.Noun,
            [Key.Enter] = DskyKey.Enter,
            [Key.C] = DskyKey.Clear,
            [Key.Back] = DskyKey.Clear,
            [Key.R] = DskyKey.Reset,
            [Key.K] = DskyKey.KeyRelease,
            [Key.P] = DskyKey.Proceed,
        };

        foreach (var (key, dsky) in expected)
        {
            Assert.AreEqual(dsky, DskyKeyboardMap.TryMap(key, ModifierKeys.None), $"{key}");
        }
    }

    [TestMethod]
    [TestCategory("UI-K07")]
    public void CtrlOrAltCombinations_AreIgnored()
    {
        Assert.IsNull(DskyKeyboardMap.TryMap(Key.C, ModifierKeys.Control), "Ctrl+C(복사)는 CLR이 아니다");
        Assert.IsNull(DskyKeyboardMap.TryMap(Key.V, ModifierKeys.Control), "Ctrl+V(붙여넣기)는 VERB가 아니다");
        Assert.IsNull(DskyKeyboardMap.TryMap(Key.N, ModifierKeys.Alt));
        Assert.IsNull(DskyKeyboardMap.TryMap(Key.D5, ModifierKeys.Control | ModifierKeys.Shift));
    }

    [TestMethod]
    [TestCategory("UI-K08")]
    public void UnmappedKeys_ReturnNull()
    {
        foreach (var key in new[] { Key.A, Key.Z, Key.Space, Key.Tab, Key.Left, Key.F5, Key.Escape, Key.Delete, Key.D })
        {
            Assert.IsNull(DskyKeyboardMap.TryMap(key, ModifierKeys.None), $"{key}");
        }
    }

    [TestMethod]
    [TestCategory("UI-K09")]
    public void EveryDskyKey_IsReachableFromSomePcKey()
    {
        var reachable = Enum.GetValues<Key>()
            .Select(k => DskyKeyboardMap.TryMap(k, ModifierKeys.None) ?? DskyKeyboardMap.TryMap(k, ModifierKeys.Shift))
            .Where(k => k.HasValue)
            .Select(k => k!.Value)
            .ToHashSet();

        CollectionAssert.AreEquivalent(Enum.GetValues<DskyKey>().ToList(), reachable.ToList(), "DSKY 키 19개 모두 PC 키로 누를 수 있어야 한다");
    }

    // ---- 창에 반영 (UI-K10 ~ K13) ----

    [STATestMethod]
    [TestCategory("UI-K10")]
    public void KeyboardSequence_V35E_RunsLampTestInTheWindow()
    {
        using var ui = new UiDriver();

        foreach (var key in new[] { Key.V, Key.D3, Key.D5, Key.Enter })
        {
            Assert.IsTrue(ui.KeyDown(key), $"{key}는 DSKY 키로 처리되어야 한다");
        }

        Assert.HasCount(UiDriver.AllLampLabels.Length, ui.LitLamps, "램프 테스트: 모든 램프 점등");
        Assert.AreEqual("88", ui.Verb);
        Assert.AreEqual("+88888", ui.R1);
    }

    [STATestMethod]
    [TestCategory("UI-K11")]
    public void KeyboardNumpadAndSigns_LoadAClockValue()
    {
        using var ui = new UiDriver();

        // V25 N36 ENTR 후 +00102 ENTR 처럼 넘패드/부호 키로 입력
        ui.KeyDown(Key.V);
        ui.KeyDown(Key.NumPad2);
        ui.KeyDown(Key.NumPad5);
        ui.KeyDown(Key.N);
        ui.KeyDown(Key.D3);
        ui.KeyDown(Key.D6);
        ui.KeyDown(Key.Enter);
        ui.KeyDown(Key.Add);
        ui.KeyDown(Key.NumPad0);
        ui.KeyDown(Key.NumPad0);

        Assert.AreEqual("+00   ", ui.R1);
        Assert.IsTrue(ui.KeyDown(Key.OemMinus), "'-' 키는 − 로 입력된다");
        Assert.IsTrue(ui.LampLit("OPR ERR"), "숫자 중간에 부호는 오류");
    }

    [STATestMethod]
    [TestCategory("UI-K12")]
    public void RealKeyDownEvent_IsRoutedAndMarkedHandled()
    {
        using var ui = new UiDriver();
        using var source = new HwndSource(new HwndSourceParameters("agc-test"));

        var args = new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.V) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
        ui.Window.RaiseEvent(args);

        Assert.IsTrue(args.Handled, "DSKY 키는 처리됨으로 표시되어 다른 컨트롤이 가로채지 않는다");
        Assert.AreEqual("  ", ui.Verb, "VERB 키를 누르면 VERB 창이 비워진다(입력 대기)");
    }

    [STATestMethod]
    [TestCategory("UI-K13")]
    public void UnmappedKeyEvent_IsNotHandled()
    {
        using var ui = new UiDriver();
        using var source = new HwndSource(new HwndSourceParameters("agc-test"));

        var args = new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Left) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
        ui.Window.RaiseEvent(args);

        Assert.IsFalse(args.Handled, "DSKY와 무관한 키(슬라이더 방향키 등)는 기본 동작에 맡긴다");
    }

    [STATestMethod]
    [TestCategory("UI-K14")]
    public void MouseAndKeyboardCanBeMixedInOneCommand()
    {
        using var ui = new UiDriver();

        ui.KeyDown(Key.V);       // 키보드
        ui.Click("1");           // 마우스
        ui.KeyDown(Key.D6);      // 키보드
        ui.Click("NOUN");        // 마우스
        ui.Press("36");          // 마우스
        ui.KeyDown(Key.Enter);   // 키보드

        Assert.AreEqual("16", ui.Verb);
        Assert.AreEqual("36", ui.Noun);
        Assert.AreEqual("+00000", ui.R1);
        Assert.IsTrue(ui.LampLit("COMP ACTY"), "V16 모니터 동작");
    }
}

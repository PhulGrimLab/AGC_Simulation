"""기존 UI 테스트(DskyPanelUiTests, MainWindowUiTests)에 시나리오 ID(TestCategory)를 부여한다.

한 번만 실행하는 보조 스크립트. 이미 [TestCategory]가 붙은 메서드는 건너뛴다.
사용: python tools/assign_scenario_ids.py
"""
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parent.parent / "tests" / "Agc.Tests" / "Ui"

IDS = {
    "DskyPanelUiTests.cs": {
        "SegmentDisplay_Digit8_LightsMoreThanDigit1": "UI-P01",
        "SegmentDisplay_BlankText_LightsNothing": "UI-P02",
        "SegmentDisplay_PlusLightsMoreThanMinus": "UI-P03",
        "Panel_Display_UpdatesAllSegmentWindows": "UI-P04",
        "Panel_Lamps_LitColorsFollowRealDsky": "UI-P05",
        "Flash_VerbAndNounBlinkOnlyWhenDisplayRequestsIt": "UI-P06",
        "Flash_StopsAndStaysVisibleWhenFlashingEnds": "UI-P07",
        "Layout_LampPanel_HasRealTwoByFourteenOrder": "UI-P08",
        "Layout_DisplayPanel_HasFiveLinesInRealOrder": "UI-P09",
        "Layout_Keypad_HasNineteenKeysInSevenColumnsWithHalfRowOffsets": "UI-P10",
        "Layout_Keys_AreSquareWithinTheirCells": "UI-P11",
        "Panel_Keypad_EveryButtonRaisesItsKey": "UI-P12",
        "Panel_Keypad_ButtonsDoNotStealKeyboardFocus": "UI-P13",
        "Panel_FullDisplay_LitSegmentsAppearOnlyWhereValuesAreShown": "UI-P14",
    },
    "MainWindowUiTests.cs": {
        "Play_AfterMissionEnd_RestartsFromTheBeginning": "UI-W01",
        "PhaseBox_FollowsTheMissionPosition": "UI-W02",
        "PhaseBox_SelectingTheSamePhaseAgainStillJumps": "UI-W03",
    },
}

for file_name, mapping in IDS.items():
    path = ROOT / file_name
    lines = path.read_text(encoding="utf-8").splitlines(keepends=True)
    out = []
    for i, line in enumerate(lines):
        out.append(line)
        if line.strip() != "[STATestMethod]":
            continue
        # 다음 줄에 [TestCategory]가 이미 있으면 건너뛴다.
        if i + 1 < len(lines) and "[TestCategory" in lines[i + 1]:
            continue
        for j in range(i + 1, min(i + 4, len(lines))):
            m = re.search(r"public void (\w+)\(", lines[j])
            if m and m.group(1) in mapping:
                indent = line[: len(line) - len(line.lstrip())]
                out.append(f'{indent}[TestCategory("{mapping[m.group(1)]}")]\n')
                break
    path.write_text("".join(out), encoding="utf-8")
    print(file_name, "->", sum(1 for l in out if "[TestCategory" in l), "categories")

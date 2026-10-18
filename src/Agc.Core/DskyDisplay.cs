namespace Agc.Core;

/// <summary>
/// DSKY 화면 한 프레임의 스냅샷(값 객체).
/// Prog/Verb/Noun은 2글자, R1~R3는 부호 1글자 + 숫자 5글자(총 6글자)이며,
/// 비어 있는 자리는 공백(' ')으로 표현한다.
/// Vehicle은 지금 보여 주는 DSKY의 종류(사령선/달착륙선)로, 램프 패널의 ALT/VEL 칸 유무를 결정한다.
/// VerbNounFlashing이 true이면 VERB/NOUN 창이 깜박인다(사용자 입력/응답이 필요하다는 표시).
/// </summary>
public sealed record DskyDisplay(
    string Prog,
    string Verb,
    string Noun,
    string R1,
    string R2,
    string R3,
    DskyLamps Lamps,
    bool VerbNounFlashing = false,
    AgcVehicle Vehicle = AgcVehicle.Csm);

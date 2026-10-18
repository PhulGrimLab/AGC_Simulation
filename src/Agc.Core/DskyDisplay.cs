namespace Agc.Core;

/// <summary>
/// DSKY 화면 한 프레임의 스냅샷(값 객체).
/// Prog/Verb/Noun은 2글자, R1~R3는 부호 1글자 + 숫자 5글자(총 6글자)이며,
/// 비어 있는 자리는 공백(' ')으로 표현한다.
/// </summary>
public sealed record DskyDisplay(
    string Prog,
    string Verb,
    string Noun,
    string R1,
    string R2,
    string R3,
    DskyLamps Lamps);

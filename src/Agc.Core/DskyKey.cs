namespace Agc.Core;

/// <summary>
/// DSKY 키패드의 키 종류. Digit0~Digit9는 정수 값이 곧 숫자 값이 되도록 0~9로 고정한다.
/// </summary>
public enum DskyKey
{
    Digit0 = 0,
    Digit1 = 1,
    Digit2 = 2,
    Digit3 = 3,
    Digit4 = 4,
    Digit5 = 5,
    Digit6 = 6,
    Digit7 = 7,
    Digit8 = 8,
    Digit9 = 9,
    Plus,
    Minus,
    Verb,
    Noun,
    Clear,
    Proceed,
    KeyRelease,
    Enter,
    Reset,
}

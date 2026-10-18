namespace Agc.Core;

/// <summary>
/// DSKY 상태 램프 집합. 켜진 램프를 비트 플래그로 표현한다.
/// </summary>
[Flags]
public enum DskyLamps
{
    None = 0,
    CompActy = 1 << 0,
    UplinkActy = 1 << 1,
    NoAtt = 1 << 2,
    Stby = 1 << 3,
    KeyRel = 1 << 4,
    OprErr = 1 << 5,
    Temp = 1 << 6,
    GimbalLock = 1 << 7,
    Prog = 1 << 8,
    Restart = 1 << 9,
    Tracker = 1 << 10,
    Alt = 1 << 11,
    Vel = 1 << 12,
    All = (1 << 13) - 1,
}

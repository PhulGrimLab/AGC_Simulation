global using Agc.Core;
global using Agc.Mission;
global using System.IO;

namespace Agc.Tests.Harness;

/// <summary>
/// 테스트가 값을 직접 바꿀 수 있는 가짜 텔레메트리 공급자.
/// </summary>
internal sealed class FakeTelemetry : ITelemetrySource
{
    public AgcTelemetry Current { get; set; } = Make();

    /// <summary>필요한 값만 지정해 텔레메트리를 만든다(나머지는 0).</summary>
    public static AgcTelemetry Make(
        TimeSpan? met = null, int program = 0, double altitude = 0, double speed = 0,
        double verticalSpeed = 0, double lat = 0, double lon = 0,
        double range = 0, TimeSpan? timeToNextPhase = null)
    {
        return new AgcTelemetry(
            met ?? TimeSpan.Zero, program, altitude, speed, verticalSpeed, lat, lon, range, timeToNextPhase ?? TimeSpan.Zero);
    }
}

/// <summary>
/// 기록된 로그를 메모리에 모아 두는 가짜 로거. 로그 항목 검증에 사용한다.
/// </summary>
internal sealed class ListLog : ILog
{
    public List<string> Infos { get; } = [];

    public List<string> Warnings { get; } = [];

    public void Info(string message)
    {
        Infos.Add(message);
    }

    public void Warn(string message)
    {
        Warnings.Add(message);
    }
}

/// <summary>
/// DSKY 키 입력을 문자열로 간편하게 재생하는 헬퍼.
/// 표기: 0-9 숫자, V=VERB, N=NOUN, E=ENTR, C=CLR, R=RSET, K=KEY REL, P=PRO, +, -
/// </summary>
internal static class DskyDriver
{
    public static void Press(this DskyController dsky, string keys)
    {
        foreach (var c in keys)
        {
            dsky.PressKey(ToKey(c));
        }
    }

    private static DskyKey ToKey(char c)
    {
        return c switch
        {
            >= '0' and <= '9' => (DskyKey)(c - '0'),
            'V' => DskyKey.Verb,
            'N' => DskyKey.Noun,
            'E' => DskyKey.Enter,
            'C' => DskyKey.Clear,
            'R' => DskyKey.Reset,
            'K' => DskyKey.KeyRelease,
            'P' => DskyKey.Proceed,
            '+' => DskyKey.Plus,
            '-' => DskyKey.Minus,
            _ => throw new ArgumentException($"알 수 없는 키 표기: {c}"),
        };
    }
}

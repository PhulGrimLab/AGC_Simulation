namespace Agc.Core;

/// <summary>
/// 지원하는 Noun의 번호와 레지스터(R1~R3) 표시 형식을 정의한다. 상태가 없는 변환 유틸리티.
/// 정의는 Luminary 099 소스(PINBALL_NOUN_TABLES.agc)의 Noun 표를 따른다:
///   N09 ALARM CODES            3COMP OCTAL ONLY
///   N36 TIME OF AGC CLOCK      3COMP HMS (시, 분, 초×100)
///   N43 LAT / LONG / ALTITUDE  DPDEG XXX.XX deg, DPDEG XXX.XX deg, POS4 XXXX.X nm
///   N63 |V| / ALT RATE / COMPUTED ALTITUDE   VEL3 XXXX.X ft/s, VEL3 XXXX.X ft/s, COMP ALT XXXXX. ft
///   N68 SLANT RANGE TO LANDING SITE / TIME TO GO IN BRAKING / LR ALT - COMPUTED ALT
///                              POS7 XXXX.X nm, MIN/SEC XXbXX, COMP ALT XXXXX. ft
/// 십진 표시 레지스터는 항상 부호(+/-)를 가지며, 부호 칸이 공백인 것은 8진 표시(N09)뿐이다.
/// </summary>
internal static class DskyNouns
{
    public const int AlarmCodes = 9;
    public const int ClockTime = 36;
    public const int LatLongAltitude = 43;
    public const int LandingVelocity = 63;
    public const int LandingRange = 68;

    /// <summary>빈 레지스터(부호 1 + 숫자 5 = 6글자 공백).</summary>
    public const string BlankRegister = "      ";

    private const double MetersPerFoot = 0.3048;
    private const double MetersPerNauticalMile = 1852.0;
    private const double HundredthScale = 100.0;
    private const double TenthScale = 10.0;
    internal const int MillisecondsPerCentisecond = 10;
    internal const int CentisecondsPerSecond = 100;
    private const int SecondsPerMinute = 60;
    private const int MaxRegisterValue = 99999;
    private const int MaxMinSecMinutes = 99;
    private const int RegisterDigits = 5;

    /// <summary>데이터 로드(V21~V25)로 값을 바꿀 수 있는 Noun인지. 현재는 AGC 시계(N36)만 지원한다.</summary>
    public static bool IsLoadable(int noun)
    {
        return noun == ClockTime;
    }

    /// <summary>텔레메트리로 값을 만드는 Noun(36/43/63/68)인지.</summary>
    public static bool IsTelemetryNoun(int noun)
    {
        return noun is ClockTime or LatLongAltitude or LandingVelocity or LandingRange;
    }

    /// <summary>텔레메트리 기반 Noun의 레지스터 3개를 만든다. 미지원 Noun이면 false.</summary>
    public static bool TryFormat(int noun, AgcTelemetry t, out string[] registers)
    {
        switch (noun)
        {
            case ClockTime:
                registers = FormatClock(t.MissionElapsedTime);
                return true;
            case LatLongAltitude:
                registers =
                [
                    Format(t.LatitudeDegrees * HundredthScale),
                    Format(t.LongitudeDegrees * HundredthScale),
                    Format(t.AltitudeMeters / MetersPerNauticalMile * TenthScale),
                ];
                return true;
            case LandingVelocity:
                registers =
                [
                    Format(t.SpeedMetersPerSecond / MetersPerFoot * TenthScale),
                    Format(t.VerticalSpeedMetersPerSecond / MetersPerFoot * TenthScale),
                    Format(t.AltitudeMeters / MetersPerFoot),
                ];
                return true;
            case LandingRange:
                registers =
                [
                    Format(t.RangeToTargetMeters / MetersPerNauticalMile * TenthScale),
                    FormatMinSec(t.TimeToNextPhase),
                    // LR 고도와 계산 고도의 차이는 모델링하지 않으므로 0으로 표시한다.
                    Format(0),
                ];
                return true;
            default:
                registers = [];
                return false;
        }
    }

    /// <summary>
    /// 십진 값 하나를 레지스터 문자열(부호 1 + 숫자 5)로 만든다. 반올림 후 99999로 제한한다.
    /// </summary>
    public static string Format(double value)
    {
        var rounded = (long)Math.Round(value, MidpointRounding.AwayFromZero);
        var magnitude = (int)Math.Min(Math.Abs(rounded), MaxRegisterValue);
        var sign = rounded < 0 ? '-' : '+';
        return sign + magnitude.ToString($"D{RegisterDigits}");
    }

    /// <summary>8진 표시 레지스터(부호 칸 공백 + 숫자 5). 알람 코드처럼 이미 8진 자릿수로 표기된 값을 받는다.</summary>
    public static string FormatOctal(int digits)
    {
        return " " + Math.Min(Math.Max(digits, 0), MaxRegisterValue).ToString($"D{RegisterDigits}");
    }

    /// <summary>MIN/SEC 형식(XXbXX): 분 2자리, 공백, 초 2자리. 음수 시간은 0으로 본다.</summary>
    public static string FormatMinSec(TimeSpan time)
    {
        var totalSeconds = (int)Math.Round(Math.Max(time.TotalSeconds, 0));
        var minutes = Math.Min(totalSeconds / SecondsPerMinute, MaxMinSecMinutes);
        var seconds = totalSeconds % SecondsPerMinute;
        return $"+{minutes:D2} {seconds:D2}";
    }

    /// <summary>Noun 36: R1=시, R2=분, R3=초×100.</summary>
    private static string[] FormatClock(TimeSpan met)
    {
        var hours = (int)met.TotalHours;
        var centiseconds = met.Seconds * CentisecondsPerSecond
            + met.Milliseconds / MillisecondsPerCentisecond;
        return [Format(hours), Format(met.Minutes), Format(centiseconds)];
    }
}

namespace Agc.Core;

/// <summary>
/// DSKY(Display and Keyboard) 동작 로직: 키 입력 → Verb/Noun 입력 상태기계 → 실행 → 화면 상태 생성.
/// - 주요 특징: UI와 무관한 순수 로직. 화면은 <see cref="Display"/> 스냅샷으로만 노출한다.
/// - Owner: UI 스레드(MainWindow)가 생성하고 소유한다.
/// - Lifetime: 앱 실행 동안 하나만 존재한다.
/// - 스레드: 단일 스레드(UI)에서만 호출한다. 내부 동기화는 없다.
/// 지원 Verb: 05(알람 코드 표시, Noun 09), 06(1회 표시), 16(연속 모니터), 35(램프 테스트).
/// 지원 Noun: 09(알람, 8진), 36(MET), 43(위도/경도/고도), 63(속도/고도변화율/계산 고도), 68(착륙지점 사거리/제동 잔여시간).
/// Noun 정의는 DskyNouns를 참고한다.
/// </summary>
public sealed class DskyController
{
    private const int VerbAlarmDisplay = 5;
    private const int VerbDisplayOnce = 6;
    private const int VerbMonitor = 16;
    private const int VerbLampTest = 35;
    private const int FieldDigits = 2;
    private const int MaxStoredAlarms = 3;
    private const int RegisterCount = 3;
    private const string BlankField = "  ";
    private const string LampTestField = "88";
    private const string LampTestRegister = "+88888";

    /// <summary>V35 램프 테스트 유지 시간(실제 AGC도 약 5초).</summary>
    public static readonly TimeSpan LampTestDuration = TimeSpan.FromSeconds(5);

    private enum EntryField
    {
        None,
        Verb,
        Noun,
    }

    private readonly ITelemetrySource _telemetry;
    private readonly ILog _log;
    private readonly List<int> _alarmCodes = [];
    private string[] _registers = NewBlankRegisters();
    private EntryField _entry = EntryField.None;
    private string _verbText = string.Empty;
    private string _nounText = string.Empty;
    private int? _activeNoun;
    private bool _isMonitoring;
    private bool _oprErr;
    private bool _progAlarm;
    private TimeSpan _lampTestRemaining = TimeSpan.Zero;

    public DskyController(ITelemetrySource telemetry, ILog log)
    {
        _telemetry = telemetry;
        _log = log;
    }

    /// <summary>현재 화면 스냅샷. PROG 창은 항상 현재 텔레메트리의 프로그램 번호를 따른다.</summary>
    public DskyDisplay Display
    {
        get
        {
            if (IsLampTestActive)
            {
                return new DskyDisplay(
                    LampTestField, LampTestField, LampTestField,
                    LampTestRegister, LampTestRegister, LampTestRegister,
                    DskyLamps.All);
            }

            return new DskyDisplay(
                _telemetry.Current.ProgramNumber.ToString("D2"),
                _verbText.PadRight(FieldDigits),
                _nounText.PadRight(FieldDigits),
                _registers[0], _registers[1], _registers[2],
                BuildLamps());
        }
    }

    private bool IsLampTestActive => _lampTestRemaining > TimeSpan.Zero;

    /// <summary>
    /// 키 한 개를 누른다. 입력 상태에 맞지 않는 키는 OPR ERR 램프를 켠다.
    /// </summary>
    public void PressKey(DskyKey key)
    {
        switch (key)
        {
            case DskyKey.Verb:
                BeginEntry(EntryField.Verb);
                break;
            case DskyKey.Noun:
                BeginEntry(EntryField.Noun);
                break;
            case DskyKey.Enter:
                ExecuteEntered();
                break;
            case DskyKey.Clear:
                ClearEntry();
                break;
            case DskyKey.Reset:
                _oprErr = false;
                _progAlarm = false;
                break;
            case DskyKey.KeyRelease:
                ReleaseDisplay();
                break;
            case DskyKey.Proceed:
                // 이 단계에서는 PRO가 응답할 대기 상태(깜박임 요청)가 없으므로 의도적으로 아무 일도 하지 않는다.
                break;
            case DskyKey.Plus:
            case DskyKey.Minus:
                // 데이터 입력(Load verb)을 아직 지원하지 않으므로 부호 키는 잘못된 입력이다.
                RaiseOperatorError("sign key without data entry");
                break;
            default:
                EnterDigit((int)key);
                break;
        }
    }

    /// <summary>
    /// 실제 경과 시간을 반영한다: 램프 테스트 카운트다운, 모니터(V16) 레지스터 갱신.
    /// </summary>
    public void Update(TimeSpan elapsed)
    {
        if (IsLampTestActive)
        {
            _lampTestRemaining -= elapsed;
        }

        if (_isMonitoring)
        {
            RefreshRegisters();
        }
    }

    /// <summary>
    /// 프로그램 알람을 발생시킨다(예: 1201/1202). PROG 램프를 켜고 최근 3개 코드를 보관한다.
    /// V05 N09로 코드를 볼 수 있고 RSET으로 램프를 끈다.
    /// </summary>
    public void RaiseAlarm(int code)
    {
        _alarmCodes.Insert(0, code);
        if (_alarmCodes.Count > MaxStoredAlarms)
        {
            _alarmCodes.RemoveAt(MaxStoredAlarms);
        }

        _progAlarm = true;
        _log.Warn($"Program alarm {code} at MET {_telemetry.Current.MissionElapsedTime:hh\\:mm\\:ss}");
        if (_activeNoun == DskyNouns.AlarmCodes)
        {
            RefreshRegisters();
        }
    }

    private static string[] NewBlankRegisters()
    {
        return [DskyNouns.BlankRegister, DskyNouns.BlankRegister, DskyNouns.BlankRegister];
    }

    private DskyLamps BuildLamps()
    {
        var lamps = DskyLamps.None;
        if (_oprErr)
        {
            lamps |= DskyLamps.OprErr;
        }

        if (_progAlarm)
        {
            lamps |= DskyLamps.Prog;
        }

        if (_isMonitoring)
        {
            // 모니터 중에는 AGC가 계속 계산 중이라는 의미로 COMP ACTY를 켠다.
            lamps |= DskyLamps.CompActy;
        }

        return lamps;
    }

    private void BeginEntry(EntryField field)
    {
        // 실제 DSKY처럼 VERB/NOUN 키를 누르면 해당 창을 비우고 새 입력을 받는다.
        _entry = field;
        if (field == EntryField.Verb)
        {
            _verbText = string.Empty;
        }
        else
        {
            _nounText = string.Empty;
        }
    }

    private void EnterDigit(int digit)
    {
        if (_entry == EntryField.None)
        {
            RaiseOperatorError("digit without VERB/NOUN");
            return;
        }

        var text = (_entry == EntryField.Verb ? _verbText : _nounText) + digit;
        if (_entry == EntryField.Verb)
        {
            _verbText = text;
        }
        else
        {
            _nounText = text;
        }

        if (text.Length >= FieldDigits)
        {
            // 두 자리가 채워지면 해당 필드 입력이 끝난다. 이후 숫자는 새 VERB/NOUN 없이는 오류.
            _entry = EntryField.None;
        }
        else
        {
            // 한 자리만 입력된 상태: 계속 입력을 받는다.
        }
    }

    private void ClearEntry()
    {
        if (_entry == EntryField.Verb)
        {
            _verbText = string.Empty;
        }
        else if (_entry == EntryField.Noun)
        {
            _nounText = string.Empty;
        }
        else
        {
            // 입력 중이 아니면 지울 데이터가 없다. 데이터 입력 기능이 생기기 전까지는 무시한다.
        }
    }

    private void ReleaseDisplay()
    {
        _isMonitoring = false;
        _activeNoun = null;
        _registers = NewBlankRegisters();
        _verbText = string.Empty;
        _nounText = string.Empty;
        _entry = EntryField.None;
    }

    private void ExecuteEntered()
    {
        if (!TryParseField(_verbText, out var verb))
        {
            RaiseOperatorError("ENTR without complete verb");
            return;
        }

        _entry = EntryField.None;
        if (verb == VerbLampTest)
        {
            _lampTestRemaining = LampTestDuration;
            _log.Info("V35 lamp test");
            return;
        }

        if (verb is not (VerbAlarmDisplay or VerbDisplayOnce or VerbMonitor))
        {
            RaiseOperatorError($"unsupported verb {verb:D2}");
            return;
        }

        if (!TryParseField(_nounText, out var noun) || !IsNounAllowed(verb, noun))
        {
            RaiseOperatorError($"invalid noun for verb {verb:D2}: '{_nounText}'");
            return;
        }

        _activeNoun = noun;
        _isMonitoring = verb == VerbMonitor;
        RefreshRegisters();
        _log.Info($"V{verb:D2} N{noun:D2} executed");
    }

    /// <summary>V05는 알람 Noun(09)만, V06/V16은 텔레메트리 Noun(36/43/63/68)만 허용한다.</summary>
    private static bool IsNounAllowed(int verb, int noun)
    {
        if (verb == VerbAlarmDisplay)
        {
            return noun == DskyNouns.AlarmCodes;
        }

        return DskyNouns.IsTelemetryNoun(noun);
    }

    private void RefreshRegisters()
    {
        if (_activeNoun == DskyNouns.AlarmCodes)
        {
            _registers = NewBlankRegisters();
            for (var i = 0; i < RegisterCount && i < _alarmCodes.Count; i++)
            {
                _registers[i] = DskyNouns.FormatOctal(_alarmCodes[i]);
            }
        }
        else if (_activeNoun is int noun && DskyNouns.TryFormat(noun, _telemetry.Current, out var formatted))
        {
            _registers = formatted;
        }
        else
        {
            // 표시할 Noun이 없으면 레지스터는 그대로(빈 상태) 둔다.
        }
    }

    private void RaiseOperatorError(string reason)
    {
        _oprErr = true;
        _log.Warn($"OPR ERR: {reason}");
    }

    /// <summary>정확히 두 자리 숫자인 필드를 정수로 읽는다.</summary>
    private static bool TryParseField(string text, out int value)
    {
        value = 0;
        return text.Length == FieldDigits && int.TryParse(text, out value);
    }
}

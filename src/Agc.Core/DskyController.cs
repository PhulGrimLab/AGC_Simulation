namespace Agc.Core;

/// <summary>
/// DSKY(Display and Keyboard) 동작 로직: 키 입력 → Verb/Noun 입력 상태기계 → 실행 → 화면 상태 생성.
/// - 주요 특징: UI와 무관한 순수 로직. 화면은 <see cref="Display"/> 스냅샷으로만 노출한다.
/// - Owner: UI 스레드(MainWindow)가 생성하고 소유한다.
/// - Lifetime: 앱 실행 동안 하나만 존재한다.
/// - 스레드: 단일 스레드(UI)에서만 호출한다. 내부 동기화는 없다.
///
/// 지원 Verb
///   05 알람 코드 표시(N09)            06 Noun 1회 표시       16 Noun 연속 모니터(1초 갱신)
///   21~25 데이터 로드(R1/R2/R3/R1R2/R1R2R3; 현재 N36 시계만)
///   33 진행(PRO와 같은 응답)          34 종료(진행 중인 입력/표시 취소)
///   35 램프 테스트(약 5초, V/N 깜박임, 종료 후 88 표시가 남음)   37 프로그램 변경(요청은 <see cref="IProgramControl"/>로 전달)
/// 지원 Noun: 09(알람, 8진), 36(시계), 43(위도/경도/고도), 62(속도 — 사령선/착륙선 정의가 다름),
/// 63·68(착륙선 하강 전용: 속도·고도변화율·계산 고도 / 착륙지점 사거리·제동 잔여시간).
/// Noun 정의는 DskyNouns를 참고한다.
///
/// 깜박임(VerbNounFlashing): 램프 테스트, 데이터 로드 대기, V37 프로그램 번호 대기, 깜박임 요청 표시 중.
/// KEY REL 램프: 프로그램이 표시를 요청(<see cref="RequestDisplay"/>)했지만 승무원이 DSKY를 사용 중일 때 켜지며,
/// KEY REL 키로 사용 중인 표시를 놓으면 대기 중이던 요청이 나타난다.
/// </summary>
public sealed class DskyController
{
    private const int VerbAlarmDisplay = 5;
    private const int VerbDisplayOnce = 6;
    private const int VerbMonitor = 16;
    private const int VerbProceed = 33;
    private const int VerbTerminate = 34;
    private const int VerbLampTest = 35;
    private const int VerbProgramChange = 37;
    private const int FieldDigits = 2;
    private const int MaxStoredAlarms = 3;
    private const int RegisterCount = 3;
    private const int MaxClockMinutes = 59;
    private const int MaxClockCentiseconds = 5999;
    private const string LampTestField = "88";
    private const string LampTestRegister = "+88888";

    /// <summary>V35 램프 테스트 유지 시간(실제 AGC도 약 5초).</summary>
    public static readonly TimeSpan LampTestDuration = TimeSpan.FromSeconds(5);

    /// <summary>V16 모니터가 레지스터를 갱신하는 주기(실제 AGC는 1초마다 갱신).</summary>
    public static readonly TimeSpan MonitorRefreshInterval = TimeSpan.FromSeconds(1);

    private enum EntryField
    {
        None,
        Verb,
        Noun,
    }

    /// <summary>키 입력을 해석하는 방식: 일반, 데이터 로드 중, V37 프로그램 번호 대기 중.</summary>
    private enum Mode
    {
        Idle,
        LoadingData,
        AwaitingProgram,
    }

    private readonly ITelemetrySource _telemetry;
    private readonly ILog _log;
    private readonly IProgramControl? _programs;
    private readonly List<int> _alarmCodes = [];
    private string[] _registers = NewBlankRegisters();
    private EntryField _entry = EntryField.None;
    private Mode _mode = Mode.Idle;
    private RegisterLoadSession? _load;
    private string _verbText = string.Empty;
    private string _nounText = string.Empty;
    private int? _activeNoun;
    // 로드/프로그램 번호 대기 중에 VERB 키로 새 Verb를 입력하는 중인지(V33/V34 응답 등). 이때 ENTR은 그 Verb를 실행한다.
    private bool _verbOverride;
    private bool _isMonitoring;
    private bool _oprErr;
    private bool _progAlarm;
    private bool _keyRelLamp;
    private bool _requestFlash;
    // 지금 떠 있는 표시가 프로그램의 표시 요청으로 나타난 것인지(true) 승무원이 직접 명령한 것인지(false).
    // 프로그램 표시는 다음 프로그램 표시로 바로 교체되지만, 승무원이 쓰는 표시는 KEY REL을 거쳐야 교체된다.
    private bool _programOwnsDisplay;
    private (int Verb, int Noun, bool Flash)? _pendingRequest;
    private TimeSpan _clockOffset = TimeSpan.Zero;
    private TimeSpan _sinceMonitorRefresh = TimeSpan.Zero;
    private TimeSpan _lampTestRemaining = TimeSpan.Zero;

    /// <param name="programs">V37 요청을 받아 줄 쪽. null이면 V37은 지원되지 않는다(OPR ERR).</param>
    public DskyController(ITelemetrySource telemetry, ILog log, IProgramControl? programs = null)
    {
        _telemetry = telemetry;
        _log = log;
        _programs = programs;
    }

    /// <summary>현재 화면 스냅샷. PROG 창은 항상 현재 텔레메트리의 프로그램 번호를 따른다.</summary>
    public DskyDisplay Display
    {
        get
        {
            var vehicle = _telemetry.Current.Vehicle;
            if (IsLampTestActive)
            {
                return new DskyDisplay(
                    LampTestField, LampTestField, LampTestField,
                    LampTestRegister, LampTestRegister, LampTestRegister,
                    InstalledLamps(vehicle), VerbNounFlashing: true, Vehicle: vehicle);
            }

            return new DskyDisplay(
                _telemetry.Current.ProgramNumber.ToString("D2"),
                _verbText.PadRight(FieldDigits),
                _nounText.PadRight(FieldDigits),
                _registers[0], _registers[1], _registers[2],
                BuildLamps(),
                VerbNounFlashing: _mode != Mode.Idle || _requestFlash,
                Vehicle: vehicle);
        }
    }

    private bool IsLampTestActive => _lampTestRemaining > TimeSpan.Zero;

    /// <summary>승무원이 DSKY를 사용 중인지(키 입력 중, 로드/프로그램 대기 중, 표시가 떠 있음).</summary>
    private bool IsCrewUsingDisplay =>
        _entry != EntryField.None || _mode != Mode.Idle || IsLampTestActive
        || (_activeNoun is not null && !_programOwnsDisplay);

    /// <summary>
    /// 키 한 개를 누른다. 입력 상태에 맞지 않는 키는 OPR ERR 램프를 켠다.
    /// </summary>
    public void PressKey(DskyKey key)
    {
        if (IsLampTestActive)
        {
            // 램프 테스트 중에는 모든 창이 테스트 패턴으로 덮여 있어 입력 결과를 볼 수 없다.
            // 보이지 않는 상태 변경을 막기 위해 테스트가 끝날 때까지 키를 무시한다.
            return;
        }

        switch (key)
        {
            case DskyKey.Verb:
                // 로드/프로그램 번호 대기 중에는 V33(진행)/V34(종료)로 응답해야 하므로 대기를 취소하지 않고
                // 새 Verb 입력만 받는다. 다른 Verb가 실행될 때 비로소 대기가 취소된다.
                _verbOverride = _mode != Mode.Idle;
                BeginEntry(EntryField.Verb);
                break;
            case DskyKey.Noun:
                AbortMode();
                BeginEntry(EntryField.Noun);
                break;
            case DskyKey.Enter:
                HandleEnter();
                break;
            case DskyKey.Clear:
                HandleClear();
                break;
            case DskyKey.Reset:
                _oprErr = false;
                _progAlarm = false;
                break;
            case DskyKey.KeyRelease:
                ReleaseDisplay();
                break;
            case DskyKey.Proceed:
                Proceed();
                break;
            case DskyKey.Plus:
            case DskyKey.Minus:
                HandleSign(key == DskyKey.Plus ? '+' : '-');
                break;
            default:
                HandleDigit((int)key);
                break;
        }
    }

    /// <summary>
    /// 실제 경과 시간을 반영한다: 램프 테스트 카운트다운, 모니터(V16) 레지스터 갱신(1초 주기).
    /// </summary>
    public void Update(TimeSpan elapsed)
    {
        if (IsLampTestActive)
        {
            _lampTestRemaining -= elapsed;
            if (!IsLampTestActive)
            {
                LeaveLampTestResidue();
            }
        }

        if (_isMonitoring)
        {
            _sinceMonitorRefresh += elapsed;
            if (_sinceMonitorRefresh >= MonitorRefreshInterval)
            {
                _sinceMonitorRefresh = TimeSpan.Zero;
                RefreshRegisters();
            }
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

    /// <summary>
    /// 프로그램(AGC 쪽)이 표시를 요청한다. 승무원이 DSKY를 쓰고 있지 않으면 바로 표시하고,
    /// 쓰고 있으면 KEY REL 램프를 켜서 알린 뒤 승무원이 KEY REL을 누르면 표시한다.
    /// </summary>
    /// <param name="verb">05, 06, 16 중 하나.</param>
    /// <param name="noun">해당 Verb가 허용하는 Noun.</param>
    /// <param name="flash">true이면 V/N을 깜박여 응답(PRO)을 요청한다.</param>
    public void RequestDisplay(int verb, int noun, bool flash)
    {
        if (verb is not (VerbAlarmDisplay or VerbDisplayOnce or VerbMonitor) || !IsNounAllowed(verb, noun))
        {
            _log.Warn($"Ignored display request V{verb:D2} N{noun:D2}");
            return;
        }

        if (IsCrewUsingDisplay)
        {
            _pendingRequest = (verb, noun, flash);
            _keyRelLamp = true;
            _log.Info($"Display request V{verb:D2} N{noun:D2} waiting for KEY REL");
        }
        else
        {
            PresentRequest(verb, noun, flash);
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

        if (_keyRelLamp)
        {
            lamps |= DskyLamps.KeyRel;
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
        // 승무원이 VERB/NOUN을 누르면 표시의 주인이 승무원이 된다.
        _programOwnsDisplay = false;
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

    // ---- 키 처리 ----

    private void HandleDigit(int digit)
    {
        if (_entry != EntryField.None)
        {
            // VERB/NOUN 창에 입력 중이면(로드 중 V33 응답 입력, V37 프로그램 번호 포함) 숫자는 그 창으로 간다.
            EnterDigit(digit);
            return;
        }

        if (_mode == Mode.LoadingData && _load is not null)
        {
            if (_load.PressDigit(digit))
            {
                ShowLoadRegisters();
            }
            else
            {
                RaiseOperatorError("load digit without sign or beyond 5 digits");
            }

            return;
        }

        EnterDigit(digit);
    }

    private void HandleSign(char sign)
    {
        if (_entry == EntryField.None && _mode == Mode.LoadingData && _load is not null)
        {
            if (_load.PressSign(sign))
            {
                ShowLoadRegisters();
            }
            else
            {
                RaiseOperatorError("unexpected sign during load");
            }

            return;
        }

        // 데이터를 받는 중이 아니므로 부호 키는 잘못된 입력이다.
        RaiseOperatorError("sign key without data entry");
    }

    private void HandleClear()
    {
        if (_entry == EntryField.None && _mode == Mode.LoadingData && _load is not null)
        {
            _load.ClearCurrent();
            ShowLoadRegisters();
            return;
        }

        ClearEntry();
    }

    private void HandleEnter()
    {
        if (_verbOverride)
        {
            // 로드/프로그램 번호 대기 중에 VERB 키로 입력한 Verb(V33, V34 등)를 실행한다.
            ExecuteEntered();
        }
        else if (_mode == Mode.LoadingData)
        {
            AcceptLoadedRegister();
        }
        else if (_mode == Mode.AwaitingProgram)
        {
            CompleteProgramChange();
        }
        else
        {
            ExecuteEntered();
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

    /// <summary>
    /// CLR: 입력 중인 VERB/NOUN 창(두 자리가 다 차기 전)을 지운다. 데이터 로드 중에는 현재 레지스터 입력을 지운다.
    /// 두 자리가 완성된 Verb/Noun은 CLR로 고치지 않고 VERB/NOUN 키를 다시 눌러 새로 입력한다
    /// (실제 DSKY의 CLR도 데이터 입력에만 작용한다).
    /// </summary>
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
            // 입력 중이 아니면 지울 데이터가 없다.
        }
    }

    /// <summary>PRO: 깜박이는 요청 표시에 응답한다. 응답할 대상이 없으면 아무 일도 하지 않는다.</summary>
    private void Proceed()
    {
        if (_requestFlash)
        {
            _requestFlash = false;
            _log.Info("PRO: flashing display acknowledged");
        }
        else
        {
            // 데이터 로드 대기 중의 PRO는 무시된다(실제로도 V33E를 써야 한다). 그 밖에는 응답할 요청이 없다.
        }
    }

    /// <summary>KEY REL: 사용 중인 표시를 놓는다. 대기 중이던 프로그램 요청이 있으면 이어서 표시한다.</summary>
    private void ReleaseDisplay()
    {
        AbortMode();
        _isMonitoring = false;
        _activeNoun = null;
        _requestFlash = false;
        _registers = NewBlankRegisters();
        _verbText = string.Empty;
        _nounText = string.Empty;
        _entry = EntryField.None;
        _keyRelLamp = false;
        if (_pendingRequest is { } request)
        {
            _pendingRequest = null;
            PresentRequest(request.Verb, request.Noun, request.Flash);
        }
    }

    /// <summary>진행 중인 데이터 로드/프로그램 번호 대기를 취소한다.</summary>
    private void AbortMode()
    {
        var wasLoading = _load is not null;
        _mode = Mode.Idle;
        _verbOverride = false;
        _load = null;
        if (wasLoading)
        {
            // 로드 중에는 레지스터를 비워 두었으므로 원래 Noun 값을 다시 보여준다.
            RefreshRegisters();
        }
    }

    // ---- Verb 실행 ----

    private void ExecuteEntered()
    {
        if (!TryParseField(_verbText, out var verb))
        {
            RaiseOperatorError("ENTR without complete verb");
            return;
        }

        _entry = EntryField.None;
        _verbOverride = false;
        if (verb is not (VerbProceed or VerbTerminate))
        {
            // 새 명령(V33/V34 이외)은 대기 중이던 로드/프로그램 번호 입력을 대체한다.
            AbortMode();
        }
        else
        {
            // V33/V34는 대기 중인 입력에 대한 응답이므로 각 처리에서 다룬다.
        }

        switch (verb)
        {
            case VerbLampTest:
                _lampTestRemaining = LampTestDuration;
                _log.Info("V35 lamp test");
                return;
            case VerbProgramChange:
                BeginProgramChange();
                return;
            case VerbProceed:
                ProceedWithoutData();
                return;
            case VerbTerminate:
                Terminate();
                return;
            default:
                ExecuteNounVerb(verb);
                return;
        }
    }

    /// <summary>Noun이 필요한 Verb(표시 05/06/16, 로드 21~25)를 실행한다.</summary>
    private void ExecuteNounVerb(int verb)
    {
        if (!TryParseField(_nounText, out var noun))
        {
            RaiseOperatorError($"V{verb:D2} needs a complete noun: '{_nounText}'");
            return;
        }

        if (RegisterLoadSession.TryCreate(verb, noun, out var session))
        {
            StartLoad(verb, noun, session!);
            return;
        }

        if (verb is not (VerbAlarmDisplay or VerbDisplayOnce or VerbMonitor))
        {
            RaiseOperatorError($"unsupported verb {verb:D2}");
            return;
        }

        if (!IsNounAllowed(verb, noun))
        {
            RaiseOperatorError($"invalid noun for verb {verb:D2}: '{_nounText}'");
            return;
        }

        _programOwnsDisplay = false;
        _activeNoun = noun;
        _isMonitoring = verb == VerbMonitor;
        _sinceMonitorRefresh = TimeSpan.Zero;
        RefreshRegisters();
        _log.Info($"V{verb:D2} N{noun:D2} executed");
    }

    /// <summary>
    /// V05는 알람 Noun(09)만, V06/V16은 현재 우주선의 텔레메트리 Noun만 허용한다
    /// (사령선 36/43/62, 달착륙선 36/43/62/63/68).
    /// </summary>
    private bool IsNounAllowed(int verb, int noun)
    {
        if (verb == VerbAlarmDisplay)
        {
            return noun == DskyNouns.AlarmCodes;
        }

        return DskyNouns.IsTelemetryNoun(noun, _telemetry.Current.Vehicle);
    }

    /// <summary>이 우주선의 DSKY에 실제로 있는 램프. ALT/VEL은 달착륙선에만 있다.</summary>
    private static DskyLamps InstalledLamps(AgcVehicle vehicle)
    {
        return vehicle == AgcVehicle.Lm ? DskyLamps.All : DskyLamps.All & ~(DskyLamps.Alt | DskyLamps.Vel);
    }

    /// <summary>V35가 끝난 뒤: 깜박임은 멈추고 88 표시는 남는다(Virtual AGC 문서 기준).</summary>
    private void LeaveLampTestResidue()
    {
        AbortMode();
        _entry = EntryField.None;
        _isMonitoring = false;
        _activeNoun = null;
        _verbText = LampTestField;
        _nounText = LampTestField;
        _registers = [LampTestRegister, LampTestRegister, LampTestRegister];
    }

    private void PresentRequest(int verb, int noun, bool flash)
    {
        _verbText = verb.ToString("D2");
        _nounText = noun.ToString("D2");
        _activeNoun = noun;
        _isMonitoring = verb == VerbMonitor;
        _requestFlash = flash;
        _programOwnsDisplay = true;
        _sinceMonitorRefresh = TimeSpan.Zero;
        RefreshRegisters();
        _log.Info($"Program display V{verb:D2} N{noun:D2}{(flash ? " (flashing)" : string.Empty)}");
    }

    /// <summary>V33: 데이터 없이 진행. 로드 중이면 로드를 끝내고, 깜박임 요청이면 응답한다.</summary>
    private void ProceedWithoutData()
    {
        if (_mode == Mode.LoadingData)
        {
            AbortMode();
        }

        Proceed();
    }

    /// <summary>V34: 진행 중인 입력, 깜박이는 요청 표시, 모니터를 종료한다. 이미 표시된 값은 화면에 남는다.</summary>
    private void Terminate()
    {
        AbortMode();
        _requestFlash = false;
        _isMonitoring = false;
        // 표시를 더 이상 쓰는 중이 아니므로 이후 프로그램 표시 요청은 KEY REL 없이 바로 나타난다.
        _activeNoun = null;
        _log.Info("V34 terminate");
    }

    // ---- 데이터 로드 (V21~V25) ----

    private void StartLoad(int verb, int noun, RegisterLoadSession session)
    {
        if (!DskyNouns.IsLoadable(noun))
        {
            RaiseOperatorError($"noun {noun:D2} cannot be loaded");
            return;
        }

        if (noun == DskyNouns.ClockTime && verb != RegisterLoadSession.VerbLoadAll)
        {
            // 시/분/초 Noun은 3개 워드를 모두 로드해야 한다(원본: 그렇지 않으면 알람).
            RaiseOperatorError("clock noun requires loading all 3 registers (V25)");
            return;
        }

        _load = session;
        _mode = Mode.LoadingData;
        _programOwnsDisplay = false;
        _activeNoun = noun;
        _isMonitoring = false;
        ShowLoadRegisters();
        _log.Info($"V{verb:D2} N{noun:D2} load started");
    }

    private void ShowLoadRegisters()
    {
        if (_load is null)
        {
            return;
        }

        for (var i = 0; i < RegisterCount; i++)
        {
            if (_load.Covers(i))
            {
                _registers[i] = _load.TextFor(i);
            }
            else
            {
                // 로드 대상이 아닌 레지스터는 기존 표시를 유지한다.
            }
        }
    }

    private void AcceptLoadedRegister()
    {
        if (_load is null || !_load.Accept())
        {
            RaiseOperatorError("ENTR with incomplete load data (sign + 5 digits required)");
            return;
        }

        ShowLoadRegisters();
        if (_load.IsComplete)
        {
            ApplyLoad(_load);
        }
        else
        {
            // 다음 레지스터 입력을 계속 기다린다.
        }
    }

    /// <summary>모든 레지스터가 입력되면 Noun에 맞게 적용한다(현재는 N36 시계).</summary>
    private void ApplyLoad(RegisterLoadSession session)
    {
        var values = session.AcceptedValues;
        var hours = int.Parse(values[0]!);
        var minutes = int.Parse(values[1]!);
        var centiseconds = int.Parse(values[2]!);
        if (hours < 0 || minutes is < 0 or > MaxClockMinutes || centiseconds is < 0 or > MaxClockCentiseconds)
        {
            // 범위를 벗어난 시계 값: 오류를 알리고 처음부터 다시 입력받는다.
            RaiseOperatorError($"invalid clock value {hours}:{minutes}:{centiseconds}");
            session.Restart();
            ShowLoadRegisters();
            return;
        }

        var loaded = TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes)
            + TimeSpan.FromMilliseconds(centiseconds * DskyNouns.MillisecondsPerCentisecond);
        _clockOffset = loaded - _telemetry.Current.MissionElapsedTime;
        _mode = Mode.Idle;
        _load = null;
        RefreshRegisters();
        _log.Info($"AGC clock set to {loaded:hh\\:mm\\:ss\\.ff} (offset {_clockOffset:c})");
    }

    // ---- 프로그램 변경 (V37) ----

    private void BeginProgramChange()
    {
        if (_programs is null)
        {
            RaiseOperatorError("V37 not supported (no program control)");
            return;
        }

        // V37 뒤에는 NOUN 키 없이 2자리 프로그램 번호를 입력하고 ENTR 한다. 입력은 NOUN 창에 나타난다.
        _mode = Mode.AwaitingProgram;
        BeginEntry(EntryField.Noun);
    }

    private void CompleteProgramChange()
    {
        if (!TryParseField(_nounText, out var program))
        {
            RaiseOperatorError("V37 needs a 2-digit program number");
            return;
        }

        _mode = Mode.Idle;
        if (_programs is not null && _programs.TryRequestProgram(program))
        {
            _log.Info($"V37 P{program:D2} requested");
        }
        else
        {
            RaiseOperatorError($"program P{program:D2} rejected");
        }
    }

    // ---- 레지스터 갱신 ----

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
        else if (_activeNoun is int noun && DskyNouns.TryFormat(noun, AdjustedTelemetry(), out var formatted))
        {
            _registers = formatted;
        }
        else
        {
            // 표시할 Noun이 없으면 레지스터는 그대로(빈 상태) 둔다.
        }
    }

    /// <summary>AGC 시계 보정(V25 N36 로드)을 반영한 텔레메트리. 시계가 0보다 작아지지 않게 제한한다.</summary>
    private AgcTelemetry AdjustedTelemetry()
    {
        var t = _telemetry.Current;
        var clock = t.MissionElapsedTime + _clockOffset;
        return t with { MissionElapsedTime = clock < TimeSpan.Zero ? TimeSpan.Zero : clock };
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

namespace Agc.Core;

/// <summary>
/// 데이터 로드 Verb(V21~V25)의 레지스터 입력 한 번(세션)을 관리한다.
/// 규칙(Virtual AGC 문서 기준): 십진 데이터는 반드시 부호(+/-)로 시작하고 숫자 5자리를 모두 입력해야 하며,
/// ENTR로 한 레지스터씩 확정한다. CLR은 현재 레지스터의 입력을 지운다.
/// - V21=R1, V22=R2, V23=R3, V24=R1·R2, V25=R1·R2·R3 를 순서대로 입력한다.
/// - 상태를 가진 단순 객체이며 UI 스레드의 DskyController만 소유/사용한다.
/// </summary>
internal sealed class RegisterLoadSession
{
    private const int VerbLoadR1 = 21;
    private const int VerbLoadR2 = 22;
    private const int VerbLoadR3 = 23;
    private const int VerbLoadR1R2 = 24;
    private const int VerbLoadAll = 25;
    private const int ComponentCount = 3;
    private const int DataDigits = 5;
    private const int CompleteLength = DataDigits + 1;

    private readonly int _firstComponent;
    private readonly int _lastComponent;
    private readonly string?[] _accepted = new string?[ComponentCount];
    private string _buffer = string.Empty;
    private int _index;

    private RegisterLoadSession(int noun, int first, int last)
    {
        Noun = noun;
        _firstComponent = first;
        _lastComponent = last;
        _index = first;
    }

    public int Noun { get; }

    /// <summary>모든 대상 레지스터의 입력이 확정되었는지.</summary>
    public bool IsComplete => _index > _lastComponent;

    /// <summary>확정된 값(부호 포함 6글자). 아직 입력되지 않은 레지스터는 null.</summary>
    public IReadOnlyList<string?> AcceptedValues => _accepted;

    /// <summary>V21~V25이면 해당 범위의 세션을 만든다. 아니면 false.</summary>
    public static bool TryCreate(int verb, int noun, out RegisterLoadSession? session)
    {
        (int First, int Last)? range = verb switch
        {
            VerbLoadR1 => (0, 0),
            VerbLoadR2 => (1, 1),
            VerbLoadR3 => (2, 2),
            VerbLoadR1R2 => (0, 1),
            VerbLoadAll => (0, 2),
            _ => null,
        };
        session = range is { } r ? new RegisterLoadSession(noun, r.First, r.Last) : null;
        return session is not null;
    }

    /// <summary>레지스터 i가 이 세션의 입력 대상인지.</summary>
    public bool Covers(int component)
    {
        return component >= _firstComponent && component <= _lastComponent;
    }

    /// <summary>레지스터 i의 표시 문자열(6글자): 확정값 > 입력 중 값 > 공백.</summary>
    public string TextFor(int component)
    {
        if (_accepted[component] is { } accepted)
        {
            return accepted;
        }

        if (component == _index && !IsComplete)
        {
            return _buffer.PadRight(CompleteLength);
        }

        return DskyNouns.BlankRegister;
    }

    /// <summary>부호 키. 버퍼가 비어 있을 때만 허용된다.</summary>
    public bool PressSign(char sign)
    {
        if (IsComplete || _buffer.Length != 0)
        {
            return false;
        }

        _buffer = sign.ToString();
        return true;
    }

    /// <summary>숫자 키. 부호 뒤에서 5자리까지만 허용된다.</summary>
    public bool PressDigit(int digit)
    {
        if (IsComplete || _buffer.Length == 0 || _buffer.Length >= CompleteLength)
        {
            return false;
        }

        _buffer += digit;
        return true;
    }

    /// <summary>CLR: 현재 레지스터 입력을 지운다.</summary>
    public void ClearCurrent()
    {
        _buffer = string.Empty;
    }

    /// <summary>ENTR: 부호+5자리가 모두 입력되었으면 확정하고 다음 레지스터로 넘어간다.</summary>
    public bool Accept()
    {
        if (IsComplete || _buffer.Length != CompleteLength)
        {
            return false;
        }

        _accepted[_index] = _buffer;
        _buffer = string.Empty;
        _index++;
        return true;
    }

    /// <summary>입력을 처음부터 다시 시작한다(검증 실패 시).</summary>
    public void Restart()
    {
        Array.Clear(_accepted);
        _buffer = string.Empty;
        _index = _firstComponent;
    }
}

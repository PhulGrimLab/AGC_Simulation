namespace Agc.Core;

/// <summary>
/// DSKY 키 입력을 문자열로 적는 표기법과 그 변환. 단계 가이드가 "V37E63E" 같은 키 순서를 담고,
/// 화면이 이를 읽기 좋게 보여 주거나(<see cref="Describe"/>) DSKY에 그대로 누르게(<see cref="Parse"/>) 한다.
/// 표기: 숫자 0-9, V=VERB, N=NOUN, E=ENTR, C=CLR, R=RSET, K=KEY REL, P=PRO, +, -.
/// 상태 없는 정적 유틸리티이다.
/// </summary>
public static class DskyKeyNotation
{
    private const string Alphabet = "0123456789VNECRKP+-";

    /// <summary>문자열이 표기법만으로 이루어져 있는지(빈 문자열은 false).</summary>
    public static bool IsValid(string keys)
    {
        return !string.IsNullOrEmpty(keys) && keys.All(c => Alphabet.Contains(c));
    }

    /// <summary>키 문자열을 DSKY 키 순서로 바꾼다. 표기법에 없는 문자가 있으면 예외.</summary>
    public static IReadOnlyList<DskyKey> Parse(string keys)
    {
        return keys.Where(c => c != ' ').Select(ToKey).ToList();
    }

    /// <summary>
    /// 사람이 읽기 좋은 형태로 바꾼다. 예: "V16N63E" → "VERB 1 6 NOUN 6 3 ENTR". 키패드의 글자와 같다.
    /// </summary>
    public static string Describe(string keys)
    {
        return string.Join(' ', Parse(keys).Select(Label));
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
            _ => throw new ArgumentException($"알 수 없는 DSKY 키 표기: '{c}'", nameof(c)),
        };
    }

    private static string Label(DskyKey key)
    {
        return key switch
        {
            DskyKey.Verb => "VERB",
            DskyKey.Noun => "NOUN",
            DskyKey.Enter => "ENTR",
            DskyKey.Clear => "CLR",
            DskyKey.Reset => "RSET",
            DskyKey.KeyRelease => "KEY REL",
            DskyKey.Proceed => "PRO",
            DskyKey.Plus => "+",
            DskyKey.Minus => "−",
            _ => ((int)key).ToString(),
        };
    }
}

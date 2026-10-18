using System.Windows.Input;
using Agc.Core;

namespace Agc.Wpf;

/// <summary>
/// PC 키보드 키를 DSKY 키로 대응시키는 표. 상태 없는 변환 유틸리티.
/// 숫자(상단/넘패드), +/-, V=VERB, N=NOUN, Enter=ENTR, C/Backspace=CLR, R=RSET, K=KEY REL, P=PRO.
/// Ctrl/Alt가 함께 눌린 키는 단축키(복사 등)와 충돌하므로 DSKY 키로 보지 않는다.
/// </summary>
internal static class DskyKeyboardMap
{
    /// <summary>키와 보조키 상태를 DSKY 키로 변환한다. 대응하는 DSKY 키가 없으면 null.</summary>
    public static DskyKey? TryMap(Key key, ModifierKeys modifiers)
    {
        if (modifiers.HasFlag(ModifierKeys.Control) || modifiers.HasFlag(ModifierKeys.Alt))
        {
            return null;
        }

        var shift = modifiers.HasFlag(ModifierKeys.Shift);
        return key switch
        {
            >= Key.D0 and <= Key.D9 when !shift => DskyKey.Digit0 + (key - Key.D0),
            >= Key.NumPad0 and <= Key.NumPad9 => DskyKey.Digit0 + (key - Key.NumPad0),
            Key.Add => DskyKey.Plus,
            Key.OemPlus when shift => DskyKey.Plus,
            Key.Subtract or Key.OemMinus => DskyKey.Minus,
            Key.V => DskyKey.Verb,
            Key.N => DskyKey.Noun,
            Key.Enter => DskyKey.Enter,
            Key.C or Key.Back => DskyKey.Clear,
            Key.R => DskyKey.Reset,
            Key.K => DskyKey.KeyRelease,
            Key.P => DskyKey.Proceed,
            _ => null,
        };
    }
}

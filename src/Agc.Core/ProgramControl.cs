namespace Agc.Core;

/// <summary>
/// V37(프로그램 변경) 요청을 받아 주는 쪽. DSKY는 요청만 하고, 허용 여부는 구현체(미션 시뮬레이터)가 판단한다.
/// </summary>
public interface IProgramControl
{
    /// <summary>프로그램 번호 변경을 요청한다. 알 수 없는 프로그램이면 false.</summary>
    bool TryRequestProgram(int program);
}

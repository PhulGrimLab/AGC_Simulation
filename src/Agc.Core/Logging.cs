using System.Text;
using System.Threading.Channels;

namespace Agc.Core;

/// <summary>
/// 최소 로깅 인터페이스. 호출 스레드를 막지 않아야 한다.
/// </summary>
public interface ILog
{
    void Info(string message);
    void Warn(string message);
}

/// <summary>
/// 아무 것도 기록하지 않는 로거. 로거가 필요 없는 곳에서 사용한다.
/// </summary>
public sealed class NullLog : ILog
{
    public static readonly NullLog Instance = new();

    public void Info(string message) { }

    public void Warn(string message) { }
}

/// <summary>
/// 비동기 큐 + 전용 Writer 태스크 방식의 파일 로거.
/// - 주요 특징: 호출 스레드(UI 포함)는 큐에 넣기만 하고 파일 I/O를 기다리지 않는다.
/// - Owner: 애플리케이션(MainWindow)이 생성하고 종료 시 Dispose 한다.
/// - Lifetime: 앱 시작부터 종료까지. Dispose 시 큐를 닫고 Writer 종료를 최대 2초 기다린다.
/// - 공유자원/동기화: Channel(다중 Writer, 단일 Reader)이 스레드 안전성을 제공하며, 파일은 Writer 태스크만 접근한다.
/// - 주의: 파일 오류가 나면 로깅만 중단하고 앱은 중단시키지 않는다.
/// </summary>
public sealed class AsyncFileLogger : ILog, IDisposable
{
    private static readonly TimeSpan DisposeWaitTimeout = TimeSpan.FromSeconds(2);

    private readonly Channel<string> _queue =
        Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });
    private readonly Task _writerTask;
    private readonly string _path;

    /// <param name="directory">로그 폴더(없으면 생성).</param>
    /// <param name="filePrefix">파일명 접두사. 날짜별 파일 {prefix}-yyyyMMdd.log 로 기록한다.</param>
    public AsyncFileLogger(string directory, string filePrefix)
    {
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, $"{filePrefix}-{DateTime.Now:yyyyMMdd}.log");
        _writerTask = Task.Run(WriteLoopAsync);
    }

    /// <summary>기록 중인 로그 파일 경로.</summary>
    public string FilePath => _path;

    public void Info(string message)
    {
        Enqueue("INFO", message);
    }

    public void Warn(string message)
    {
        Enqueue("WARN", message);
    }

    /// <summary>큐를 닫고 남은 로그가 파일에 쓰일 때까지 잠시 기다린다.</summary>
    public void Dispose()
    {
        _queue.Writer.TryComplete();
        _writerTask.Wait(DisposeWaitTimeout);
    }

    private void Enqueue(string level, string message)
    {
        // 큐가 닫혔으면(Dispose 이후 또는 파일 오류) 조용히 버린다.
        _queue.Writer.TryWrite($"{DateTime.Now:HH:mm:ss.fff} [{level}] {message}");
    }

    private async Task WriteLoopAsync()
    {
        try
        {
            await using var writer = new StreamWriter(
                new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite),
                Encoding.UTF8);
            while (await _queue.Reader.WaitToReadAsync())
            {
                while (_queue.Reader.TryRead(out var line))
                {
                    await writer.WriteLineAsync(line);
                }

                await writer.FlushAsync();
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // 로깅 실패가 앱 동작에 영향을 주면 안 되므로 큐를 닫고 로깅만 중단한다.
            _queue.Writer.TryComplete();
        }
    }
}

namespace Agc.Tests.Core;

/// <summary>기능: 비동기 파일 로거(AsyncFileLogger).</summary>
[TestClass]
public sealed class AsyncFileLoggerTests
{
    private DirectoryInfo _dir = null!;

    [TestInitialize]
    public void Setup()
    {
        _dir = Directory.CreateTempSubdirectory("agc-log-test");
    }

    [TestCleanup]
    public void Cleanup()
    {
        _dir.Delete(recursive: true);
    }

    [TestMethod]
    public void Dispose_FlushesQueuedLinesWithLevelAndMessage()
    {
        string path;
        using (var logger = new AsyncFileLogger(_dir.FullName, "test"))
        {
            path = logger.FilePath;
            logger.Info("hello");
            logger.Warn("careful");
        }

        var text = File.ReadAllText(path);
        StringAssert.Contains(text, "[INFO] hello");
        StringAssert.Contains(text, "[WARN] careful");
    }

    [TestMethod]
    public void Logging_AfterDispose_DoesNotThrow()
    {
        var logger = new AsyncFileLogger(_dir.FullName, "test");
        logger.Dispose();

        logger.Info("late message");
    }

    [TestMethod]
    public void Logging_FromManyThreads_KeepsEveryLine()
    {
        string path;
        using (var logger = new AsyncFileLogger(_dir.FullName, "test"))
        {
            path = logger.FilePath;
            Parallel.For(0, 200, i => logger.Info($"line-{i}"));
        }

        Assert.HasCount(200, File.ReadAllLines(path));
    }

    [TestMethod]
    public void NullLog_AcceptsMessagesSilently()
    {
        NullLog.Instance.Info("x");
        NullLog.Instance.Warn("y");
    }
}

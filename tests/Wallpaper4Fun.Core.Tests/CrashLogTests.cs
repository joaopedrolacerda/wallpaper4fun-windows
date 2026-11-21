namespace Wallpaper4Fun.Core.Tests;

public sealed class CrashLogTests : IDisposable
{
    private readonly TemporaryFolder _folder = new();
    private static readonly DateTimeOffset When = new(2026, 11, 21, 10, 30, 0, TimeSpan.Zero);

    public void Dispose() => _folder.Dispose();

    private static Exception Thrown(Exception e)
    {
        try { throw e; }
        catch (Exception caught) { return caught; }
    }

    [Fact(DisplayName = "o relatório tem a data, o tipo e a mensagem do erro")]
    public void ReportHasTypeAndMessage()
    {
        var report = CrashLog.Format(new InvalidOperationException("ícone quebrado"), When);

        Assert.Contains("2026-11-21", report);
        Assert.Contains("InvalidOperationException", report);
        Assert.Contains("ícone quebrado", report);
    }

    [Fact(DisplayName = "o relatório inclui onde o erro aconteceu (stack trace)")]
    public void ReportHasStackTrace()
    {
        var report = CrashLog.Format(Thrown(new InvalidOperationException("x")), When);
        Assert.Contains(nameof(Thrown), report);
    }

    [Fact(DisplayName = "o relatório inclui o erro interno, que costuma ser a causa real")]
    public void ReportHasInnerException()
    {
        var error = new InvalidOperationException("por fora", new FileNotFoundException("arquivo sumiu"));
        Assert.Contains("arquivo sumiu", CrashLog.Format(error, When));
    }

    [Fact(DisplayName = "gravar cria a pasta e o arquivo de log")]
    public void WriteCreatesFile()
    {
        var log = new CrashLog(_folder.File(Path.Combine("logs", "crash.log")));

        log.Write(new InvalidOperationException("primeiro"), When);

        Assert.Contains("primeiro", File.ReadAllText(log.FilePath));
    }

    [Fact(DisplayName = "erros novos são acrescentados, sem apagar os anteriores")]
    public void WriteAppends()
    {
        var log = new CrashLog(_folder.File("crash.log"));

        log.Write(new InvalidOperationException("primeiro"), When);
        log.Write(new InvalidOperationException("segundo"), When);

        var text = File.ReadAllText(log.FilePath);
        Assert.Contains("primeiro", text);
        Assert.Contains("segundo", text);
    }

    [Fact(DisplayName = "se não der para gravar, não lança outro erro")]
    public void WriteNeverThrows()
    {
        var blocked = _folder.MakeFile("ocupado");
        var log = new CrashLog(Path.Combine(blocked, "crash.log"));

        var error = Record.Exception(() => log.Write(new InvalidOperationException("x"), When));

        Assert.Null(error);
    }
}

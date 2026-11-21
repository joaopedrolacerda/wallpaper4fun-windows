using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Wallpaper4Fun.Core;

namespace Wallpaper4Fun.App;

/// <summary>
/// Marca até onde a inicialização chegou, no console (versão de diagnóstico) e em
/// <c>%APPDATA%\wallpaper4fun\startup.log</c>. Se o app sumir sem mensagem, a última linha
/// mostra o passo em que ele parou.
/// </summary>
internal static class StartupTrace
{
    private static readonly CrashLog Log = new(AppPaths.StartupLogFile);

#pragma warning disable CA2255 // De propósito: precisa rodar antes de tudo, inclusive do Main gerado pelo WPF.
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Begin()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Step($"ERRO FATAL: {e.ExceptionObject}");
        Step("----- nova execução -----");
        Step($"wallpaper4fun {typeof(StartupTrace).Assembly.GetName().Version} | {RuntimeInformation.OSDescription} | " +
             $"{RuntimeInformation.FrameworkDescription} | {RuntimeInformation.ProcessArchitecture}");
        Step($"pasta do app: {AppContext.BaseDirectory}");
    }

    public static void Step(string message)
    {
        Console.WriteLine(message);
        Log.Note(message, DateTimeOffset.Now);
    }
}

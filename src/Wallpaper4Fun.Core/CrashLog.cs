namespace Wallpaper4Fun.Core;

/// <summary>
/// Guarda erros inesperados num arquivo de texto, para dar para descobrir o que aconteceu
/// num PC onde não temos depurador. Nunca lança erro: falhar ao gravar não pode piorar a falha.
/// </summary>
public sealed class CrashLog(string filePath)
{
    public string FilePath { get; } = filePath;

    public void Write(Exception error, DateTimeOffset when)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FilePath))!);
            File.AppendAllText(FilePath, Format(error, when) + Environment.NewLine);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Sem onde gravar: a mensagem na tela ainda mostra o erro.
        }
    }

    /// <summary>Data, tipo, mensagem, erros internos e stack trace (<c>ToString</c> já inclui tudo isso).</summary>
    public static string Format(Exception error, DateTimeOffset when) =>
        $"===== {when:yyyy-MM-dd HH:mm:ss zzz} =====" + Environment.NewLine + error;
}

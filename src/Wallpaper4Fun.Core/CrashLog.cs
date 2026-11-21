namespace Wallpaper4Fun.Core;

/// <summary>
/// Guarda erros inesperados e passos da inicialização num arquivo de texto, para dar para
/// descobrir o que aconteceu num PC onde não temos depurador. Nunca lança erro: falhar ao
/// gravar não pode piorar a falha.
/// </summary>
public sealed class CrashLog(string filePath)
{
    public string FilePath { get; } = filePath;

    public void Write(Exception error, DateTimeOffset when) => Append(Format(error, when));

    /// <summary>Uma linha curta, para marcar até onde a inicialização chegou.</summary>
    public void Note(string step, DateTimeOffset when) => Append($"{when:yyyy-MM-dd HH:mm:ss.fff} {step}");

    /// <summary>Data, tipo, mensagem, erros internos e stack trace (<c>ToString</c> já inclui tudo isso).</summary>
    public static string Format(Exception error, DateTimeOffset when) =>
        $"===== {when:yyyy-MM-dd HH:mm:ss zzz} =====" + Environment.NewLine + error;

    private void Append(string text)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FilePath))!);
            File.AppendAllText(FilePath, text + Environment.NewLine);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Sem onde gravar: a mensagem na tela (ou no console) ainda mostra o problema.
        }
    }
}

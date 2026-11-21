using System.Reflection;

namespace Wallpaper4Fun.Core;

/// <summary>O mínimo que todo tipo de wallpaper tem. A parte visual fica no app (<c>IWallpaperSource</c>).</summary>
public interface IWallpaperSourceInfo
{
    /// <summary>Identificador salvo nas configurações. Não mude depois de publicado.</summary>
    string Id { get; }
    string Title { get; }
}

/// <summary>
/// Lista dos tipos de wallpaper disponíveis. Open-closed: um tipo novo entra só
/// sendo registrado aqui, sem mudar quem usa o registro.
/// </summary>
public sealed class SourceRegistry<T> where T : IWallpaperSourceInfo
{
    public SourceRegistry(IReadOnlyList<T> sources)
    {
        if (sources.Count == 0) throw new ArgumentException("O registro precisa de pelo menos um tipo.", nameof(sources));
        if (HasDuplicateIds(sources)) throw new ArgumentException("Há tipos com o mesmo id.", nameof(sources));
        Sources = sources;
    }

    public IReadOnlyList<T> Sources { get; }

    /// <summary>Um id desconhecido (ex.: tipo removido) cai no primeiro tipo da lista.</summary>
    public T Source(string id) => Sources.FirstOrDefault(s => s.Id == id) ?? Sources[0];

    private static bool HasDuplicateIds(IReadOnlyList<T> sources) =>
        sources.Select(s => s.Id).Distinct().Count() != sources.Count;
}

/// <summary>Um shader. O código SkSL fica em <c>Shaders/&lt;Id&gt;.sksl</c>.</summary>
public sealed record ShaderStyle(string Id, string Title)
{
    public string Source => ShaderCatalog.LoadSource(Id);
}

/// <summary>
/// Shaders disponíveis. Para adicionar um: crie <c>Shaders/&lt;id&gt;.sksl</c> e inclua
/// uma entrada em <see cref="All"/>; nada mais precisa mudar (open-closed).
/// </summary>
public static class ShaderCatalog
{
    public const string DefaultId = "aurora";

    public static IReadOnlyList<ShaderStyle> All { get; } =
    [
        new(DefaultId, "Aurora"),
        new("waves", "Ondas"),
        new("plasma", "Plasma"),
    ];

    /// <summary>Um id desconhecido (ex.: shader removido) cai no primeiro da lista.</summary>
    public static ShaderStyle Style(string id) => All.FirstOrDefault(s => s.Id == id) ?? All[0];

    internal static string LoadSource(string id)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Shaders.{id}.sksl")
            ?? throw new InvalidOperationException($"Shader '{id}' não encontrado em Shaders/.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

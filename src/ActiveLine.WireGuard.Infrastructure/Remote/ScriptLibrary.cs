using System.Reflection;

namespace ActiveLine.WireGuard.Infrastructure.Remote;

/// <summary>Lê os scripts .sh embarcados como EmbeddedResource.</summary>
internal static class ScriptLibrary
{
    private static readonly Assembly Assembly = typeof(ScriptLibrary).Assembly;

    public static string Read(string logicalName)
    {
        // RecursiveDir no Linux usa "/", no Windows "\" — normalizamos.
        var name = Assembly.GetManifestResourceNames()
            .SingleOrDefault(n => n.Replace('\\', '/') == logicalName)
            ?? throw new InvalidOperationException($"Script embarcado não encontrado: {logicalName}");

        using var stream = Assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Replace("\r\n", "\n");
    }
}

using ActiveLine.WireGuard.Domain.Common;

namespace ActiveLine.WireGuard.Domain.Storage;

/// <summary>
/// Raiz onde ficam os dados persistentes do servidor.
/// BasePath = "/data" → "/data/docker" (data-root do Docker) e "/data/wireguard" (chaves/peers).
/// </summary>
public sealed record DataVolume
{
    public string BasePath { get; }

    public DataVolume(string basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath) || !basePath.StartsWith('/'))
            throw new DomainException($"O caminho base deve ser absoluto. Recebido: '{basePath}'.");
        if (basePath.TrimEnd('/').Length == 0)
            throw new DomainException("O caminho base não pode ser a raiz '/'.");

        BasePath = basePath.TrimEnd('/');
    }

    public string Combine(string child) => $"{BasePath}/{child}";
}

using System.Text.RegularExpressions;
using ActiveLine.WireGuard.Domain.Common;

namespace ActiveLine.WireGuard.Domain.WireGuard;

/// <summary>
/// Peers (clientes) da VPN. Aceita, como a imagem linuxserver/wireguard:
///   "3"                → peer1, peer2, peer3
///   "notebook,celular" → peer_notebook, peer_celular (somente alfanuméricos)
/// </summary>
public sealed partial record WireGuardPeers
{
    public IReadOnlyList<string> Names { get; }
    public int? Count { get; }

    private WireGuardPeers(IReadOnlyList<string> names, int? count)
    {
        Names = names;
        Count = count;
    }

    public static WireGuardPeers Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Informe ao menos um peer do WireGuard.");

        if (int.TryParse(value.Trim(), out var count))
        {
            if (count < 1)
                throw new DomainException($"Quantidade de peers inválida: {count}.");
            return new WireGuardPeers([], count);
        }

        var names = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var name in names)
        {
            if (!AlphanumericRegex().IsMatch(name))
                throw new DomainException($"Nome de peer inválido '{name}': use apenas letras e números.");
        }
        if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Length)
            throw new DomainException("Há nomes de peers repetidos.");

        return new WireGuardPeers(names, null);
    }

    /// <summary>Valor da variável PEERS do container.</summary>
    public string ToEnvironmentValue() => Count?.ToString() ?? string.Join(',', Names);

    [GeneratedRegex("^[A-Za-z0-9]+$")]
    private static partial Regex AlphanumericRegex();
}

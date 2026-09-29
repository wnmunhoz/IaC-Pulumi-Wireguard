using System.Net;
using System.Net.Sockets;
using ActiveLine.WireGuard.Domain.Common;
using ActiveLine.WireGuard.Domain.Storage;

namespace ActiveLine.WireGuard.Domain.WireGuard;

/// <summary>
/// Estado desejado do WireGuard rodando em container (imagem linuxserver/wireguard).
/// Os valores aqui viram variáveis de ambiente do container; a validação acontece antes
/// de qualquer SSH, então um erro de config falha já no "pulumi preview".
/// </summary>
public sealed record WireGuardSettings
{
    public const int ContainerPort = 51820;

    public DataVolume Volume { get; }
    public string Image { get; }
    public string ServerUrl { get; }
    public int Port { get; }
    public WireGuardPeers Peers { get; }
    public string PeerDns { get; }
    public string InternalSubnet { get; }
    public string AllowedIps { get; }
    public string TimeZone { get; }

    /// <summary>Diretório no host com compose + /config (chaves, peers). Preservado no destroy.</summary>
    public string Directory => Volume.Combine("wireguard");
    public string ConfigDirectory => $"{Directory}/config";

    public WireGuardSettings(
        DataVolume volume,
        string image,
        string serverUrl,
        int port,
        WireGuardPeers peers,
        string peerDns,
        string internalSubnet,
        string allowedIps,
        string timeZone)
    {
        if (string.IsNullOrWhiteSpace(image))
            throw new DomainException("A imagem do WireGuard é obrigatória.");
        if (string.IsNullOrWhiteSpace(serverUrl))
            throw new DomainException("O serverUrl do WireGuard é obrigatório (use 'auto' para detectar o IP público).");
        if (port is < 1 or > 65535)
            throw new DomainException($"Porta do WireGuard inválida: {port}.");
        if (!IPAddress.TryParse(internalSubnet, out var subnet)
            || subnet.AddressFamily != AddressFamily.InterNetwork
            || !internalSubnet.EndsWith(".0"))
            throw new DomainException($"internalSubnet deve ser uma rede IPv4 /24, ex.: 10.13.13.0. Recebido: '{internalSubnet}'.");
        if (string.IsNullOrWhiteSpace(allowedIps))
            throw new DomainException("allowedIps é obrigatório (ex.: '0.0.0.0/0, ::/0').");
        if (string.IsNullOrWhiteSpace(peerDns))
            throw new DomainException("peerDns é obrigatório (use 'auto' para o DNS do próprio container).");

        Volume = volume;
        Image = image.Trim();
        ServerUrl = serverUrl.Trim();
        Port = port;
        Peers = peers;
        PeerDns = peerDns.Trim();
        InternalSubnet = internalSubnet;
        AllowedIps = allowedIps.Trim();
        TimeZone = string.IsNullOrWhiteSpace(timeZone) ? "Etc/UTC" : timeZone.Trim();
    }
}

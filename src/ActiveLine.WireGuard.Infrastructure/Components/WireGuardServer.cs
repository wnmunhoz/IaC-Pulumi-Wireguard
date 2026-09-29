using ActiveLine.WireGuard.Domain.WireGuard;
using ActiveLine.WireGuard.Infrastructure.Remote;
using Pulumi;
using Pulumi.Command.Remote;

namespace ActiveLine.WireGuard.Infrastructure.Components;

public sealed class WireGuardServerArgs
{
    public required RemoteHost Host { get; init; }
    public required WireGuardSettings Settings { get; init; }
}

/// <summary>
/// WireGuard rodando como container (docker compose) com a imagem linuxserver/wireguard.
/// O compose e as chaves ficam em {BasePath}/wireguard — o destroy derruba o container
/// mas preserva as chaves, para os clientes não precisarem ser reconfigurados.
/// </summary>
public sealed class WireGuardServer : ComponentResource
{
    /// <summary>stdout do script: status do container + peers gerados.</summary>
    [Output] public Output<string> Info { get; }

    public WireGuardServer(string name, WireGuardServerArgs args, ComponentResourceOptions? options = null)
        : base("activeline:server:WireGuardServer", name, options)
    {
        var s = args.Settings;
        var variables = new Dictionary<string, string>
        {
            ["WG_DIR"] = s.Directory,
            ["WG_CONFIG_DIR"] = s.ConfigDirectory,
            ["WG_IMAGE"] = s.Image,
            ["WG_SERVER_URL"] = s.ServerUrl,
            ["WG_PORT"] = s.Port.ToString(),
            ["WG_CONTAINER_PORT"] = WireGuardSettings.ContainerPort.ToString(),
            ["WG_PEERS"] = s.Peers.ToEnvironmentValue(),
            ["WG_PEER_DNS"] = s.PeerDns,
            ["WG_INTERNAL_SUBNET"] = s.InternalSubnet,
            ["WG_ALLOWED_IPS"] = s.AllowedIps,
            ["WG_TZ"] = s.TimeZone,
        };

        // Qualquer mudança nas settings muda o texto do script → o Pulumi roda de novo
        // e o "docker compose up -d" recria o container com as novas variáveis.
        var install = new Command($"{name}-install",
            args.Host.Script("WireGuard/install.sh", "WireGuard/uninstall.sh", variables),
            new CustomResourceOptions { Parent = this });

        Info = install.Stdout;

        RegisterOutputs(new Dictionary<string, object?> { ["info"] = Info });
    }
}

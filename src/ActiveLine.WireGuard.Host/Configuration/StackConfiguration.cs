using ActiveLine.WireGuard.Domain.Docker;
using ActiveLine.WireGuard.Domain.Servers;
using ActiveLine.WireGuard.Domain.Storage;
using ActiveLine.WireGuard.Domain.WireGuard;
using Pulumi;

namespace ActiveLine.WireGuard.Host.Configuration;

/// <summary>
/// Equivalente ao IOptions/appsettings do ASP.NET: lê o Pulumi.&lt;stack&gt;.yaml
/// e converte para objetos do Domain (onde as validações acontecem).
///
///   server:name               (default vpn)
///   server:host               (obrigatório)
///   server:user               (obrigatório; usuário com sudo)
///   server:port               (default 22)
///   server:privateKey         (secret)  ─┐ um dos dois
///   server:privateKeyPath               ─┘
///   server:sudoPassword       (secret, opcional; sem ele exige NOPASSWD)
///   provisioning:basePath     (default /data → /data/docker e /data/wireguard)
///   wireguard:serverUrl       (default auto = IP público detectado)
///   wireguard:port            (default 51820)
///   wireguard:peers           (default 1; número ou nomes separados por vírgula)
///   wireguard:peerDns         (default auto)
///   wireguard:internalSubnet  (default 10.13.13.0)
///   wireguard:allowedIps      (default "0.0.0.0/0, ::/0")
///   wireguard:timezone        (default America/Sao_Paulo)
///   wireguard:image           (default lscr.io/linuxserver/wireguard:latest)
/// </summary>
public sealed class StackConfiguration
{
    public ServerProvisioningPlan Plan { get; }
    public Output<string> PrivateKey { get; }
    public Output<string>? SudoPassword { get; }

    private StackConfiguration(ServerProvisioningPlan plan, Output<string> privateKey, Output<string>? sudoPassword)
    {
        Plan = plan;
        PrivateKey = privateKey;
        SudoPassword = sudoPassword;
    }

    public static StackConfiguration Load()
    {
        var server = new Config("server");
        var provisioning = new Config("provisioning");
        var wireGuard = new Config("wireguard");

        var endpoint = new ServerEndpoint(
            host: server.Require("host"),
            port: server.GetInt32("port") ?? 22,
            user: server.Require("user"));

        var volume = new DataVolume(provisioning.Get("basePath") ?? "/data");

        var wireGuardSettings = new WireGuardSettings(
            volume: volume,
            image: wireGuard.Get("image") ?? "lscr.io/linuxserver/wireguard:latest",
            serverUrl: wireGuard.Get("serverUrl") ?? "auto",
            port: wireGuard.GetInt32("port") ?? WireGuardSettings.ContainerPort,
            peers: WireGuardPeers.Parse(wireGuard.Get("peers") ?? "1"),
            peerDns: wireGuard.Get("peerDns") ?? "auto",
            internalSubnet: wireGuard.Get("internalSubnet") ?? "10.13.13.0",
            allowedIps: wireGuard.Get("allowedIps") ?? "0.0.0.0/0, ::/0",
            timeZone: wireGuard.Get("timezone") ?? "America/Sao_Paulo");

        var plan = new ServerProvisioningPlan(
            Name: server.Get("name") ?? "vpn",
            Endpoint: endpoint,
            Docker: new DockerEngineSettings(volume),
            WireGuard: wireGuardSettings);

        return new StackConfiguration(plan, LoadPrivateKey(server), server.GetSecret("sudoPassword"));
    }

    private static Output<string> LoadPrivateKey(Config server)
    {
        // Preferência: chave guardada como secret no próprio stack (criptografada no yaml).
        var secret = server.GetSecret("privateKey");
        if (secret is not null)
            return secret;

        var path = server.Get("privateKeyPath")
            ?? throw new InvalidOperationException(
                "Configure 'server:privateKey' (--secret) ou 'server:privateKeyPath'.");

        path = path.StartsWith("~/")
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path[2..])
            : path;

        // CreateSecret garante que o conteúdo da chave nunca vá em claro para o state.
        return Output.CreateSecret(File.ReadAllText(path));
    }
}

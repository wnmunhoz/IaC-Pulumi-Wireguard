using ActiveLine.WireGuard.Domain.Servers;
using ActiveLine.WireGuard.Infrastructure.Remote;
using Pulumi;
using Pulumi.Command.Remote;

namespace ActiveLine.WireGuard.Infrastructure.Components;

/// <summary>
/// Componente raiz de um servidor: recebe o Aggregate do Domain
/// (ServerProvisioningPlan) e materializa a árvore de recursos:
///
///   ProvisionedServer
///    ├─ Command  (facts)   → valida SSH, Debian e sudo
///    ├─ DockerEngine       → depende de facts
///    └─ WireGuardServer    → depende de DockerEngine (roda dentro dele)
/// </summary>
public sealed class ProvisionedServer : ComponentResource
{
    [Output] public Output<string> OperatingSystem { get; }
    [Output] public Output<string> DockerInfo { get; }
    [Output] public Output<string> WireGuardInfo { get; }

    public ProvisionedServer(
        ServerProvisioningPlan plan,
        RemoteHost host,
        ComponentResourceOptions? options = null)
        : base("activeline:server:ProvisionedServer", plan.Name, options)
    {
        var facts = new Command($"{plan.Name}-facts",
            host.Script("Server/facts.sh"),
            new CustomResourceOptions { Parent = this });

        var docker = new DockerEngine($"{plan.Name}-docker", new DockerEngineArgs
        {
            Host = host,
            Settings = plan.Docker,
        }, new ComponentResourceOptions { Parent = this, DependsOn = { facts } });

        // DependsOn explícito: não há dado fluindo do Docker para o WireGuard, mas o
        // container precisa do engine instalado. No destroy a ordem inverte sozinha:
        // primeiro derruba o container, depois remove o Docker.
        var wireGuard = new WireGuardServer($"{plan.Name}-wireguard", new WireGuardServerArgs
        {
            Host = host,
            Settings = plan.WireGuard,
        }, new ComponentResourceOptions { Parent = this, DependsOn = { docker } });

        OperatingSystem = facts.Stdout;
        DockerInfo = docker.Info;
        WireGuardInfo = wireGuard.Info;

        RegisterOutputs(new Dictionary<string, object?>
        {
            ["operatingSystem"] = OperatingSystem,
            ["dockerInfo"] = DockerInfo,
            ["wireGuardInfo"] = WireGuardInfo,
        });
    }
}

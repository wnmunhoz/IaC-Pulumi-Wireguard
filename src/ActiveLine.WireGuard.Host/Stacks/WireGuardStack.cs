using ActiveLine.WireGuard.Host.Configuration;
using ActiveLine.WireGuard.Infrastructure.Components;
using ActiveLine.WireGuard.Infrastructure.Remote;
using Pulumi;

namespace ActiveLine.WireGuard.Host.Stacks;

/// <summary>
/// A Stack é o "Program/Startup": só compõe. Toda regra fica no Domain
/// e todo "como fazer" fica nos Components da Infrastructure.
/// </summary>
public sealed class WireGuardStack : Stack
{
    // [Output] na Stack = o que aparece em "pulumi stack output".
    [Output] public Output<string> Server { get; set; }
    [Output] public Output<string> OperatingSystem { get; set; }
    [Output] public Output<string> Docker { get; set; }
    [Output] public Output<string> WireGuard { get; set; }

    public WireGuardStack()
    {
        var config = StackConfiguration.Load();

        var host = new RemoteHost(config.Plan.Endpoint, config.PrivateKey, config.SudoPassword);
        var server = new ProvisionedServer(config.Plan, host);

        Server = Output.Create(config.Plan.Endpoint.ToString());
        OperatingSystem = server.OperatingSystem;
        Docker = server.DockerInfo;
        WireGuard = server.WireGuardInfo;
    }
}

using ActiveLine.WireGuard.Domain.Docker;
using ActiveLine.WireGuard.Domain.WireGuard;

namespace ActiveLine.WireGuard.Domain.Servers;

/// <summary>
/// Aggregate que descreve o estado desejado de UM servidor.
/// É o "o quê"; a camada de Infrastructure decide o "como" (Pulumi + SSH + sudo).
/// </summary>
public sealed record ServerProvisioningPlan(
    string Name,
    ServerEndpoint Endpoint,
    DockerEngineSettings Docker,
    WireGuardSettings WireGuard);

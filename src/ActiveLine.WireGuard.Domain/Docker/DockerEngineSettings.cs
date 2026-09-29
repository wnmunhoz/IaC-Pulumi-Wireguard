using ActiveLine.WireGuard.Domain.Storage;

namespace ActiveLine.WireGuard.Domain.Docker;

/// <summary>
/// Regras de como o Docker deve existir no servidor:
/// imagens, volumes, containers etc. vão para "{BasePath}/docker" em vez de /var/lib/docker.
/// </summary>
public sealed record DockerEngineSettings(DataVolume Volume)
{
    public string DataRoot => Volume.Combine("docker");
}

using ActiveLine.WireGuard.Domain.Docker;
using ActiveLine.WireGuard.Infrastructure.Remote;
using Pulumi;
using Pulumi.Command.Remote;

namespace ActiveLine.WireGuard.Infrastructure.Components;

public sealed class DockerEngineArgs
{
    public required RemoteHost Host { get; init; }
    public required DockerEngineSettings Settings { get; init; }
}

/// <summary>
/// ComponentResource = um "agregado" de recursos Pulumi com nome próprio.
/// Aparece no preview como um nó da árvore: activeline:server:DockerEngine.
/// </summary>
public sealed class DockerEngine : ComponentResource
{
    /// <summary>stdout do script de instalação (versão + DataRoot efetivo).</summary>
    [Output] public Output<string> Info { get; }

    public DockerEngine(string name, DockerEngineArgs args, ComponentResourceOptions? options = null)
        : base("activeline:server:DockerEngine", name, options)
    {
        var variables = new Dictionary<string, string>
        {
            ["DOCKER_DATA_ROOT"] = args.Settings.DataRoot,
        };

        // Command (remote) = "rode isto via SSH".
        //  - Create: roda no primeiro "pulumi up" e sempre que o texto do script mudar.
        //  - Delete: roda no "pulumi destroy".
        var install = new Command($"{name}-install",
            args.Host.Script("Docker/install.sh", "Docker/uninstall.sh", variables),
            new CustomResourceOptions { Parent = this });

        Info = install.Stdout;

        RegisterOutputs(new Dictionary<string, object?> { ["info"] = Info });
    }
}

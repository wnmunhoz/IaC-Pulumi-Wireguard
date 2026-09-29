using ActiveLine.WireGuard.Domain.Servers;
using Pulumi;
using Pulumi.Command.Remote;
using Pulumi.Command.Remote.Inputs;

namespace ActiveLine.WireGuard.Infrastructure.Remote;

/// <summary>
/// "Como falar com o servidor": conexão SSH + como elevar privilégio (sudo).
///  - sem senha  → os scripts usam "sudo -n" (exige NOPASSWD no sudoers);
///  - com senha  → a senha vai pelo STDIN do comando (Output secreto) e os scripts usam "sudo -A".
/// </summary>
public sealed class RemoteHost
{
    private readonly Output<string>? _sudoPassword;

    public ConnectionArgs Connection { get; }

    public RemoteHost(ServerEndpoint endpoint, Input<string> privateKey, Output<string>? sudoPassword)
    {
        Connection = new ConnectionArgs
        {
            Host = endpoint.Host,
            Port = endpoint.Port,
            User = endpoint.User,
            PrivateKey = privateKey,   // Output secreto: nunca aparece em log/state em texto puro
            DialErrorLimit = 10,       // tentativas de conexão antes de falhar
            PerDialTimeout = 15,       // segundos por tentativa
        };
        _sudoPassword = sudoPassword;
    }

    /// <summary>CommandArgs com conexão, scripts e (se houver) senha do sudo no stdin.</summary>
    public CommandArgs Script(
        string createScript,
        string? deleteScript = null,
        IReadOnlyDictionary<string, string>? variables = null)
    {
        var withPassword = _sudoPassword is not null;

        var args = new CommandArgs
        {
            Connection = Connection,
            Create = RemoteScript.Build(createScript, variables, withPassword),
        };

        // Delete: roda no "pulumi destroy" (se o recurso tiver um).
        if (deleteScript is not null)
            args.Delete = RemoteScript.Build(deleteScript, variables, withPassword);

        // Stdin: lido pela 1ª linha do comando (RemoteScript) e exportado como SUDO_PASSWORD.
        if (_sudoPassword is not null)
            args.Stdin = _sudoPassword.Apply(p => p + "\n");

        return args;
    }
}

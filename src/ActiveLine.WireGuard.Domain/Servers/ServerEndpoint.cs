using ActiveLine.WireGuard.Domain.Common;

namespace ActiveLine.WireGuard.Domain.Servers;

/// <summary>
/// Value Object que identifica "onde" e "como quem" nos conectamos.
/// Não conhece Pulumi nem SSH — só as regras do que é um endpoint válido.
/// </summary>
public sealed record ServerEndpoint
{
    public string Host { get; }
    public int Port { get; }
    public string User { get; }

    public ServerEndpoint(string host, int port, string user)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new DomainException("O host do servidor é obrigatório.");
        if (port is < 1 or > 65535)
            throw new DomainException($"Porta SSH inválida: {port}.");
        if (string.IsNullOrWhiteSpace(user))
            throw new DomainException("O usuário SSH é obrigatório.");

        Host = host.Trim();
        Port = port;
        User = user.Trim();
    }

    public override string ToString() => $"{User}@{Host}:{Port}";
}

using System.Text;

namespace ActiveLine.WireGuard.Infrastructure.Remote;

/// <summary>
/// Monta o comando final enviado via SSH:
///   IFS= read -r SUDO_PASSWORD || true; export SUDO_PASSWORD   ← só se houver senha (vem do stdin)
///   bash -s &lt;&lt;'PULUMI_SCRIPT'
///   VAR='valor'        ← variáveis vindas do Domain
///   (prelude.sh)       ← set -euo pipefail, $SUDO, pkg_install...
///   (script.sh)
///   PULUMI_SCRIPT
/// Assim não dependemos do shell default do usuário remoto nem de AcceptEnv no sshd,
/// e a senha do sudo nunca faz parte do texto do comando (nem do state, nem do "ps").
/// </summary>
internal static class RemoteScript
{
    private const string Delimiter = "PULUMI_SCRIPT";

    public static string Build(
        string scriptName,
        IReadOnlyDictionary<string, string>? variables = null,
        bool readSudoPasswordFromStdin = false)
    {
        var body = new StringBuilder();

        foreach (var (key, value) in variables ?? new Dictionary<string, string>())
            body.Append(key).Append('=').Append(ShellQuote(value)).Append('\n');

        body.Append(ScriptLibrary.Read("Common/prelude.sh")).Append('\n');
        body.Append(ScriptLibrary.Read(scriptName));

        var passwordReader = readSudoPasswordFromStdin
            ? "IFS= read -r SUDO_PASSWORD || true; export SUDO_PASSWORD\n"
            : string.Empty;

        return $"{passwordReader}bash -s <<'{Delimiter}'\n{body.ToString().TrimEnd()}\n{Delimiter}\n";
    }

    private static string ShellQuote(string value) => "'" + value.Replace("'", "'\"'\"'") + "'";
}

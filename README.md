# activeline-wireguard — Pulumi (C#)

Conecta num servidor **Debian** via **SSH (chave privada)** com um usuário que usa **sudo**,
instala o **Docker** com o data-root em **`/data/docker`** e sobe o **WireGuard** em container
(imagem [`linuxserver/wireguard`](https://docs.linuxserver.io/images/docker-wireguard/)),
com chaves e peers persistidos em **`/data/wireguard`**.

Mesma arquitetura do projeto `testes-pulumi`.

## Estrutura

```
Pulumi.yaml                      ← "manifesto" do projeto Pulumi (aponta o main para o Host)
Pulumi.dev.yaml                  ← config do stack "dev" (não versionado; ver .example)
src/
├─ ActiveLine.WireGuard.Domain/          ← SEM dependência de Pulumi
│   ├─ Servers/ServerEndpoint.cs         Value Object (host/porta/usuário + validação)
│   ├─ Servers/ServerProvisioningPlan.cs Aggregate: estado desejado do servidor
│   ├─ Storage/DataVolume.cs             /data → /data/docker e /data/wireguard
│   ├─ Docker/DockerEngineSettings.cs    data-root do Docker
│   ├─ WireGuard/WireGuardSettings.cs    porta, serverUrl, subnet, allowedIps, DNS...
│   └─ WireGuard/WireGuardPeers.cs       "3" ou "notebook,celular"
│
├─ ActiveLine.WireGuard.Infrastructure/  ← "COMO": Pulumi + Pulumi.Command (SSH)
│   ├─ Components/ProvisionedServer.cs   ComponentResource raiz (orquestra)
│   ├─ Components/DockerEngine.cs        ComponentResource
│   ├─ Components/WireGuardServer.cs     ComponentResource (docker compose)
│   ├─ Remote/RemoteHost.cs              conexão SSH + estratégia de sudo
│   ├─ Remote/RemoteScript.cs            monta o comando enviado via SSH
│   └─ Scripts/**/*.sh                   bash embarcado (EmbeddedResource)
│
└─ ActiveLine.WireGuard.Host/            ← composition root (o Pulumi roda "dotnet run" aqui)
    ├─ Program.cs                        Deployment.RunAsync<WireGuardStack>()
    ├─ Configuration/StackConfiguration  lê Pulumi.<stack>.yaml → objetos do Domain
    └─ Stacks/WireGuardStack.cs          instancia o ProvisionedServer e expõe outputs
```

Dependências: `Host → Infrastructure → Domain`.

## Sudo

Os scripts rodam como o usuário SSH e elevam com `sudo` apenas onde precisa:

| Situação                                   | O que acontece                                                   |
|--------------------------------------------|------------------------------------------------------------------|
| usuário com `NOPASSWD` no sudoers          | `sudo -n` (nada a configurar)                                    |
| sudo pede senha                            | `pulumi config set --secret server:sudoPassword` → `sudo -A`     |
| usuário é root                             | sem sudo                                                         |

Com senha, ela é enviada pelo **stdin** do comando SSH (Output secreto, criptografado no state)
e entregue ao sudo por um `SUDO_ASKPASS` temporário — nunca aparece no texto do comando,
no `ps` do servidor nem em disco. O recurso `facts` valida o sudo (e se é Debian) antes de tudo.

## Pré-requisitos

- .NET SDK 10
- Pulumi CLI: `curl -fsSL https://get.pulumi.com | sh`
- Servidor **Debian 11+** (o módulo `wireguard` já vem no kernel) com usuário no grupo `sudo`.
- Porta UDP do WireGuard (default `51820`) liberada/encaminhada até o servidor.

## Rodando

```bash
# 1. Backend de state local (não precisa de conta no Pulumi Cloud)
pulumi login --local
export PULUMI_CONFIG_PASSPHRASE="troque-isto"   # protege os secrets do stack

# 2. Stack
pulumi stack init dev

# 3. Servidor
pulumi config set server:host 192.168.0.10
pulumi config set server:user debian
pulumi config set server:port 22                        # opcional
pulumi config set server:privateKeyPath ~/.ssh/id_ed25519
#   ou: cat ~/.ssh/id_ed25519 | pulumi config set --secret server:privateKey
pulumi config set --secret server:sudoPassword           # só se o sudo pedir senha
pulumi config set provisioning:basePath /data            # opcional (default /data)

# 4. WireGuard
pulumi config set wireguard:serverUrl vpn.exemplo.com.br # IP/DNS público (default: auto)
pulumi config set wireguard:peers notebook,celular        # ou um número, ex.: 3
pulumi config set wireguard:port 51820                    # opcional
pulumi config set wireguard:allowedIps "0.0.0.0/0, ::/0"  # opcional (split tunnel: "10.13.13.0/24,192.168.0.0/24")
pulumi config set wireguard:peerDns auto                  # opcional
pulumi config set wireguard:internalSubnet 10.13.13.0     # opcional

# 5. Ver o plano e aplicar
pulumi preview
pulumi up

# 6. Resultados
pulumi stack output
```

Saída esperada do `preview`:

```
 +  pulumi:pulumi:Stack                         activeline-wireguard-dev
 +  └─ activeline:server:ProvisionedServer      vpn
 +     ├─ command:remote:Command                vpn-facts
 +     ├─ activeline:server:DockerEngine        vpn-docker
 +     │  └─ command:remote:Command             vpn-docker-install
 +     └─ activeline:server:WireGuardServer     vpn-wireguard
 +        └─ command:remote:Command             vpn-wireguard-install
```

## Configurando os clientes

As chaves privadas dos peers **não** vão para o state do Pulumi; pegue-as no servidor:

```bash
# QR code no terminal (app WireGuard do celular)
sudo docker exec wireguard /app/show-peer celular

# arquivo .conf (notebook)
sudo cat /data/wireguard/config/peer_notebook/peer_notebook.conf
```

Para adicionar/remover peers basta alterar `wireguard:peers` e rodar `pulumi up`: o container
é recriado e os peers existentes mantêm as chaves.

Verificação no servidor: `docker info | grep "Docker Root Dir"` → `/data/docker`
e `sudo docker exec wireguard wg show`.

## Destroy

`pulumi destroy` roda os `uninstall.sh` na ordem inversa: derruba o container do WireGuard
e remove os pacotes do Docker, mas **preserva `/data/docker` e `/data/wireguard/config`** (chaves).

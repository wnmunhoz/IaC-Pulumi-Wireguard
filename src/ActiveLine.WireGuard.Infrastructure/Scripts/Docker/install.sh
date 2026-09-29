# Instala o Docker Engine pelo repositório oficial (Debian) com data-root em ${DOCKER_DATA_ROOT}.
. /etc/os-release

$SUDO mkdir -p "${DOCKER_DATA_ROOT}" /etc/docker

# daemon.json ANTES da instalação: o serviço já sobe usando ${DOCKER_DATA_ROOT}
# e nada é criado em /var/lib/docker.
DAEMON_JSON="$(mktemp)"
cat > "${DAEMON_JSON}" <<JSON
{
  "data-root": "${DOCKER_DATA_ROOT}",
  "log-driver": "json-file",
  "log-opts": { "max-size": "10m", "max-file": "3" }
}
JSON

CHANGED=0
if ! $SUDO cmp -s "${DAEMON_JSON}" /etc/docker/daemon.json; then
  $SUDO install -m 0644 "${DAEMON_JSON}" /etc/docker/daemon.json
  CHANGED=1
fi
rm -f "${DAEMON_JSON}"

if ! command -v docker >/dev/null 2>&1; then
  pkg_install ca-certificates curl
  $SUDO install -m 0755 -d /etc/apt/keyrings
  curl -fsSL https://download.docker.com/linux/debian/gpg | $SUDO tee /etc/apt/keyrings/docker.asc >/dev/null
  $SUDO chmod a+r /etc/apt/keyrings/docker.asc
  echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/debian ${VERSION_CODENAME} stable" \
    | $SUDO tee /etc/apt/sources.list.d/docker.list >/dev/null
  pkg_install docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
fi

$SUDO systemctl enable docker >/dev/null 2>&1
# Só reinicia se a config mudou (evita derrubar containers a cada "pulumi up").
if [ "${CHANGED}" -eq 1 ] || ! $SUDO systemctl is-active --quiet docker; then
  $SUDO systemctl restart docker
fi

# Permite usar docker sem sudo (vale a partir do próximo login).
if [ "$(id -u)" -ne 0 ]; then $SUDO usermod -aG docker "$(id -un)"; fi

# stdout vira o output "stdout" do recurso no Pulumi.
$SUDO docker version --format 'Docker {{.Server.Version}}'
$SUDO docker info --format 'DataRoot={{.DockerRootDir}}'

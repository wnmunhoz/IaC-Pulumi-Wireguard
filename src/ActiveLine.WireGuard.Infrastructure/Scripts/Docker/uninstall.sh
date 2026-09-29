# Executado no "pulumi destroy". Remove pacotes mas PRESERVA ${DOCKER_DATA_ROOT}.
$SUDO systemctl stop docker docker.socket 2>/dev/null || true
pkg_remove docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
$SUDO rm -f /etc/docker/daemon.json /etc/apt/sources.list.d/docker.list /etc/apt/keyrings/docker.asc
echo "Docker removido. Dados preservados em ${DOCKER_DATA_ROOT}."

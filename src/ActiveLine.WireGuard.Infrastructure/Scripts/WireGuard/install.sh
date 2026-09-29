# Sobe o WireGuard (linuxserver/wireguard) via docker compose em ${WG_DIR}.

# O Debian 11+ já traz o módulo no kernel; o container usa o módulo do host.
if ! $SUDO modprobe wireguard; then
  echo "Módulo 'wireguard' indisponível no kernel $(uname -r)." >&2
  exit 1
fi

$SUDO mkdir -p "${WG_CONFIG_DIR}"
$SUDO chmod 700 "${WG_DIR}"

$SUDO tee "${WG_DIR}/compose.yaml" >/dev/null <<YAML
# Gerado pelo Pulumi (activeline-wireguard). Alterações manuais serão sobrescritas.
services:
  wireguard:
    image: "${WG_IMAGE}"
    container_name: wireguard
    cap_add:
      - NET_ADMIN
      - SYS_MODULE
    environment:
      PUID: "1000"
      PGID: "1000"
      TZ: "${WG_TZ}"
      SERVERURL: "${WG_SERVER_URL}"
      SERVERPORT: "${WG_PORT}"
      PEERS: "${WG_PEERS}"
      PEERDNS: "${WG_PEER_DNS}"
      INTERNAL_SUBNET: "${WG_INTERNAL_SUBNET}"
      ALLOWEDIPS: "${WG_ALLOWED_IPS}"
      LOG_CONFS: "false"
    volumes:
      - "${WG_CONFIG_DIR}:/config"
      - /lib/modules:/lib/modules:ro
    ports:
      - "${WG_PORT}:${WG_CONTAINER_PORT}/udp"
    sysctls:
      - net.ipv4.conf.all.src_valid_mark=1
    restart: unless-stopped
YAML

$SUDO docker compose -f "${WG_DIR}/compose.yaml" pull --quiet
$SUDO docker compose -f "${WG_DIR}/compose.yaml" up -d --remove-orphans

# Aguarda o container gerar a config do servidor e dos peers.
for _ in $(seq 1 60); do
  if $SUDO test -f "${WG_CONFIG_DIR}/wg_confs/wg0.conf"; then break; fi
  sleep 2
done
if ! $SUDO test -f "${WG_CONFIG_DIR}/wg_confs/wg0.conf"; then
  echo "WireGuard não gerou ${WG_CONFIG_DIR}/wg_confs/wg0.conf a tempo. Logs:" >&2
  $SUDO docker logs --tail 50 wireguard >&2 || true
  exit 1
fi

echo "WireGuard $($SUDO docker inspect -f '{{.State.Status}}' wireguard) | udp/${WG_PORT} | ${WG_SERVER_URL}"
echo "Peers: $($SUDO find "${WG_CONFIG_DIR}" -maxdepth 1 -type d -name 'peer*' -printf '%f ' | sed 's/ $//')"

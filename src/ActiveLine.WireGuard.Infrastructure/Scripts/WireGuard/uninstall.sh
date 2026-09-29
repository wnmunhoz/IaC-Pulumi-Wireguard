# Executado no "pulumi destroy". Derruba o container mas PRESERVA ${WG_CONFIG_DIR} (chaves dos peers).
if command -v docker >/dev/null 2>&1 && $SUDO test -f "${WG_DIR}/compose.yaml"; then
  $SUDO docker compose -f "${WG_DIR}/compose.yaml" down --remove-orphans || true
fi
$SUDO rm -f "${WG_DIR}/compose.yaml"
echo "WireGuard removido. Chaves preservadas em ${WG_CONFIG_DIR}."

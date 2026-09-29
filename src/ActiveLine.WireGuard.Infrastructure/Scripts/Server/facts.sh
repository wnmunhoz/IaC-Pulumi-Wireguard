# Coleta informações do servidor e valida as premissas (também serve como "ping" do SSH).
. /etc/os-release
if [ "${ID}" != "debian" ]; then
  echo "Distribuição não suportada: ${PRETTY_NAME}. Este projeto provisiona apenas Debian." >&2
  exit 1
fi

if ! $SUDO true; then
  echo "Falha ao usar sudo como '$(id -un)'. Configure NOPASSWD ou 'pulumi config set --secret server:sudoPassword'." >&2
  exit 1
fi

echo "${PRETTY_NAME} | kernel $(uname -r) | $(uname -m) | sudo ok ($(id -un))"

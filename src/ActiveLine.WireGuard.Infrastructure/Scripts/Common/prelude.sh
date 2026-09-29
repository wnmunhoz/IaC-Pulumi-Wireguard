# --- prelude (injetado antes de todo script) ---------------------------------
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

# Elevação de privilégio:
#  - root              → sem sudo
#  - SUDO_PASSWORD set → "sudo -A" com um askpass temporário que lê a senha do ambiente
#                        (a senha não é gravada em disco nem aparece na linha de comando)
#  - caso contrário    → "sudo -n" (falha em vez de pedir senha; exige NOPASSWD)
if [ "$(id -u)" -eq 0 ]; then
  SUDO=""
elif [ -n "${SUDO_PASSWORD:-}" ]; then
  SUDO_ASKPASS="$(mktemp)"
  export SUDO_ASKPASS
  trap 'rm -f "$SUDO_ASKPASS"' EXIT
  cat > "$SUDO_ASKPASS" <<'ASKPASS'
#!/bin/sh
printf '%s\n' "$SUDO_PASSWORD"
ASKPASS
  chmod 700 "$SUDO_ASKPASS"
  SUDO="sudo -A"
else
  SUDO="sudo -n"
fi

# Apenas Debian (apt).
pkg_install() {
  $SUDO apt-get update -y -qq
  $SUDO apt-get install -y -qq "$@"
}

pkg_remove() {
  $SUDO apt-get purge -y -qq "$@" || true
}
# -----------------------------------------------------------------------------

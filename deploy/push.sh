#!/bin/sh
# İki repoyu (commit'lenmiş hâli, HEAD) sunucuya gönderir; .env.server ve sertifikalar sunucuda kalır.
# Kullanım (ProKantar kökünden): sh deploy/push.sh
set -e
HOST=${DEPLOY_HOST:-root@213.142.150.174}
KEY=${DEPLOY_KEY:-$HOME/.ssh/baseforge_deploy}
ROOT=/root/apps/prokantar
here=$(cd "$(dirname "$0")/.." && pwd)
ui=$(cd "$here/../ProKantarUI" && pwd)
ssh -i "$KEY" "$HOST" "mkdir -p $ROOT/ProKantar $ROOT/ProKantarUI"
(cd "$here" && git -c core.autocrlf=false archive --format=tar HEAD) | ssh -i "$KEY" "$HOST" "tar -x -C $ROOT/ProKantar"
(cd "$ui" && git -c core.autocrlf=false archive --format=tar HEAD) | ssh -i "$KEY" "$HOST" "tar -x -C $ROOT/ProKantarUI"
echo "Gönderildi: ProKantar $(cd "$here" && git rev-parse --short HEAD), ProKantarUI $(cd "$ui" && git rev-parse --short HEAD)"

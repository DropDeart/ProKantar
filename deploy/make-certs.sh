#!/bin/sh
# Identity'nin kalıcı imza (token) ve şifreleme (refresh token, yetki kodu) sertifikaları. Bir kez çalıştırılır;
# yeniden üretmek tüm oturumları ve dağıtılmış token'ları geçersiz kılar. Parola .env.server'daki CERT_PASSWORD.
set -e
cd "$(dirname "$0")"
. ./.env.server
[ -n "$CERT_PASSWORD" ] || { echo "CERT_PASSWORD boş"; exit 1; }
mkdir -p certs
make_cert() {
  name=$1; usage=$2
  [ -f "certs/$name.pfx" ] && { echo "certs/$name.pfx zaten var, atlandı"; return; }
  openssl req -x509 -newkey rsa:2048 -sha256 -days 3650 -nodes -subj "/CN=ProKantar Identity $name" \
    -addext "keyUsage=critical,$usage" -keyout "certs/$name.key" -out "certs/$name.crt" 2>/dev/null
  openssl pkcs12 -export -inkey "certs/$name.key" -in "certs/$name.crt" -out "certs/$name.pfx" -passout "pass:$CERT_PASSWORD"
  rm -f "certs/$name.key" "certs/$name.crt"
  echo "certs/$name.pfx üretildi"
}
make_cert signing digitalSignature
make_cert encryption keyEncipherment
# Yalnız root okur (servis imajları root kullanıcıyla çalışır).
chmod 600 certs/*.pfx

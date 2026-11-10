# ProKantar sunucu kurulumu

Tek compose (`docker-compose.server.yml`): identity, kantar, platform, tracking, tek PostgreSQL (PostGIS; dört veritabanı),
RabbitMQ ve web arayüzü. Servisler yalnız `127.0.0.1:84xx`'te dinler; dışarıya nginx (`nginx/prokantar.conf`) açar.

| Adres | Servis | Yerel port |
|---|---|---|
| prokantar.com.tr, www | web (Next.js) | 8409 |
| identity.prokantar.com.tr | identity | 8401 |
| kantar.prokantar.com.tr | kantar | 8403 |
| platform.prokantar.com.tr | platform (SignalR `/hubs/`) | 8405 |
| tracking.prokantar.com.tr | tracking | 8407 |
| log.prokantar.com.tr | Grafana (loglar; Loki yalnız iç ağda) | 8410 |

## İlk kurulum
1. **DNS:** yukarıdaki altı ad için A kaydı → sunucu IP'si.
2. **Kod:** `/root/apps/prokantar/ProKantar` ve `/root/apps/prokantar/ProKantarUI` (yerelden `deploy/push.sh`).
3. **Gizli değerler:** `cp .env.server.example .env.server`, şifreleri üret (`openssl rand -base64 32 | tr -d '/+=' | cut -c1-32`).
4. **Sertifikalar:** `sh make-certs.sh` (identity imza + şifreleme; bir kez).
5. **Çalıştır:** `docker compose -f docker-compose.server.yml --env-file .env.server up -d --build`
6. **nginx:** `cp nginx/prokantar.conf /etc/nginx/sites-available/ && ln -s ../sites-available/prokantar.conf /etc/nginx/sites-enabled/ && nginx -t && systemctl reload nginx`
7. **SSL:** dosyanın başındaki `certbot --nginx ...` komutu.
8. **Yönetici:** identity'de SuperAdmin kullanıcısı yok; kayıt olup rolü veritabanından verin (bkz. aşağı).

## Güncelleme
Yerelde `deploy/push.sh` (iki repo `git archive` ile gönderilir), sunucuda 5. adım. Yalnız değişen servis: `... up -d --build kantar`.

## Kapalı test notları
- `ALLOW_LOG_MESSAGING=true`: SMS ve e-postalar gönderilmez, log'a yazılır: `docker logs prokantar-identity-1 | grep -i kod`.
- `UI_SIMULATOR=1`: `/panel/simulasyon` açık (şoför uygulaması ve kantar cihazı yerine).
- nginx `X-Robots-Tag: noindex` gönderir; canlı açılışta kaldırın.
- OSRM demo sunucusu ve Nominatim/OSM karoları yalnız düşük hacimli test içindir.

## SuperAdmin verme
```sh
docker exec -i prokantar-postgres-1 psql -U baseforge -d identity_db -c \
  "INSERT INTO \"AspNetUserRoles\"(\"UserId\",\"RoleId\") SELECT u.\"Id\", r.\"Id\" FROM \"AspNetUsers\" u, \"AspNetRoles\" r WHERE u.\"Email\"='<yonetici-e-postasi>' AND r.\"Name\"='SuperAdmin';"
```

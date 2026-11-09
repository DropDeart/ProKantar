#!/usr/bin/env python3
"""
Simülasyon için YEREL test firması hazırlar (yalnız yerel docker; production'da çalıştırmayın).

  - Operatör hesabı açar ve şirket başvurusu yapar.
  - Başvuruyu yerel identity veritabanında SQL ile onaylar (belge şartı atlanır; `docker exec identity-postgres-1`).
  - Firmaya istasyon (konumlu), kantar, malzeme (ton), müşteri cari ve iki teslimat noktası tanımlar.
  - Giriş bilgilerini tools/simulate/.env.local'e yazar (gitignore'lu); sevkiyat_simulasyonu.py bunu okur.

Kullanım:  python tools/simulate/yerel_firma_hazirla.py
"""
from __future__ import annotations

import os
import random
import subprocess
import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from sevkiyat_simulasyonu import IDENTITY, KANTAR, call, jwt_sub, ok, step, token_for_tenant, token_password  # noqa: E402

ENV_FILE = Path(__file__).resolve().parent / ".env.local"
IDENTITY_DB_CONTAINER = os.environ.get("PK_IDENTITY_DB_CONTAINER", "identity-postgres-1")


def test_vkn() -> str:
    """Sağlama algoritmasına uyan rastgele test vergi no'su (10 hane)."""
    digits = [random.randint(1, 9)] + [random.randint(0, 9) for _ in range(8)]
    total = 0
    for i, d in enumerate(digits):
        tmp = (d + 10 - (i + 1)) % 10
        total += 9 if tmp == 9 else (tmp * 2 ** (10 - (i + 1))) % 9
    return "".join(map(str, digits)) + str((10 - total % 10) % 10)


def approve_locally(tenant_id: str) -> None:
    sql = f"UPDATE \"Tenants\" SET \"Status\" = 'Active' WHERE \"Id\" = '{tenant_id}'"
    subprocess.run(["docker", "exec", IDENTITY_DB_CONTAINER, "psql", "-U", "baseforge", "-d", "identity_db", "-c", sql],
                   check=True, capture_output=True)


def main() -> None:
    stamp = int(time.time())
    email = f"sim-operator-{stamp}@example.com"
    password = f"Sim!{stamp}Op"

    step("Operatör ve şirket")
    call("POST", f"{IDENTITY}/api/account/register", body={"fullName": "Simülasyon Operatör", "email": email, "password": password})
    login = token_password(email, password)
    membership = call("POST", f"{IDENTITY}/api/tenants", login["access_token"], body={
        "name": f"Simülasyon Lojistik {stamp % 10000}", "taxNo": test_vkn(), "taxOffice": "Kadıköy",
        "phone": "2165550000", "email": email, "city": "İstanbul", "district": "Tuzla",
        "address": "Aydınlı Mah. Simülasyon Cd. No:1", "postalCode": "34953",
    })
    tenant_id = membership["tenant"]["id"]
    approve_locally(tenant_id)
    op = token_for_tenant(login["refresh_token"], tenant_id)["access_token"]
    ok(f"{membership['tenant']['name']} ({tenant_id}) yerelde onaylandı · {email}")

    step("Tanımlar")
    station_id = call("POST", f"{KANTAR}/api/Stations", op, body={
        "name": "Tuzla Ocak", "code": f"S{stamp % 1000:03d}", "address": "Aydınlı Mah. Ocak Yolu No:5", "city": "İstanbul",
        "district": "Tuzla", "postalCode": "34953", "latitude": 40.8526, "longitude": 29.3361, "radiusM": 250, "isActive": True,
    })
    call("POST", f"{KANTAR}/api/Weighbridges", op, body={
        "name": "Kantar 1", "serialNo": f"SIM-{stamp}", "capacityKg": 60000, "isActive": True, "stationId": station_id,
    })
    call("POST", f"{KANTAR}/api/Materials", op, body={
        "code": "AGR-0-5", "name": "Agrega 0-5 mm", "unit": "Ton", "isActive": True,
    })
    company_id = call("POST", f"{KANTAR}/api/Companys", op, body={
        "name": "Simülasyon İnşaat A.Ş.", "shortName": "Sim İnşaat", "taxNo": test_vkn(), "taxOffice": "Pendik",
        "isCustomer": True, "isSupplier": False, "isCarrier": False, "phone": "2165551111", "email": "santiye@example.com",
        "address": "Kurtköy Mah. Ankara Cd. No:10", "city": "İstanbul", "district": "Pendik", "postalCode": "34912", "isActive": True,
    })
    for name, address, district, postal, lat, lng in [
        ("Pendik Şantiye", "Kurtköy Mah. Şantiye Sk. No:3", "Pendik", "34912", 40.9122, 29.3005),
        ("Kartal Şantiye", "Cevizli Mah. Yapı Sk. No:8", "Kartal", "34865", 40.9006, 29.1873),
    ]:
        call("POST", f"{KANTAR}/api/DeliveryLocations", op, body={
            "name": name, "address": address, "city": "İstanbul", "district": district, "postalCode": postal,
            "latitude": lat, "longitude": lng, "radiusM": 200, "contactName": "Şantiye Şefi", "contactPhone": "5325550000",
            "isActive": True, "companyId": company_id,
        })
    ok("istasyon + kantar (60 t) + Agrega 0-5 (ton) + müşteri cari + 2 teslimat noktası")

    ENV_FILE.write_text(
        f"# yerel_firma_hazirla.py üretti ({time.strftime('%d.%m.%Y %H:%M')}); yalnız yerel test.\n"
        f"PK_OPERATOR_EMAIL={email}\nPK_OPERATOR_PASSWORD={password}\nPK_TENANT_ID={tenant_id}\n",
        encoding="utf-8",
    )
    ok(f"bilgiler {ENV_FILE} dosyasına yazıldı (operatör kullanıcı {jwt_sub(op)})")


if __name__ == "__main__":
    main()

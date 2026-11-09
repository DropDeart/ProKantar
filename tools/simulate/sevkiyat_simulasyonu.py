#!/usr/bin/env python3
"""
ProKantar sevkiyat simülasyonu — uçtan uca senaryo (yalnız standart kütüphane).

Mobil uygulama ve kantar cihazı olmadığı için şoför ve kantar tarafını bu betik oynar:

  1. Operatör girer, firmayı seçer; istasyon, kantar, malzeme ve teslimat noktalarını tanımlardan alır.
  2. Her çalıştırmada yeni bir şoför hesabı açar (Driver rolü + şoför profili, geçerli test TCKN'si).
  3. Plakayı platformda bulur/açar, iki teslimat duraklı sevkiyat planlar ve yayınlar.
  4. Şoför: seferi görür, istasyona gelir (konum).
  5. Kantar: giriş (boş) tartımı — malzeme göndermeden; plakadan sevkiyata bağlanmalı ve malzeme plandan gelmeli.
  6. Kantar: çıkış (dolu) tartımı; net, plandan --sapma oranı kadar farklı.
  7. Her teslimat durağına irsaliye keser (satırlar durağın yükünden).
  8. Platform şoför bildirim zincirini bekler (SignalR → push → SMS; betik hub'a bağlanmaz, --push ile cihaz token'ı kaydeder).
  9. Şoför duraklara gider, teslimleri onaylar; sevkiyat tamamlanır.
 10. Plan/tartım karşılaştırmasını ve giriş istemeyen irsaliye linkini doğrular.

Kullanım:
  set PK_OPERATOR_EMAIL=... & set PK_OPERATOR_PASSWORD=...
  python tools/simulate/sevkiyat_simulasyonu.py [--tenant <id>] [--sapma 0.05] [--tek-durak] [--tckn-yok] [--sms --telefon 5XXXXXXXXX]

Adresler ortam değişkenleriyle değişir: PK_IDENTITY_URL, PK_PLATFORM_URL, PK_KANTAR_URL, PK_TRACKING_URL
(varsayılanlar yerel docker: 8081, 8085, 8083, 8087). Yalnız yerel/test ortamında çalıştırın: kayıt açar.
"""
from __future__ import annotations

import argparse
import json
import math
import os
import random
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from datetime import datetime, timedelta, timezone

def _load_env_local() -> None:
    """yerel_firma_hazirla.py'nin yazdığı .env.local (ortam değişkenleri önceliklidir)."""
    path = os.path.join(os.path.dirname(os.path.abspath(__file__)), ".env.local")
    if os.path.exists(path):
        for line in open(path, encoding="utf-8"):
            key, sep, value = line.strip().partition("=")
            if sep and not key.startswith("#"):
                os.environ.setdefault(key, value)


_load_env_local()

IDENTITY = os.environ.get("PK_IDENTITY_URL", "http://localhost:8081")
PLATFORM = os.environ.get("PK_PLATFORM_URL", "http://localhost:8085")
KANTAR = os.environ.get("PK_KANTAR_URL", "http://localhost:8083")
TRACKING = os.environ.get("PK_TRACKING_URL", "http://localhost:8087")
CLIENT_ID = os.environ.get("PK_CLIENT_ID", "prokantar-app")
SCOPE = "api offline_access"
EMPTY_GUID = "00000000-0000-0000-0000-000000000000"


class ApiError(RuntimeError):
    def __init__(self, method: str, url: str, status: int, body: str):
        super().__init__(f"{method} {url} -> {status}: {body[:800]}")
        self.status = status
        self.body = body


# ---------------------------------------------------------------- yardımcılar

def step(title: str) -> None:
    print(f"\n== {title}")


def ok(message: str) -> None:
    print(f"   ✓ {message}")


def warn(message: str) -> None:
    print(f"   ! {message}")


def fail(message: str) -> None:
    print(f"   ✗ {message}")
    raise SystemExit(1)


def now_iso(offset_s: float = 0) -> str:
    return (datetime.now(timezone.utc) + timedelta(seconds=offset_s)).isoformat()


def call(method: str, url: str, token: str | None = None, body=None, form: dict | None = None, expect_json=True):
    headers = {"Accept": "application/json"}
    data = None
    if form is not None:
        data = urllib.parse.urlencode(form).encode()
        headers["Content-Type"] = "application/x-www-form-urlencoded"
    elif body is not None:
        data = json.dumps(body).encode("utf-8")
        headers["Content-Type"] = "application/json"
    if token:
        headers["Authorization"] = f"Bearer {token}"
    request = urllib.request.Request(url, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            raw = response.read().decode("utf-8")
            if response.status == 204 or not raw:
                return None
            return json.loads(raw) if expect_json else raw
    except urllib.error.HTTPError as e:
        raise ApiError(method, url, e.code, e.read().decode("utf-8", "replace")) from None


def items(page) -> list:
    return page.get("items", []) if isinstance(page, dict) else (page or [])


def token_password(email: str, password: str) -> dict:
    return call("POST", f"{IDENTITY}/connect/token",
                form={"grant_type": "password", "client_id": CLIENT_ID, "username": email, "password": password, "scope": SCOPE})


def token_for_tenant(refresh_token: str, tenant_id: str) -> dict:
    return call("POST", f"{IDENTITY}/connect/token",
                form={"grant_type": "refresh_token", "client_id": CLIENT_ID, "refresh_token": refresh_token, "tenant_id": tenant_id})


def jwt_sub(token: str) -> str:
    """Token'daki kullanıcı kimliği (imza doğrulanmaz; yalnız kendi token'ımızı okuruz)."""
    import base64
    payload = token.split(".")[1]
    payload += "=" * (-len(payload) % 4)
    return json.loads(base64.urlsafe_b64decode(payload))["sub"]


def test_tckn() -> str:
    """Sağlama algoritmasına uyan rastgele test TCKN'si (gerçek kişiye ait olması beklenmez)."""
    d = [random.randint(1, 9)] + [random.randint(0, 9) for _ in range(8)]
    d10 = ((d[0] + d[2] + d[4] + d[6] + d[8]) * 7 - (d[1] + d[3] + d[5] + d[7])) % 10
    d11 = (sum(d) + d10) % 10
    return "".join(map(str, d + [d10, d11]))


def kg_of(quantity: float, unit: str, material: dict) -> float | None:
    if unit == "Kg":
        return quantity
    if unit == "Ton":
        return quantity * 1000
    if unit == material.get("unit") and (material.get("kgPerUnit") or 0) > 0:
        return quantity * material["kgPerUnit"]
    return None


def interpolate(a: tuple[float, float], b: tuple[float, float], n: int) -> list[tuple[float, float]]:
    return [(a[0] + (b[0] - a[0]) * i / n, a[1] + (b[1] - a[1]) * i / n) for i in range(1, n + 1)]


def distance_m(a: tuple[float, float], b: tuple[float, float]) -> float:
    lat = math.radians((a[0] + b[0]) / 2)
    return math.hypot((b[0] - a[0]) * 111_320, (b[1] - a[1]) * 111_320 * math.cos(lat))


class Clock:
    """Konum zamanları: gerçek saatten geride başlar, her konumda ileri gider (sunucu ileri tarihi kırpar)."""

    def __init__(self, start_offset_s: float):
        self.t = datetime.now(timezone.utc) + timedelta(seconds=start_offset_s)

    def tick(self, seconds: float) -> str:
        self.t += timedelta(seconds=seconds)
        return min(self.t, datetime.now(timezone.utc)).isoformat()


# ---------------------------------------------------------------- senaryo

def main() -> None:
    parser = argparse.ArgumentParser(description="ProKantar sevkiyat simülasyonu")
    parser.add_argument("--tenant", default=os.environ.get("PK_TENANT_ID"), help="Firma (tenant) kimliği; verilmezse ilk aktif üyelik")
    parser.add_argument("--sapma", type=float, default=0.0, help="Tartılan netin plandan farkı (0.05 = %%5 fazla)")
    parser.add_argument("--sms", action="store_true",
                        help="Sahte push cihazı kaydetme; zincir SMS'e iner. Netgsm bağlıyken GERÇEK SMS gider: --telefon ile kendi numaranızı verin")
    parser.add_argument("--telefon", help="Şoförün cep telefonu (5XXXXXXXXX); --sms ile kullanın")
    parser.add_argument("--tek-durak", action="store_true", help="Tek teslimat duraklı sevkiyat (irsaliyeye gerçek net yazılır)")
    parser.add_argument("--tckn-yok", action="store_true", help="Şoför profilinde TCKN yok: irsaliye taslak kalmalı, operatör girip kesmeli")
    parser.add_argument("--bekle", type=int, default=25, help="Bildirim zinciri için en çok bekleme (sn)")
    args = parser.parse_args()

    email = os.environ.get("PK_OPERATOR_EMAIL")
    password = os.environ.get("PK_OPERATOR_PASSWORD")
    if not email or not password:
        fail("PK_OPERATOR_EMAIL ve PK_OPERATOR_PASSWORD verin (firmada TenantAdmin/Operator) ya da önce yerel_firma_hazirla.py çalıştırın.")

    # 1 -------------------------------------------------------------- operatör ve tanımlar
    step("1. Operatör girişi ve tanımlar")
    login = token_password(email, password)
    memberships = call("GET", f"{IDENTITY}/api/tenants/mine", login["access_token"])
    active = [m for m in memberships if m.get("status") == "Active" and m["tenant"].get("status") == "Active"]
    chosen = next((m for m in active if m["tenant"]["id"] == args.tenant), None) if args.tenant else (active[0] if active else None)
    if not chosen:
        fail("Aktif firma üyeliği yok (ya da --tenant bulunamadı).")
    op = token_for_tenant(login["refresh_token"], chosen["tenant"]["id"])["access_token"]
    ok(f"firma: {chosen['tenant']['name']}")

    stations = [s for s in items(call("GET", f"{KANTAR}/api/Stations?pageSize=100", op))
                if s.get("isActive", True) and s.get("latitude") is not None]
    if not stations:
        fail("Konumu tanımlı aktif istasyon yok (Tanımlar → İstasyonlar).")
    bridges = items(call("GET", f"{KANTAR}/api/Weighbridges?pageSize=100", op))
    station = next((s for s in stations if any(b["stationId"] == s["id"] and b.get("isActive", True) for b in bridges)), None)
    if not station:
        fail("Aktif kantarı olan istasyon yok.")
    bridge = next(b for b in bridges if b["stationId"] == station["id"] and b.get("isActive", True))
    materials = [m for m in items(call("GET", f"{KANTAR}/api/Materials?pageSize=100", op)) if m.get("isActive", True)]
    material = next((m for m in materials if m.get("unit") in ("Ton", "Kg")), None)
    if not material:
        fail("Birimi ton/kg olan aktif malzeme yok.")
    locations = [l for l in items(call("GET", f"{KANTAR}/api/DeliveryLocations?pageSize=100", op))
                 if l.get("isActive", True) and l.get("latitude") is not None]
    need = 1 if args.tek_durak else 2
    if len(locations) < need:
        fail(f"En az {need} teslimat noktası gerekli (konumlu).")
    origin = (float(station["latitude"]), float(station["longitude"]))
    locations.sort(key=lambda l: distance_m(origin, (float(l["latitude"]), float(l["longitude"]))))
    targets = locations[:need]
    capacity = float(bridge.get("capacityKg") or 60000)
    ok(f"istasyon: {station['name']}, kantar: {bridge['name']} ({capacity:,.0f} kg), malzeme: {material['name']} ({material['unit']})")
    ok("teslimat: " + ", ".join(l["name"] for l in targets))

    # 2 -------------------------------------------------------------- şoför
    step("2. Yeni şoför hesabı")
    stamp = int(time.time())
    driver_email = f"sim-sofor-{stamp}@example.com"
    driver_password = f"Sim!{stamp}aA"
    driver_name = f"Simülasyon Şoför {stamp % 10000}"
    if args.sms and not args.telefon:
        fail("--sms gerçek SMS gönderir; rastgele numaraya gitmesin diye --telefon 5XXXXXXXXX ile kendi numaranızı verin.")
    driver_phone = args.telefon or "55" + str(random.randint(10_000_000, 99_999_999))
    call("POST", f"{IDENTITY}/api/account/register", body={"fullName": driver_name, "email": driver_email, "password": driver_password, "role": "Driver"})
    drv = token_password(driver_email, driver_password)["access_token"]
    driver_id = call("POST", f"{PLATFORM}/api/Drivers", drv, body={"fullName": driver_name, "phone": driver_phone, "nationalId": None if args.tckn_yok else test_tckn()})
    ok(f"{driver_name} · {driver_email} · 0{driver_phone} · profil {driver_id}")
    # Varsayılan: sahte push cihazı → irsaliye bildirimi push'ta durur, rastgele numaraya SMS gitmez.
    if not args.sms:
        call("POST", f"{PLATFORM}/api/Devices", drv, body={
            "deviceIdentifier": f"sim-{stamp}", "platform": "Android", "pushToken": f"sim-push-{stamp}", "appVersion": "sim", "driverId": driver_id,
        })
        ok("sahte push token'lı cihaz kaydedildi")

    # 3 -------------------------------------------------------------- araç ve plan
    step("3. Sevkiyat planı ve yayın")
    plate = f"34SIM{random.randint(100, 999)}"
    found = items(call("GET", f"{PLATFORM}/api/vehicles?plate={plate}&pageSize=1", op))
    vehicle_id = found[0]["id"] if found else call("POST", f"{PLATFORM}/api/vehicles", op, body={"plate": plate, "ownerUserId": jwt_sub(op)})
    tare = 14_500.0
    budget = min(capacity - tare, 30_000) * 0.6  # kantar kapasitesi içinde kalsın
    unit = material["unit"]
    per_stop = []
    for i, _ in enumerate(targets):
        kg = round(budget / len(targets) * (1.2 if i == 0 else 0.8)) if len(targets) > 1 else round(budget)
        per_stop.append(round(kg / 1000, 2) if unit == "Ton" else float(kg))
    stops = [{"stopType": "Weighbridge", "stationId": station["id"]}] + [
        {"stopType": "Delivery", "deliveryLocationId": loc["id"],
         "lines": [{"materialId": material["id"], "plannedQuantity": qty, "unit": unit}]}
        for loc, qty in zip(targets, per_stop)
    ]
    planned_kg = sum(kg_of(q, unit, material) or 0 for q in per_stop)
    shipment_id = call("POST", f"{KANTAR}/api/Shipments/plan", op, body={
        "direction": "Outbound", "plannedAt": now_iso(), "stationId": station["id"], "vehicleId": vehicle_id,
        "plate": plate, "driverId": driver_id, "driverName": driver_name, "note": "Simülasyon", "stops": stops,
    })
    call("POST", f"{KANTAR}/api/Shipments/{shipment_id}/dispatch", op)
    shipment = call("GET", f"{KANTAR}/api/Shipments/{shipment_id}", op)
    ok(f"sevkiyat {shipment['shipmentNo']} · {plate} · plan {planned_kg:,.0f} kg · durum {shipment['status']}")
    drafts = call("GET", f"{KANTAR}/api/Shipments/{shipment_id}/documents", op)
    if len(drafts) != len(targets) or any(d["issued"] for d in drafts):
        fail(f"Yayında durak başına taslak irsaliye bekleniyordu: {drafts}")
    for d in drafts:
        lines = ", ".join(f"{l['name']} {l['quantity']:g} {l['unitCode']}" for l in d["lines"])
        missing = "; ".join(m["message"] for m in d["missing"])
        ok(f"taslak irsaliye → {d['stopName']}: {d.get('customerName')} · {d['plate']} · {d.get('driverName')} · {lines}"
           + (f" · eksik: {missing}" if missing else ""))

    # 4 -------------------------------------------------------------- şoför seferi görür, istasyona gelir
    step("4. Şoför: sefer ve istasyona varış")
    trip = None
    for _ in range(20):
        try:
            trip = call("GET", f"{TRACKING}/api/driver/trips/active", drv)
        except ApiError as e:
            warn(f"sefer sorgusu: {e.status}")
        if trip:
            break
        time.sleep(1)
    if not trip:
        fail("Şoförün açık seferi 20 sn içinde oluşmadı (tracking ShipmentDispatched olayını işledi mi?).")
    ok(f"sefer {trip['id']} · {len(trip.get('stops', []))} durak · {trip.get('status')}")
    clock = Clock(-30 * 60)
    start = (origin[0] - 0.01, origin[1] - 0.01)
    path = interpolate(start, origin, 6)
    res = call("POST", f"{TRACKING}/api/driver/pings", drv, body={"pings": [
        {"latitude": lat, "longitude": lng, "accuracyM": 8, "speedMps": 9, "heading": 45, "recordedAt": clock.tick(40)} for lat, lng in path
    ]})
    ok(f"{res['stored']} konum · sefer {res.get('tripStatus')}")

    # 5 -------------------------------------------------------------- kantar: giriş tartımı (malzemesiz)
    step("5. Kantar: giriş (boş) tartımı — malzeme gönderilmez")
    ticket = f"SIM{stamp % 1_000_000}"
    weighing_id = call("POST", f"{KANTAR}/api/Weighings", op, body={
        "ticketNo": ticket, "direction": "Outbound", "plate": plate, "firstWeightKg": tare, "firstWeighedAt": now_iso(),
        "firstIsManual": False, "stationId": station["id"], "weighbridgeId": bridge["id"], "materialId": EMPTY_GUID, "vehicleId": vehicle_id,
    })
    weighing = call("GET", f"{KANTAR}/api/Weighings/{weighing_id}", op)
    if weighing.get("shipmentId") != shipment_id:
        fail(f"Tartım sevkiyata bağlanmadı (shipmentId={weighing.get('shipmentId')}).")
    if weighing.get("materialId") != material["id"]:
        fail("Malzeme plandan gelmedi.")
    ok(f"fiş {ticket} plakadan {shipment['shipmentNo']}'e bağlandı; malzeme ({material['name']}), şoför ({weighing.get('driverName')}) plandan geldi")

    # 6 -------------------------------------------------------------- kantar: çıkış tartımı
    step(f"6. Kantar: çıkış (dolu) tartımı · sapma %{args.sapma * 100:.1f}")
    gross = tare + round(planned_kg * (1 + args.sapma))
    call("POST", f"{KANTAR}/api/Weighings/{weighing_id}/complete", op, body={"secondWeightKg": gross, "secondIsManual": False})
    weighing = call("GET", f"{KANTAR}/api/Weighings/{weighing_id}", op)
    ok(f"net {weighing['netKg']:,.0f} kg (brüt {weighing['grossKg']:,.0f}, dara {weighing['tareKg']:,.0f})")

    # 7 -------------------------------------------------------------- irsaliyeler
    step("7. İrsaliyeler: çıkış tartımında taslaklar kesinleşmeli")
    waybills = []
    for d in call("GET", f"{KANTAR}/api/Shipments/{shipment_id}/documents", op):
        if d["issued"]:
            waybills.append({"waybillNo": d["waybillNo"]})
            ok(f"{d['waybillNo']} → {d['stopName']}: " + ", ".join(f"{l['name']} {l['quantity']:g} {l['unitCode']}" for l in d["lines"]))
        else:
            warn(f"{d['stopName']}: taslak kaldı — " + "; ".join(m["message"] for m in d["missing"]))
    for loc in [] if len(waybills) == len(targets) else targets:
        preview = call("GET", f"{KANTAR}/api/Weighings/{weighing_id}/despatch/preview?deliveryLocationId={loc['id']}", op)
        for w in preview.get("warnings", []):
            warn(f"uyarı: {w}")
        # Şoför TCKN'si operatörce girilir (aşağıda); diğer eksikler tanımlardan düzeltilmeli.
        blocking = [e for e in preview.get("errors", []) if not (preview.get("driverNationalIdRequired") and "NationalId" in e["field"])]
        if blocking:
            fail(f"{loc['name']}: kesilemez — " + "; ".join(f"{e['field']}: {e['message']}" for e in blocking))
        lines = ", ".join(f"{l['name']} {l['quantity']:g} {l['unitCode']}" for l in preview["document"]["lines"])
        body = {"deliveryLocationId": loc["id"]}
        if preview.get("driverNationalIdRequired"):
            body["driverNationalId"] = test_tckn()
            ok(f"{loc['name']}: operatör şoför TCKN'sini girdi")
        view = call("POST", f"{KANTAR}/api/Weighings/{weighing_id}/despatch", op, body=body)
        waybills.append(view)
        ok(f"{view['waybillNo']} → {loc['name']}: {lines}")

    # 8 -------------------------------------------------------------- bildirim zinciri
    step("8. Şoför bildirim zinciri (platform)")
    deadline = time.time() + args.bekle
    notes = []
    while time.time() < deadline:
        notes = call("GET", f"{PLATFORM}/api/DriverNotifications?shipmentId={shipment_id}", op) or []
        if len(notes) == len(waybills) and all(n["status"] != "Pending" for n in notes):
            break
        time.sleep(1)
    if len(notes) != len(waybills):
        fail(f"{len(waybills)} irsaliyeden {len(notes)} bildirim oluştu (platform kantar/WaybillIssued olayını aldı mı?).")
    for n in notes:
        chain = " → ".join(f"{a['channel']}:{a['result']}" for a in n["attempts"])
        (ok if n["status"] == "Delivered" else warn)(f"{n['waybillNo']}: {n['status']} via {n.get('deliveredVia')} [{chain}]")
    inbox = call("GET", f"{PLATFORM}/api/me/notifications", drv) or []
    if inbox:
        call("POST", f"{PLATFORM}/api/me/notifications/{inbox[0]['id']}/ack", drv)
        ok(f"şoför gelen kutusunda {len(inbox)} irsaliye; ilki uygulamadan onaylandı")

    # 9 -------------------------------------------------------------- yol ve teslim
    step("9. Şoför: yol, varış ve teslim onayı")
    trip = call("GET", f"{TRACKING}/api/driver/trips/active", drv)
    position = origin
    for loc in targets:
        target = (float(loc["latitude"]), float(loc["longitude"]))
        legs = max(4, min(40, int(distance_m(position, target) / 1500)))
        pings = [{"latitude": lat, "longitude": lng, "accuracyM": 10, "speedMps": 14, "heading": 90, "recordedAt": clock.tick(60)}
                 for lat, lng in interpolate(position, target, legs)]
        for i in range(0, len(pings), 100):
            call("POST", f"{TRACKING}/api/driver/pings", drv, body={"pings": pings[i:i + 100]})
        position = target
        trip = call("GET", f"{TRACKING}/api/driver/trips/active", drv)
        stop = next((s for s in (trip or {}).get("stops", []) if s.get("stopType") == "Delivery" and s.get("confirmedAt") is None
                     and abs(float(s["latitude"]) - target[0]) < 1e-5 and abs(float(s["longitude"]) - target[1]) < 1e-5), None)
        if not stop:
            warn(f"{loc['name']}: sefer durağı bulunamadı (sefer kapanmış olabilir)")
            continue
        call("POST", f"{TRACKING}/api/driver/trips/{trip['id']}/stops/{stop['id']}/confirm", drv)
        ok(f"{loc['name']}: vardı ({stop.get('status')}) ve teslim onaylandı")

    # 10 ------------------------------------------------------------- sonuç
    step("10. Sonuç")
    for _ in range(15):
        shipment = call("GET", f"{KANTAR}/api/Shipments/{shipment_id}", op)
        if shipment["status"] == "Completed":
            break
        time.sleep(1)
    (ok if shipment["status"] == "Completed" else warn)(f"sevkiyat durumu: {shipment['status']}")
    progress = call("GET", f"{KANTAR}/api/Shipments/{shipment_id}/progress", op)
    ok(f"plan {progress['plannedKg'] or 0:,.0f} kg · tartılan {progress['weighedNetKg'] or 0:,.0f} kg · "
       f"fark %{progress['deviationPercent'] if progress['deviationPercent'] is not None else '-'}"
       + (" (UYARI)" if progress["deviationWarning"] else ""))
    for s in progress["stops"]:
        if s["stopType"] == "Delivery":
            ok(f"  {s['sequence']}. {s['name']}: plan {s['plannedKg'] or 0:,.0f} kg · irsaliye {s.get('waybillNo') or '—'}")
    link = notes[0]["linkUrl"] if notes else None
    if link:
        token = link.rsplit("/", 1)[-1]
        public = call("GET", f"{KANTAR}/api/public/waybills/{urllib.parse.quote(token)}")
        ok(f"giriş istemeyen link açıldı: {public['waybillNo']} · {len(public['lines'])} satır · {link}")
        try:
            call("GET", f"{KANTAR}/api/public/waybills/bozuk{urllib.parse.quote(token[5:])}")
            fail("Bozuk token kabul edildi.")
        except ApiError as e:
            ok(f"bozuk token reddedildi ({e.status})")
    print(f"\nPanel: http://localhost:3000/panel/sevkiyatlar/{shipment_id}")


if __name__ == "__main__":
    try:
        main()
    except ApiError as error:
        print(f"\nAPI hatası: {error}")
        sys.exit(2)

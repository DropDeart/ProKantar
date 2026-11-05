# ProKantar ER modeli -> draw.io + BaseForge spec YAML üretici (tek kaynak).
# Hedef: BaseForge.CodeGen 0.6.3-beta (enum, nullable relation, filterable, access/ownerField,
#        serverManaged, searchFields).
# Kullanım: python gen_prokantar.py <çıktı-klasörü>
import os, sys
from xml.sax.saxutils import escape

OUT = sys.argv[1]

def F(name, typ, null=False, ml=None, default=None, note=None, ref=None, values=None, sm=False):
    """sm = serverManaged: CRUD Create/Update'e girmez, yalnız elle yazılan komut değiştirir."""
    return dict(name=name, type=typ, null=null, ml=ml, default=default, note=note, ref=ref, values=values, sm=sm)

def R(name, kind, target, null=False):
    return (name, kind, target, null)

# Roller (identity/auth.yaml'daki roles ile aynı olmalı)
SUPER = "SuperAdmin"; TADMIN = "TenantAdmin"; OP = "Operator"; DRV = "Driver"
STAFF = [TADMIN, OP]

# Kiracı servisinde tanım (lookup) verileri için ortak erişim
LOOKUP_ACCESS = {"list": STAFF, "getById": STAFF, "create": [TADMIN], "update": [TADMIN], "delete": [TADMIN]}

# ---------------- PLATFORM (global, generate) ----------------
platform = dict(
    key="platform", title="platform — global (MultiTenant: false) · BaseForge generate",
    color="#dae8fc", stroke="#6c8ebf", multi=False, mode="generate", db="prokantar_platform_db", ports=(8085, 8086, 5434),
    defaultAccess="authenticated",
    entities=[
        dict(name="Carrier", owner="ownerUserId",
             access={"list": "authenticated", "getById": "authenticated", "create": [DRV, TADMIN, OP],
                     "update": ["owner"], "delete": [SUPER]},
             filter=["TaxNo"], search=["Name", "TaxNo"],
             fields=[
            F("ownerUserId", "guid", note="ownerField: kaydı açan kullanıcı (sub)"),
            F("name", "string", ml=200),
            F("taxNo", "string", True, 11, note="VKN/TCKN"),
            F("taxOffice", "string", True, 100),
            F("phone", "string", True, 20),
            F("address", "text", True),
        ], rel=[], ext=[]),
        dict(name="Driver", owner="userId",
             access={"list": ["owner"], "getById": ["owner"], "create": [DRV],
                     "update": ["owner"], "delete": [SUPER]},
             filter=["CarrierId", "Phone"], search=["FullName", "Phone"],
             fields=[
            F("userId", "guid", note="ownerField: identity/User (sub) - şoförün hesabı"),
            F("fullName", "string", ml=150),
            F("phone", "string", ml=20),
            F("nationalId", "string", True, 11, note="TCKN"),
            F("licenseNo", "string", True, 30),
            F("kvkkConsentAt", "datetime", True, note="açık rıza zamanı"),
            F("locationConsent", "bool", default="false"),
            F("isActive", "bool", default="true"),
        ], rel=[R("carrier", "many-to-one", "Carrier", True)], ext=[]),
        dict(name="Vehicle", owner="ownerUserId",
             access={"list": "authenticated", "getById": "authenticated", "create": [DRV, OP, TADMIN],
                     "update": ["owner"], "delete": [SUPER]},
             filter=["Plate", "CarrierId", "IsClaimed"], search=["Plate", "Brand", "Model"],
             fields=[
            F("ownerUserId", "guid", note="ownerField; sahiplenme (claim) = elle yazılacak uç"),
            F("plate", "string", ml=15, note="UNIQUE elle; normalize (boşluksuz, büyük harf)"),
            F("vehicleTypeCode", "string", True, 30),
            F("brand", "string", True, 50),
            F("model", "string", True, 50),
            F("isClaimed", "bool", default="false", sm=True, note="operatör açtıysa false; şoför sahiplenince true (claim ucu)"),
        ], rel=[R("carrier", "many-to-one", "Carrier", True)], ext=[]),
        dict(name="Trailer", owner="ownerUserId",
             access={"list": "authenticated", "getById": "authenticated", "create": [DRV, OP, TADMIN],
                     "update": ["owner"], "delete": [SUPER]},
             filter=["Plate", "CarrierId"],
             fields=[
            F("ownerUserId", "guid", note="ownerField"),
            F("plate", "string", ml=15, note="UNIQUE elle"),
        ], rel=[R("carrier", "many-to-one", "Carrier", True)], ext=[]),
        dict(name="DriverVehicle", owner="userId",
             access={"list": ["owner"], "getById": ["owner"], "create": [DRV],
                     "update": ["owner"], "delete": ["owner"]},
             filter=["DriverId", "VehicleId"],
             fields=[
            F("userId", "guid", note="ownerField: şoförün kullanıcısı"),
            F("startedAt", "datetime"),
            F("endedAt", "datetime", True, note="null = aktif eşleşme"),
        ], rel=[R("driver", "many-to-one", "Driver"), R("vehicle", "many-to-one", "Vehicle"),
                R("trailer", "many-to-one", "Trailer", True)], ext=[]),
        dict(name="Device", owner="userId",
             access={"list": ["owner"], "getById": ["owner"], "create": [DRV],
                     "update": ["owner"], "delete": ["owner"]},
             filter=["DriverId"],
             fields=[
            F("userId", "guid", note="ownerField"),
            F("deviceIdentifier", "string", ml=100),
            F("platform", "enum", values=["Android", "Ios"]),
            F("pushToken", "string", True, 500),
            F("appVersion", "string", True, 20),
            F("lastSeenAt", "datetime", True, sm=True, note="tracking ping'i günceller"),
        ], rel=[R("driver", "many-to-one", "Driver")], ext=[]),
    ])

# ---------------- KANTAR (tenant, generate) ----------------
kantar = dict(
    key="kantar", title="kantar — kiracı (MultiTenant: true) · BaseForge generate",
    color="#d5e8d4", stroke="#82b366", multi=True, mode="generate", db="prokantar_kantar_db", ports=(8083, 8084, 5433),
    defaultAccess=STAFF,
    entities=[
        dict(name="Station", access=LOOKUP_ACCESS, filter=["IsActive"], fields=[
            F("name", "string", ml=150),
            F("code", "string", ml=30),
            F("address", "text", True),
            F("city", "string", True, 50, note="il"),
            F("district", "string", True, 50, note="ilçe"),
            F("phone", "string", True, 20),
            F("latitude", "decimal", True, note="WGS84; durak/varış tespiti için"),
            F("longitude", "decimal", True),
            F("radiusM", "int", default="300", note="varış yarıçapı (m); saha poligonu tracking/Geofence'te"),
            F("isActive", "bool", default="true"),
        ], rel=[], ext=[]),
        dict(name="Weighbridge", access=LOOKUP_ACCESS, filter=["StationId", "IsActive"], fields=[
            F("name", "string", ml=100),
            F("serialNo", "string", True, 50),
            F("capacityKg", "decimal"),
            F("connectionType", "enum", True, values=["Serial", "Tcp"]),
            F("connectionInfo", "json", True, note="port, baud, ip..."),
            F("calibrationValidUntil", "date", True),
            F("isActive", "bool", default="true"),
        ], rel=[R("station", "many-to-one", "Station")], ext=[]),
        dict(name="Machine", access=LOOKUP_ACCESS, filter=["StationId", "IsActive"], fields=[
            F("name", "string", ml=100),
            F("machineType", "string", ml=50),
            F("plateOrSerial", "string", True, 50),
            F("isActive", "bool", default="true"),
        ], rel=[R("station", "many-to-one", "Station")], ext=[]),
        dict(name="Company", access=LOOKUP_ACCESS,
             filter=["IsCustomer", "IsSupplier", "IsCarrier", "IsActive", "TaxNo"],
             search=["Name", "ShortName", "TaxNo"], fields=[
            F("name", "string", ml=200),
            F("shortName", "string", True, 50),
            F("taxNo", "string", True, 11),
            F("taxOffice", "string", True, 100),
            F("isCustomer", "bool", default="false"),
            F("isSupplier", "bool", default="false"),
            F("isCarrier", "bool", default="false"),
            F("phone", "string", True, 20),
            F("email", "string", True, 150),
            F("address", "text", True),
            F("linkedTenantId", "guid", True, note="firma rehberinden: bağlı ProKantar şirketi (identity/Tenant)"),
            F("isActive", "bool", default="true"),
        ], rel=[], ext=[]),
        dict(name="DeliveryLocation",
             access={"list": STAFF, "getById": STAFF, "create": STAFF, "update": STAFF, "delete": [TADMIN]},
             filter=["CompanyId", "IsActive"], fields=[
            F("name", "string", ml=150, note="şantiye/depo adı"),
            F("address", "text", True, note="açık adres; Custom guard zorunlu tutar"),
            F("city", "string", True, 50, note="il"),
            F("district", "string", True, 50, note="ilçe"),
            F("sourceTenantId", "guid", True, note="karşı firmanın istasyonundan alındıysa o şirket"),
            F("sourceStationId", "guid", True, note="o şirketteki istasyon"),
            F("latitude", "decimal", note="WGS84"),
            F("longitude", "decimal"),
            F("radiusM", "int", default="150", note="varış yarıçapı (m)"),
            F("contactName", "string", True, 100),
            F("contactPhone", "string", True, 20),
            F("isActive", "bool", default="true"),
        ], rel=[R("company", "many-to-one", "Company")], ext=[]),
        dict(name="ErpCompanyLink", access=LOOKUP_ACCESS, filter=["CompanyId", "ErpSystem", "ErpCode"], fields=[
            F("erpSystem", "string", ml=30, note="Netsis/Logo/Mikro..."),
            F("erpCode", "string", ml=50),
            F("isDefault", "bool", default="true"),
        ], rel=[R("company", "many-to-one", "Company")], ext=[]),
        dict(name="VehicleType", access=LOOKUP_ACCESS, filter=["Code"], fields=[
            F("code", "string", ml=30),
            F("name", "string", ml=50),
            F("maxGrossKg", "decimal", True),
        ], rel=[], ext=[]),
        dict(name="Material", access=LOOKUP_ACCESS, filter=["Code", "Unit", "IsActive"], fields=[
            F("code", "string", ml=50, note="stok kodu, kiracı içinde UNIQUE elle"),
            F("name", "string", ml=200),
            F("unit", "enum", values=["Kg", "Ton", "M3", "Piece"]),
            F("kgPerUnit", "decimal", True, note="ton=1000, m3=yoğunluk"),
            F("erpStockCode", "string", True, 50),
            F("isActive", "bool", default="true"),
        ], rel=[], ext=[]),
        dict(name="TenantVehicle",
             access={"list": STAFF, "getById": STAFF, "create": STAFF, "update": STAFF, "delete": [TADMIN]},
             filter=["VehicleId", "VehicleTypeId", "IsBlacklisted"], fields=[
            F("storedTareKg", "decimal", True),
            F("storedTareAt", "datetime", True, sm=True, note="dara kaydedilince sunucu yazar"),
            F("isBlacklisted", "bool", default="false"),
            F("blacklistReason", "string", True, 250),
            F("note", "text", True),
        ], rel=[R("vehicleType", "many-to-one", "VehicleType", True)],
           ext=[("vehicle", "platform/Vehicle", "VehicleId")]),
        dict(name="Weighing", publishes=["created", "updated"],
             access={"list": STAFF, "getById": STAFF, "create": STAFF, "update": STAFF, "delete": [TADMIN]},
             filter=["StationId", "WeighbridgeId", "CompanyId", "MaterialId", "VehicleId", "DriverId",
                     "Status", "Direction", "Plate", "TicketNo"],
             search=["TicketNo", "Plate", "TrailerPlate", "DriverName"],
             fields=[
            F("ticketNo", "string", ml=30, note="kiracı+istasyon içinde UNIQUE elle"),
            F("direction", "enum", values=["Inbound", "Outbound"], note="Giriş/Alış · Çıkış/Satış"),
            F("status", "enum", values=["Open", "Completed", "Cancelled"], default="Open", sm=True, note="CompleteWeighing / iptal komutu"),
            F("plate", "string", ml=15, note="snapshot"),
            F("trailerId", "guid", True, note="platform/Trailer (yalnız ID)"),
            F("trailerPlate", "string", True, 15, note="snapshot"),
            F("driverId", "guid", True, note="platform/Driver (yalnız ID) - kayıtsız şoför null"),
            F("driverName", "string", True, 150, note="snapshot"),
            F("driverPhone", "string", True, 20, note="snapshot"),
            F("firstWeightKg", "decimal"),
            F("firstWeighedAt", "datetime"),
            F("firstIsManual", "bool", default="false"),
            F("secondWeighbridgeId", "guid", True, note="FK elle (aynı hedefe 2. ilişki)", ref="Weighbridge", sm=True),
            F("secondWeightKg", "decimal", True, sm=True),
            F("secondWeighedAt", "datetime", True, sm=True),
            F("secondIsManual", "bool", default="false", sm=True),
            F("grossKg", "decimal", True, sm=True, note="sunucu hesaplar"),
            F("tareKg", "decimal", True, sm=True, note="sunucu hesaplar"),
            F("netKg", "decimal", True, sm=True, note="sunucu hesaplar"),
            F("tareSource", "enum", True, values=["Measured", "Stored"], sm=True),
            F("cancelReason", "string", True, 250, sm=True),
            F("note", "text", True),
        ], rel=[R("station", "many-to-one", "Station"), R("weighbridge", "many-to-one", "Weighbridge"),
                R("company", "many-to-one", "Company", True), R("material", "many-to-one", "Material"),
                R("shipment", "many-to-one", "Shipment", True)],
           ext=[("vehicle", "platform/Vehicle", "VehicleId")]),
        dict(name="WeighingPhoto", appendOnly=True,
             access={"list": STAFF, "getById": STAFF, "create": STAFF},
             filter=["WeighingId", "Stage"], fields=[
            F("stage", "enum", values=["First", "Second"]),
            F("cameraName", "string", True, 50),
            F("url", "string", ml=500),
            F("takenAt", "datetime"),
        ], rel=[R("weighing", "many-to-one", "Weighing")], ext=[]),
        dict(name="WeighingAuditLog", appendOnly=True,
             access={"list": [TADMIN], "getById": [TADMIN], "create": STAFF},
             filter=["WeighingId", "Action"], fields=[
            F("action", "enum", values=["ManualWeight", "TareChange", "Cancel", "Edit"]),
            F("fieldName", "string", True, 50),
            F("oldValue", "string", True, 250),
            F("newValue", "string", True, 250),
            F("reason", "string", True, 250),
        ], rel=[R("weighing", "many-to-one", "Weighing")], ext=[]),
        dict(name="Waybill", publishes=["created"],
             access={"list": STAFF, "getById": STAFF, "create": STAFF, "update": STAFF, "delete": [TADMIN]},
             filter=["WeighingId", "CompanyId", "DeliveryLocationId", "WaybillNo", "WaybillType",
                     "ErpSyncStatus", "EDespatchStatus"], fields=[
            F("waybillNo", "string", ml=30),
            F("waybillType", "enum", values=["Paper", "EDespatch"]),
            F("issuedAt", "datetime"),
            F("quantityKg", "decimal", True, note="çoklu teslimde bu irsaliyenin payı; null = tartımın tamamı"),
            F("deliveryAddress", "text", True, note="snapshot"),
            F("eDespatchUuid", "guid", True, sm=True),
            F("eDespatchStatus", "enum", True, values=["Draft", "Sent", "Accepted", "Rejected"], sm=True),
            F("erpSyncStatus", "enum", values=["Pending", "Sent", "Failed"], default="Pending", sm=True),
            F("erpDocNo", "string", True, 50, sm=True),
            F("erpSyncedAt", "datetime", True, sm=True),
            F("erpError", "text", True, sm=True),
        ], rel=[R("weighing", "many-to-one", "Weighing"), R("company", "many-to-one", "Company", True),
                R("deliveryLocation", "many-to-one", "DeliveryLocation", True)], ext=[]),
        dict(name="Shipment",
             access={"list": STAFF, "getById": STAFF, "create": STAFF, "update": STAFF, "delete": [TADMIN]},
             filter=["StationId", "VehicleId", "DriverId", "Direction", "Status", "Plate"],
             search=["ShipmentNo", "Plate", "DriverName"], fields=[
            F("shipmentNo", "string", ml=30),
            F("direction", "enum", values=["Inbound", "Outbound"], note="Inbound = gelen araç randevusu"),
            F("status", "enum", values=["Draft", "Dispatched", "InProgress", "Completed", "Cancelled"], default="Draft", sm=True,
              note="Dispatched'a yalnız elle yazılan DispatchShipment komutu geçirir"),
            F("plannedAt", "datetime"),
            F("plate", "string", ml=15, note="snapshot"),
            F("driverId", "guid", True, note="platform/Driver (yalnız ID)"),
            F("driverName", "string", True, 150, note="snapshot"),
            F("dispatchedAt", "datetime", True, sm=True),
            F("cancelReason", "string", True, 250, sm=True),
            F("note", "text", True),
        ], rel=[R("station", "many-to-one", "Station")],
           ext=[("vehicle", "platform/Vehicle", "VehicleId")]),
        dict(name="ShipmentStop",
             access={"list": STAFF, "getById": STAFF, "create": STAFF, "update": STAFF, "delete": STAFF},
             filter=["ShipmentId", "StopType", "WaybillId"], fields=[
            F("sequence", "int", note="1'den başlayan sıra"),
            F("stopType", "enum", values=["Pickup", "Weighbridge", "Delivery"]),
            F("name", "string", ml=150, note="snapshot: lokasyon/istasyon adı"),
            F("latitude", "decimal", note="snapshot"),
            F("longitude", "decimal", note="snapshot"),
            F("radiusM", "int", default="150"),
            F("plannedAt", "datetime", True),
            F("note", "text", True),
        ], rel=[R("shipment", "many-to-one", "Shipment"),
                R("deliveryLocation", "many-to-one", "DeliveryLocation", True),
                R("station", "many-to-one", "Station", True),
                R("waybill", "many-to-one", "Waybill", True)], ext=[]),
    ])

# ---------------- IDENTITY (elle) ----------------
identity = dict(
    key="identity", title="identity — BaseForge Identity + ELLE eklenecek (Tenant)",
    color="#ffe6cc", stroke="#d79b00", multi=False, mode="manual",
    entities=[
        dict(name="User", note="mevcut (BaseForge Identity)", fields=[
            F("email", "string"), F("userName", "string"), F("phoneNumber", "string", True),
        ], rel=[], ext=[]),
        dict(name="Role", note="auth.yaml roles: SuperAdmin, TenantAdmin, Operator, Driver", fields=[
            F("name", "string"),
        ], rel=[], ext=[]),
        dict(name="Tenant", note="ELLE", fields=[
            F("name", "string", ml=200), F("taxNo", "string", True, 11), F("slug", "string", ml=50),
            F("plan", "enum", values=["Trial", "Standard", "Pro"]),
            F("status", "enum", values=["Active", "Suspended"]), F("subscriptionEndsAt", "datetime", True),
        ], rel=[], ext=[]),
        dict(name="UserTenant", note="ELLE - token'a tenant_id buradan", fields=[
            F("isDefault", "bool"),
        ], rel=[R("user", "many-to-one", "User"), R("tenant", "many-to-one", "Tenant")], ext=[]),
    ])

# ---------------- TRACKING (elle, PostGIS, BaseForge mimarisi) ----------------
tracking = dict(
    key="tracking", title="tracking — ELLE, PostGIS (8087/8088/5435)",
    color="#e1d5e7", stroke="#9673a6", multi=False, mode="manual",
    entities=[
        dict(name="Trip", note="Id = kantar Shipment Id", fields=[
            F("tenantId", "guid"), F("shipmentNo", "string"),
            F("direction", "enum", values=["Inbound", "Outbound"]),
            F("status", "enum", values=["Planned", "Active", "Completed", "Cancelled"]),
            F("stationId", "guid"), F("vehicleId", "guid"), F("plate", "string"),
            F("driverId", "guid"), F("driverUserId", "guid", note="token sub; konumu eşler"),
            F("plannedAt", "datetime"), F("startedAt", "datetime", True), F("endedAt", "datetime", True),
            F("route", "geometry(LineString,4326)", True, note="kapanınca konumlardan"),
            F("distanceM", "double", True),
        ], rel=[], ext=[]),
        dict(name="TripStop", note="Id = kantar ShipmentStop Id", fields=[
            F("tripId", "guid", ref="Trip"), F("sequence", "int"),
            F("stopType", "enum", values=["Pickup", "Weighbridge", "Delivery"]),
            F("name", "string"), F("location", "geometry(Point,4326)"), F("radiusM", "int"),
            F("waybillId", "guid", True), F("status", "enum", values=["Pending", "Arrived", "Departed", "Skipped"]),
            F("arrivedAt", "datetime", True), F("departedAt", "datetime", True),
            F("confirmedAt", "datetime", True, note="şoför 'Teslim ettim' / tartım tamamlandı"),
        ], rel=[], ext=[]),
        dict(name="LocationPing", note="EF dışı · bigserial · aylık partition(recorded_at) · 6 ay", fields=[
            F("tripId", "guid", ref="Trip"), F("driverUserId", "guid"),
            F("location", "geometry(Point,4326)"), F("accuracyM", "float", True),
            F("speedMps", "float", True), F("heading", "float", True),
            F("recordedAt", "datetime"), F("receivedAt", "datetime"),
        ], rel=[], ext=[]),
    ])

SERVICES = [identity, platform, kantar, tracking]

CROSS = [
    ("platform", "Driver", "identity", "User"),
    ("kantar", "TenantVehicle", "platform", "Vehicle"),
    ("kantar", "Weighing", "platform", "Vehicle"),
    ("kantar", "Weighing", "platform", "Driver"),
    ("kantar", "Weighing", "platform", "Trailer"),
    ("tracking", "Trip", "kantar", "Shipment"),
    ("tracking", "TripStop", "kantar", "ShipmentStop"),
    ("tracking", "Trip", "platform", "Driver"),
    ("kantar", "Station", "identity", "Tenant"),
]

# ======================= draw.io =======================
BOX_W, LINE_H, HEAD_H = 440, 17, 26
cells, ids = [], {}
nid = [2]
def new_id():
    nid[0] += 1
    return f"c{nid[0]}"

def disp_type(f):
    t = f["type"]
    if f["values"]: t = "enum{" + "|".join(f["values"]) + "}"
    if f["ml"]: t += f"({f['ml']})"
    if f["null"]: t += "?"
    return t

def acc_str(v):
    return v if isinstance(v, str) else "[" + ",".join(v) + "]"

def entity_lines(svc, e):
    lines = ["<b>Id</b> : uuid PK" if e["name"] != "LocationPing" else "<b>Id</b> : bigint PK"]
    for r in e["rel"]:
        if r[1] in ("many-to-one", "one-to-one"):
            q = "?" if r[3] else ""
            lines.append(f"<font color='#b85450'>{r[0][0].upper()+r[0][1:]}Id : uuid{q} FK → {r[2]}</font>")
    for x in e["ext"]:
        lines.append(f"<font color='#0050ef'>{x[2]} : uuid ↗ {x[1]}</font>")
    for f in e["fields"]:
        s = f"{f['name']} : {disp_type(f)}"
        if f["default"] is not None: s += f" = {f['default']}"
        col = "#666666" if f["null"] else "#000000"
        if "geometry" in f["type"]: col = "#9673a6"
        if e.get("owner") == f["name"]: s = "🔑 " + s
        if f["sm"]: s = "⚙ " + s
        s = f"<font color='{col}'>{escape(s)}</font>"
        if f["note"]: s += f" <i><font color='#999999'>// {escape(f['note'])}</font></i>"
        lines.append(s)
    if svc["mode"] == "generate":
        tail = "+ CreatedAt, UpdatedAt, CreatedBy, IsDeleted, DeletedAt"
        if svc["multi"]: tail += ", <b>TenantId</b>"
        lines.append(f"<font color='#999999'><i>{tail}</i></font>")
    if e.get("filter"):
        lines.append(f"<font color='#d79b00'>⛃ filter: {escape(', '.join(e['filter']))}</font>")
    if e.get("access"):
        a = " ".join(f"{k}:{acc_str(v)}" for k, v in e["access"].items())
        lines.append(f"<font color='#6d8764'>🔒 {escape(a)}</font>")
    return lines

x0 = 20
COLS = {"identity": 1, "platform": 2, "kantar": 4, "tracking": 2}
for svc in SERVICES:
    cols = COLS[svc["key"]]
    col_h = [0] * cols
    placements = []
    for e in svc["entities"]:
        lines = entity_lines(svc, e)
        h = HEAD_H + LINE_H * len(lines) + 8
        c = col_h.index(min(col_h))
        placements.append((e, lines, c, col_h[c], h))
        col_h[c] += h + 30
    W = cols * (BOX_W + 30) + 30
    H = max(col_h) + 50
    gid = new_id()
    cells.append(f'<mxCell id="{gid}" value="{escape(svc["title"])}" style="swimlane;startSize=30;fillColor={svc["color"]};strokeColor={svc["stroke"]};fontStyle=1;fontSize=13;rounded=1;arcSize=2;swimlaneFillColor=#fafafa;" vertex="1" parent="1"><mxGeometry x="{x0}" y="20" width="{W}" height="{H}" as="geometry"/></mxCell>')
    for e, lines, c, y, h in placements:
        eid = new_id()
        ids[(svc["key"], e["name"])] = eid
        title = e["name"]
        if e.get("appendOnly"): title += " «appendOnly»"
        if e.get("note"): title += f" — {e['note']}"
        body = "<br>".join(lines)
        style = (f"swimlane;fontStyle=1;startSize={HEAD_H};fillColor={svc['color']};strokeColor={svc['stroke']};"
                 "html=1;whiteSpace=wrap;align=center;childLayout=stackLayout;horizontal=1;resizeParent=1;collapsible=0;")
        if svc["mode"] == "manual": style += "dashed=1;"
        cells.append(f'<mxCell id="{eid}" value="{escape(title)}" style="{style}" vertex="1" parent="{gid}"><mxGeometry x="{30 + c*(BOX_W+30)}" y="{40 + y}" width="{BOX_W}" height="{h}" as="geometry"/></mxCell>')
        cells.append(f'<mxCell id="{new_id()}" value="{escape(body)}" style="text;html=1;align=left;verticalAlign=top;spacingLeft=6;fontSize=10;fontFamily=Consolas;whiteSpace=nowrap;" vertex="1" parent="{eid}"><mxGeometry y="{HEAD_H}" width="{BOX_W}" height="{h-HEAD_H}" as="geometry"/></mxCell>')
    x0 += W + 60

def edge(src, dst, style, label=""):
    cells.append(f'<mxCell id="{new_id()}" value="{escape(label)}" style="{style}" edge="1" parent="1" source="{src}" target="{dst}"><mxGeometry relative="1" as="geometry"/></mxCell>')

ER = "edgeStyle=orthogonalEdgeStyle;rounded=1;html=1;fontSize=9;"
for svc in SERVICES:
    for e in svc["entities"]:
        s = ids[(svc["key"], e["name"])]
        for r in e["rel"]:
            end = "ERzeroToOne" if r[3] else "ERmandOne"
            dash = "dashed=1;" if r[3] else ""
            start = "ERzeroToOne" if r[1] == "one-to-one" else "ERmany"
            edge(s, ids[(svc["key"], r[2])], ER + f"endArrow={end};startArrow={start};strokeColor=#b85450;{dash}")
        for f in e["fields"]:
            if f["ref"]:
                edge(s, ids[(svc["key"], f["ref"])], ER + "endArrow=ERzeroToOne;startArrow=ERmany;dashed=1;dashPattern=2 3;strokeColor=#b85450;", "elle FK")
for a, ae, b, be in CROSS:
    edge(ids[(a, ae)], ids[(b, be)], "curved=1;html=1;dashed=1;dashPattern=8 4;strokeColor=#0050ef;endArrow=open;fontSize=9;opacity=60;", "ID")

lg = ("<b>Lejant</b><br><font color='#b85450'>━ zorunlu FK · ┅ opsiyonel FK (relation nullable) · ⋯ elle FK</font><br>"
      "<font color='#0050ef'>┅ servisler arası yalnız ID (externalRef / guid)</font><br>"
      "<font color='#666666'>gri alan = nullable (?)</font> · <font color='#9673a6'>mor = PostGIS</font> · 🔑 ownerField · ⚙ serverManaged<br>"
      "<font color='#d79b00'>⛃ filterable</font> · <font color='#6d8764'>🔒 access (roller / owner)</font><br>"
      "Kesik kenarlı kutu = BaseForge dışında ELLE yazılacak")
cells.append(f'<mxCell id="{new_id()}" value="{escape(lg)}" style="text;html=1;align=left;verticalAlign=top;spacing=8;fillColor=#ffffff;strokeColor=#999999;fontSize=11;" vertex="1" parent="1"><mxGeometry x="20" y="-140" width="520" height="130" as="geometry"/></mxCell>')

xml = ('<mxfile host="ProKantar-gen"><diagram name="ProKantar ER" id="er"><mxGraphModel dx="1600" dy="900" grid="1" gridSize="10" guides="1" tooltips="1" connect="1" arrows="1" fold="1" page="0" math="0" shadow="0"><root><mxCell id="0"/><mxCell id="1" parent="0"/>'
       + "".join(cells) + "</root></mxGraphModel></diagram></mxfile>")
os.makedirs(OUT, exist_ok=True)
open(os.path.join(OUT, "prokantar-er.drawio"), "w", encoding="utf-8").write(xml)

# ======================= YAML =======================
def yl(v):
    return v if isinstance(v, str) else "[" + ", ".join(v) + "]"

def yaml_prop(f):
    simple = not f["null"] and f["ml"] is None and f["default"] is None and not f["values"] and not f["sm"]
    if simple:
        v = f["type"]
    else:
        parts = [f"type: {f['type']}"]
        if f["values"]: parts.append(f"values: [{', '.join(f['values'])}]")
        if f["null"]: parts.append("nullable: true")
        if f["ml"]: parts.append(f"maxLength: {f['ml']}")
        if f["default"] is not None: parts.append(f'default: "{f["default"]}"')
        if f["sm"]: parts.append("serverManaged: true")
        v = "{ " + ", ".join(parts) + " }"
    line = f"      {f['name']}: {v}"
    if f["note"]: line = (line + " ").ljust(78) + f"# {f['note']}"
    return line

for svc in (platform, kantar):
    L = [f"# ProKantar — {svc['title']}",
         "# Kaynak: docs/model/gen_prokantar.py (ER şemasıyla birlikte üretilir; elle düzenleme yerine oradan değiştir).",
         f"service: {svc['key']}", f"database: {svc['db']}"]
    if svc["multi"]:
        L.append("multiTenant: true                # tüm entity'ler TenantId + query filter; tenant_id JWT'den")
    L += ["",
          "auth:                            # ZORUNLU: roller ve tenant_id JWT'den okunur",
          "  authority: http://host.docker.internal:8081   # identity REST",
          "  audience: baseforge-api", "  requireHttpsMetadata: false", "  protect: true",
          f"  defaultAccess: {yl(svc['defaultAccess'])}",
          f"  superRoles: [{SUPER}]          # NOT: tenant filtresini aşmaz (BaseForge §6.1)",
          "", "dockerPorts:                     # identity: 8081/8082/5432",
          f"  rest: {svc['ports'][0]}", f"  grpc: {svc['ports'][1]}", f"  postgres: {svc['ports'][2]}",
          "", "entities:"]
    for e in svc["entities"]:
        L.append(f"  {e['name']}:")
        if e.get("appendOnly"): L.append("    appendOnly: true")
        if e.get("publishes"): L.append(f"    publishes: [{', '.join(e['publishes'])}]")
        if e.get("owner"): L.append(f"    ownerField: {e['owner'][0].upper() + e['owner'][1:]}")
        if e.get("filter"): L.append(f"    filterable: [{', '.join(e['filter'])}]")
        if e.get("search"): L.append(f"    searchFields: [{', '.join(e['search'])}]")
        if e.get("access"):
            L.append("    access:")
            for k, v in e["access"].items():
                L.append(f"      {k}: {yl(v)}")
        L.append("    props:")
        L += [yaml_prop(f) for f in e["fields"]]
        if e["rel"]:
            L.append("    relations:")
            for r in e["rel"]:
                L += [f"      {r[0]}:", f"        kind: {r[1]}", f"        target: {r[2]}"]
                if r[3]: L.append("        nullable: true")
        if e["ext"]:
            L.append("    externalRefs:")
            for x in e["ext"]:
                L += [f"      {x[0]}:", f"        target: {x[1]}", f"        store: {x[2]}", "        via: grpc"]
        L.append("")
    open(os.path.join(OUT, f"{svc['key']}.yaml"), "w", encoding="utf-8").write("\n".join(L) + "\n")
print("ok")

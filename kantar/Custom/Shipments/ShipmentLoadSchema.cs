namespace Kantar.Custom.Shipments;

/// <summary>
/// Yük satırı tablosu (EnsureCreated var olan DB'ye tablo eklemez). İdempotent; açılışta çalışır, yeni
/// kurulumda tabloyu EnsureCreated oluşturduğundan etkisizdir. Kolonlar <c>ShipmentStopLine</c> ile birebir.
/// </summary>
public static class ShipmentLoadSchema
{
    /// <summary>İdempotent DDL.</summary>
    public const string Sql = """
        CREATE TABLE IF NOT EXISTS "ShipmentStopLines" (
            "Id" uuid NOT NULL CONSTRAINT "PK_ShipmentStopLines" PRIMARY KEY,
            "Sequence" integer NOT NULL,
            "ShipmentStopId" uuid NOT NULL
                CONSTRAINT "FK_ShipmentStopLines_ShipmentStops_ShipmentStopId" REFERENCES "ShipmentStops" ("Id") ON DELETE CASCADE,
            "MaterialId" uuid NOT NULL
                CONSTRAINT "FK_ShipmentStopLines_Materials_MaterialId" REFERENCES "Materials" ("Id") ON DELETE RESTRICT,
            "MaterialCode" character varying(50) NOT NULL,
            "MaterialName" character varying(200) NOT NULL,
            "PlannedQuantity" numeric NOT NULL,
            "Unit" text NOT NULL,
            "TenantId" uuid NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "CreatedBy" text NULL,
            "IsDeleted" boolean NOT NULL,
            "DeletedAt" timestamp with time zone NULL
        );
        CREATE INDEX IF NOT EXISTS "IX_ShipmentStopLines_ShipmentStopId" ON "ShipmentStopLines" ("ShipmentStopId");
        CREATE INDEX IF NOT EXISTS "IX_ShipmentStopLines_MaterialId" ON "ShipmentStopLines" ("MaterialId");
        """;
}

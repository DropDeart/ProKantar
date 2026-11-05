namespace Kantar.Custom.EDespatch.Issuing;

/// <summary>
/// Generate tablolarına elle eklenen kolon ve indeksler (EnsureCreated var olan tabloyu değiştirmez). İdempotent;
/// açılışta çalışır, yeni kurulumda EnsureCreated kolonları zaten oluşturduğundan etkisizdir.
/// </summary>
public static class WaybillSchema
{
    /// <summary>İdempotent DDL.</summary>
    public const string Sql = """
        ALTER TABLE "Waybills"
            ADD COLUMN IF NOT EXISTS "EDespatchProviderKind" text NULL,
            ADD COLUMN IF NOT EXISTS "EDespatchProviderRef" character varying(100) NULL,
            ADD COLUMN IF NOT EXISTS "EDespatchReceiverAlias" character varying(200) NULL,
            ADD COLUMN IF NOT EXISTS "EDespatchMessage" text NULL,
            ADD COLUMN IF NOT EXISTS "EDespatchSentAt" timestamp with time zone NULL,
            ADD COLUMN IF NOT EXISTS "EDespatchCheckedAt" timestamp with time zone NULL,
            ADD COLUMN IF NOT EXISTS "UblXml" text NULL,
            ADD COLUMN IF NOT EXISTS "DriverName" character varying(150) NULL,
            ADD COLUMN IF NOT EXISTS "DriverNationalId" character varying(11) NULL,
            ADD COLUMN IF NOT EXISTS "Plate" character varying(15) NULL,
            ADD COLUMN IF NOT EXISTS "TrailerPlate" character varying(15) NULL;
        ALTER TABLE "Stations" ADD COLUMN IF NOT EXISTS "PostalCode" character varying(5) NULL;
        ALTER TABLE "DeliveryLocations" ADD COLUMN IF NOT EXISTS "PostalCode" character varying(5) NULL;
        ALTER TABLE "Companies"
            ADD COLUMN IF NOT EXISTS "City" character varying(50) NULL,
            ADD COLUMN IF NOT EXISTS "District" character varying(50) NULL,
            ADD COLUMN IF NOT EXISTS "PostalCode" character varying(5) NULL;
        ALTER TABLE "Shipments" ADD COLUMN IF NOT EXISTS "CancelReason" character varying(250) NULL;
        CREATE UNIQUE INDEX IF NOT EXISTS "UX_Waybills_TenantId_WeighingId_Valid" ON "Waybills" ("TenantId", "WeighingId")
            WHERE NOT "IsDeleted" AND "EDespatchStatus" IS DISTINCT FROM 'Rejected';
        CREATE UNIQUE INDEX IF NOT EXISTS "UX_Waybills_TenantId_WaybillNo" ON "Waybills" ("TenantId", "WaybillNo")
            WHERE NOT "IsDeleted";
        """;
}

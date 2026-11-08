namespace Platform.Custom.Drivers;

/// <summary>"Şoförlerimiz" tablosu (EnsureCreated var olan DB'ye tablo eklemez); idempotent, açılışta çalışır.</summary>
public static class TenantDriverSchema
{
    /// <summary>İdempotent DDL. Bir firmada aynı numaraya en çok bir açık (bekleyen/aktif) kayıt.</summary>
    public const string Sql = """
        CREATE TABLE IF NOT EXISTS "TenantDrivers" (
            "Id" uuid NOT NULL CONSTRAINT "PK_TenantDrivers" PRIMARY KEY,
            "TenantId" uuid NOT NULL,
            "TenantName" character varying(200) NOT NULL,
            "DriverId" uuid NULL,
            "Phone" character varying(10) NOT NULL,
            "Status" text NOT NULL,
            "Source" text NOT NULL,
            "InvitedByUserId" uuid NOT NULL,
            "RespondedAt" timestamp with time zone NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "CreatedBy" text NULL,
            "IsDeleted" boolean NOT NULL,
            "DeletedAt" timestamp with time zone NULL
        );
        CREATE INDEX IF NOT EXISTS "IX_TenantDrivers_TenantId" ON "TenantDrivers" ("TenantId");
        CREATE INDEX IF NOT EXISTS "IX_TenantDrivers_Phone" ON "TenantDrivers" ("Phone");
        CREATE INDEX IF NOT EXISTS "IX_TenantDrivers_DriverId" ON "TenantDrivers" ("DriverId");
        CREATE UNIQUE INDEX IF NOT EXISTS "UX_TenantDrivers_Open" ON "TenantDrivers" ("TenantId", "Phone")
            WHERE "Status" IN ('Invited', 'Active') AND NOT "IsDeleted";
        CREATE INDEX IF NOT EXISTS "IX_Drivers_Phone" ON "Drivers" ("Phone");
        ALTER TABLE "TenantDrivers" ADD COLUMN IF NOT EXISTS "InviteToken" character varying(32) NULL;
        UPDATE "TenantDrivers" SET "InviteToken" = substr(md5(random()::text || "Id"::text), 1, 32) WHERE "InviteToken" IS NULL;
        CREATE UNIQUE INDEX IF NOT EXISTS "UX_TenantDrivers_InviteToken" ON "TenantDrivers" ("InviteToken");
        """;
}

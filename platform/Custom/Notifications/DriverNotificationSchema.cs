namespace Platform.Custom.Notifications;

/// <summary>Şoför bildirimleri ve denemeleri (EnsureCreated var olan DB'ye tablo eklemez); idempotent, açılışta çalışır.</summary>
public static class DriverNotificationSchema
{
    /// <summary>İdempotent DDL. İrsaliye başına tek bildirim (olay yeniden teslim edilse de).</summary>
    public const string Sql = """
        CREATE TABLE IF NOT EXISTS "DriverNotifications" (
            "Id" uuid NOT NULL CONSTRAINT "PK_DriverNotifications" PRIMARY KEY,
            "TenantId" uuid NOT NULL,
            "DriverId" uuid NOT NULL,
            "UserId" uuid NOT NULL,
            "Phone" character varying(20) NULL,
            "WaybillId" uuid NOT NULL,
            "WaybillNo" character varying(30) NOT NULL,
            "ShipmentId" uuid NULL,
            "ShipmentNo" character varying(40) NULL,
            "Plate" character varying(20) NOT NULL,
            "DeliveryAddress" character varying(500) NULL,
            "LinkUrl" character varying(1000) NOT NULL,
            "IssuedAt" timestamp with time zone NOT NULL,
            "Status" text NOT NULL,
            "DeliveredVia" text NULL,
            "DeliveredAt" timestamp with time zone NULL,
            "AckedAt" timestamp with time zone NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "CreatedBy" text NULL,
            "IsDeleted" boolean NOT NULL,
            "DeletedAt" timestamp with time zone NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "UX_DriverNotifications_WaybillId" ON "DriverNotifications" ("WaybillId");
        CREATE INDEX IF NOT EXISTS "IX_DriverNotifications_UserId" ON "DriverNotifications" ("UserId");
        CREATE INDEX IF NOT EXISTS "IX_DriverNotifications_ShipmentId" ON "DriverNotifications" ("ShipmentId");
        CREATE INDEX IF NOT EXISTS "IX_DriverNotifications_Pending" ON "DriverNotifications" ("Status") WHERE "Status" = 'Pending';

        CREATE TABLE IF NOT EXISTS "DriverNotificationAttempts" (
            "Id" uuid NOT NULL CONSTRAINT "PK_DriverNotificationAttempts" PRIMARY KEY,
            "DriverNotificationId" uuid NOT NULL
                CONSTRAINT "FK_DriverNotificationAttempts_DriverNotifications" REFERENCES "DriverNotifications" ("Id") ON DELETE CASCADE,
            "Channel" text NOT NULL,
            "Result" text NOT NULL,
            "Detail" character varying(500) NULL,
            "At" timestamp with time zone NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "CreatedBy" text NULL,
            "IsDeleted" boolean NOT NULL,
            "DeletedAt" timestamp with time zone NULL
        );
        CREATE INDEX IF NOT EXISTS "IX_DriverNotificationAttempts_DriverNotificationId" ON "DriverNotificationAttempts" ("DriverNotificationId");
        """;
}

namespace Platform.Custom.Fleet;

/// <summary>
/// Araç kataloğu ve araç özelliği tabloları (EnsureCreated var olan DB'ye tablo eklemez). İdempotent; açılışta
/// çalışır, yeni kurulumda tabloları EnsureCreated oluşturduğundan etkisizdir. Kolonlar entity'lerle birebir.
/// </summary>
public static class FleetSchema
{
    /// <summary>İdempotent DDL.</summary>
    public const string Sql = """
        CREATE TABLE IF NOT EXISTS "VehicleModels" (
            "Id" uuid NOT NULL CONSTRAINT "PK_VehicleModels" PRIMARY KEY,
            "Brand" character varying(50) NOT NULL,
            "Name" character varying(80) NOT NULL,
            "BodyType" text NOT NULL,
            "AxleConfig" character varying(10) NULL,
            "AxleCount" integer NOT NULL,
            "MaxGrossKg" numeric NOT NULL,
            "TypicalTareKg" numeric NULL,
            "AvgFuelLPer100Km" numeric NULL,
            "IsApproved" boolean NOT NULL,
            "SuggestedByUserId" uuid NULL,
            "IsActive" boolean NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "CreatedBy" text NULL,
            "IsDeleted" boolean NOT NULL,
            "DeletedAt" timestamp with time zone NULL
        );
        CREATE TABLE IF NOT EXISTS "VehicleSpecs" (
            "Id" uuid NOT NULL CONSTRAINT "PK_VehicleSpecs" PRIMARY KEY,
            "VehicleId" uuid NOT NULL
                CONSTRAINT "FK_VehicleSpecs_Vehicles_VehicleId" REFERENCES "Vehicles" ("Id") ON DELETE CASCADE,
            "VehicleModelId" uuid NULL
                CONSTRAINT "FK_VehicleSpecs_VehicleModels_VehicleModelId" REFERENCES "VehicleModels" ("Id") ON DELETE RESTRICT,
            "RegisteredMaxGrossKg" numeric NULL,
            "FleetTenantId" uuid NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "CreatedBy" text NULL,
            "IsDeleted" boolean NOT NULL,
            "DeletedAt" timestamp with time zone NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_VehicleSpecs_VehicleId" ON "VehicleSpecs" ("VehicleId");
        CREATE INDEX IF NOT EXISTS "IX_VehicleSpecs_VehicleModelId" ON "VehicleSpecs" ("VehicleModelId");
        CREATE INDEX IF NOT EXISTS "IX_VehicleSpecs_FleetTenantId" ON "VehicleSpecs" ("FleetTenantId");
        CREATE INDEX IF NOT EXISTS "IX_Vehicles_Plate" ON "Vehicles" ("Plate");
        """;
}

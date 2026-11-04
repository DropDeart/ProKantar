using Identity.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Tenancy;

/// <summary>Şirket tablolarının EF eşlemesi ve var olan veritabanına eklenmesi.</summary>
public static class TenancyModel
{
    private static readonly string OpenStatuses = string.Join(",", TenantMembership.OpenStatuses.Select(s => $"'{s}'"));

    /// <summary>EF modeli (yeni veritabanında EnsureCreated bunlardan oluşturur).</summary>
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<Tenant>(tenant =>
        {
            tenant.ToTable("Tenants");
            tenant.Property(t => t.Name).HasMaxLength(200);
            tenant.Property(t => t.Slug).HasMaxLength(60);
            tenant.Property(t => t.TaxNo).HasMaxLength(20);
            tenant.Property(t => t.TaxOffice).HasMaxLength(100);
            tenant.Property(t => t.MersisNo).HasMaxLength(16);
            tenant.Property(t => t.TradeRegistryNo).HasMaxLength(30);
            tenant.Property(t => t.Phone).HasMaxLength(20);
            tenant.Property(t => t.Email).HasMaxLength(256);
            tenant.Property(t => t.KepAddress).HasMaxLength(256);
            tenant.Property(t => t.City).HasMaxLength(60);
            tenant.Property(t => t.District).HasMaxLength(60);
            tenant.Property(t => t.Address).HasMaxLength(500);
            tenant.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            tenant.Property(t => t.ReviewNote).HasMaxLength(500);
            tenant.HasIndex(t => t.Slug).IsUnique();
        });

        builder.Entity<TenantDocument>(document =>
        {
            document.ToTable("TenantDocuments");
            document.Property(d => d.Type).HasConversion<string>().HasMaxLength(30);
            document.Property(d => d.FileName).HasMaxLength(120);
            document.Property(d => d.ContentType).HasMaxLength(50);
            document.HasOne<Tenant>().WithMany().HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Cascade);
            document.HasIndex(d => d.TenantId);
        });

        builder.Entity<TenantMembership>(membership =>
        {
            membership.ToTable("TenantMemberships");
            membership.Property(m => m.InvitedEmail).HasMaxLength(256);
            membership.Property(m => m.InvitedPhone).HasMaxLength(32);
            membership.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);
            membership.HasOne(m => m.Tenant).WithMany().HasForeignKey(m => m.TenantId).OnDelete(DeleteBehavior.Cascade);
            membership.HasOne<ApplicationUser>().WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
            // Bir kişinin bir şirkette tek açık kaydı olur (davet, istek ya da üyelik).
            membership.HasIndex(m => new { m.TenantId, m.UserId }).IsUnique()
                .HasFilter($"\"UserId\" IS NOT NULL AND \"Status\" IN ({OpenStatuses})");
            membership.HasIndex(m => m.UserId);
            membership.HasIndex(m => m.InvitedEmail);
            membership.HasIndex(m => m.InvitedPhone);
        });
    }

    /// <summary>
    /// EnsureCreated var olan veritabanına tablo eklemez; 0.6.3 ve öncesinden gelen identity_db için
    /// tabloları idempotent DDL ile oluşturur (EF modeliyle aynı şema).
    /// </summary>
    public static Task EnsureTablesAsync(DbContext db, CancellationToken cancellationToken = default)
    {
        // OpenStatuses enum adlarından üretilir (kullanıcı girdisi değil).
        var ddl = $"""
            CREATE TABLE IF NOT EXISTS "Tenants" (
                "Id" uuid NOT NULL CONSTRAINT "PK_Tenants" PRIMARY KEY,
                "Name" character varying(200) NOT NULL,
                "Slug" character varying(60) NOT NULL,
                "TaxNo" character varying(20) NULL,
                "Status" character varying(20) NOT NULL,
                "CreatedByUserId" uuid NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "ReviewedAt" timestamp with time zone NULL,
                "ReviewedByUserId" uuid NULL,
                "ReviewNote" character varying(500) NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Tenants_Slug" ON "Tenants" ("Slug");
            ALTER TABLE "Tenants"
                ADD COLUMN IF NOT EXISTS "TaxOffice" character varying(100) NULL,
                ADD COLUMN IF NOT EXISTS "MersisNo" character varying(16) NULL,
                ADD COLUMN IF NOT EXISTS "TradeRegistryNo" character varying(30) NULL,
                ADD COLUMN IF NOT EXISTS "Phone" character varying(20) NULL,
                ADD COLUMN IF NOT EXISTS "Email" character varying(256) NULL,
                ADD COLUMN IF NOT EXISTS "KepAddress" character varying(256) NULL,
                ADD COLUMN IF NOT EXISTS "City" character varying(60) NULL,
                ADD COLUMN IF NOT EXISTS "District" character varying(60) NULL,
                ADD COLUMN IF NOT EXISTS "Address" character varying(500) NULL;

            CREATE TABLE IF NOT EXISTS "TenantDocuments" (
                "Id" uuid NOT NULL CONSTRAINT "PK_TenantDocuments" PRIMARY KEY,
                "TenantId" uuid NOT NULL CONSTRAINT "FK_TenantDocuments_Tenants_TenantId" REFERENCES "Tenants" ("Id") ON DELETE CASCADE,
                "Type" character varying(30) NOT NULL,
                "FileName" character varying(120) NOT NULL,
                "ContentType" character varying(50) NOT NULL,
                "Size" bigint NOT NULL,
                "UploadedByUserId" uuid NOT NULL,
                "UploadedAt" timestamp with time zone NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_TenantDocuments_TenantId" ON "TenantDocuments" ("TenantId");

            CREATE TABLE IF NOT EXISTS "TenantMemberships" (
                "Id" uuid NOT NULL CONSTRAINT "PK_TenantMemberships" PRIMARY KEY,
                "TenantId" uuid NOT NULL CONSTRAINT "FK_TenantMemberships_Tenants_TenantId" REFERENCES "Tenants" ("Id") ON DELETE CASCADE,
                "UserId" uuid NULL CONSTRAINT "FK_TenantMemberships_AspNetUsers_UserId" REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE,
                "InvitedEmail" character varying(256) NULL,
                "InvitedPhone" character varying(32) NULL,
                "Roles" text[] NOT NULL,
                "Status" character varying(20) NOT NULL,
                "IsDefault" boolean NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "CreatedByUserId" uuid NULL,
                "RespondedAt" timestamp with time zone NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_TenantMemberships_TenantId_UserId" ON "TenantMemberships" ("TenantId", "UserId")
                WHERE "UserId" IS NOT NULL AND "Status" IN ({OpenStatuses});
            CREATE INDEX IF NOT EXISTS "IX_TenantMemberships_UserId" ON "TenantMemberships" ("UserId");
            CREATE INDEX IF NOT EXISTS "IX_TenantMemberships_InvitedEmail" ON "TenantMemberships" ("InvitedEmail");
            CREATE INDEX IF NOT EXISTS "IX_TenantMemberships_InvitedPhone" ON "TenantMemberships" ("InvitedPhone");
            """;
        return db.Database.ExecuteSqlRawAsync(ddl, cancellationToken);
    }
}

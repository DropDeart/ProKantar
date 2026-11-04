namespace Tracking.Common;

/// <summary>identity/auth.yaml rolleri.</summary>
public static class TrackingRoles
{
    /// <summary>Şoför uygulaması.</summary>
    public const string Driver = "Driver";

    /// <summary>Kiracı personeli (yönetici, operatör) ve platform yöneticisi.</summary>
    public const string Staff = "TenantAdmin,Operator,SuperAdmin";
}

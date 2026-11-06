using BaseForge.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Platform.Entities;

/// <summary>
/// Aracın katalog modeli, ruhsat değeri ve firma filosu bağı (elle yazıldı; generate edilen <see cref="Vehicle"/>'a
/// dokunmamak için 1-1 ayrı tablo). Araç başına en çok bir kayıt.
/// </summary>
[Index(nameof(VehicleId), IsUnique = true)]
[Index(nameof(FleetTenantId))]
public sealed class VehicleSpec : BaseEntity
{
    /// <summary>Araç.</summary>
    public Guid VehicleId { get; set; }

    /// <summary>Katalog modeli.</summary>
    public Guid? VehicleModelId { get; set; }

    /// <summary>Ruhsattaki azami yüklü ağırlık (kg); katalog değerinin önüne geçer.</summary>
    public decimal? RegisteredMaxGrossKg { get; set; }

    /// <summary>Doluysa araç bu firmanın kendi filosundadır ("Araçlarımız").</summary>
    public Guid? FleetTenantId { get; set; }

    /// <summary>Filo aracının varsayılan şoförü (firma atar, şoför onayı gerekmez); sevkiyat planında otomatik gelir.</summary>
    public Guid? DefaultDriverId { get; set; }

    /// <summary>Araç (servis içi ilişki).</summary>
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public Vehicle? Vehicle { get; set; }

    /// <summary>Katalog modeli (servis içi ilişki). Kullanılan model silinemez.</summary>
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public VehicleModel? VehicleModel { get; set; }
}

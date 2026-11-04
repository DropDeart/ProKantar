using Identity.Configuration;
using Identity.Data;
using Identity.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace Identity.Tenancy;

/// <summary>İndirilecek belge.</summary>
public sealed record TenantDocumentFile(Stream Content, string ContentType, string FileName);

/// <summary>Şirket başvuru belgeleri: yükleme, listeleme, indirme, silme.</summary>
public interface ITenantDocumentService
{
    /// <summary>Şirketin belgeleri.</summary>
    Task<IReadOnlyList<TenantDocumentDto>> ListAsync(Guid tenantId, TenantActor actor, CancellationToken cancellationToken);

    /// <summary>Belge yükler; "Diğer" dışındaki türlerde eskisinin yerine geçer.</summary>
    Task<TenantDocumentDto> UploadAsync(Guid tenantId, TenantActor actor, TenantDocumentType type, IFormFile file, CancellationToken cancellationToken);

    /// <summary>Dosyayı açar.</summary>
    Task<TenantDocumentFile> OpenAsync(Guid tenantId, Guid documentId, TenantActor actor, CancellationToken cancellationToken);

    /// <summary>Belgeyi siler (onaylı şirkette silinemez, yenisi yüklenir).</summary>
    Task DeleteAsync(Guid tenantId, Guid documentId, TenantActor actor, CancellationToken cancellationToken);
}

/// <inheritdoc cref="ITenantDocumentService" />
internal sealed class TenantDocumentService(
    IdentityServiceDbContext db,
    ITenantAdminGuard guard,
    AuthOptions options,
    IWebHostEnvironment environment,
    TimeProvider clock) : ITenantDocumentService
{
    /// <summary>Tek dosya üst sınırı.</summary>
    public const long MaxBytes = 10 * 1024 * 1024;

    private const int MaxOtherDocuments = 5;

    private string Root => Path.GetFullPath(options.MultiTenancy.DocumentsPath, environment.ContentRootPath);

    private DbSet<TenantDocument> Documents => db.Set<TenantDocument>();

    public async Task<IReadOnlyList<TenantDocumentDto>> ListAsync(Guid tenantId, TenantActor actor, CancellationToken cancellationToken)
    {
        await guard.EnsureCanEditAsync(tenantId, actor, cancellationToken, allowSuspended: true);
        var documents = await Documents.AsNoTracking()
            .Where(d => d.TenantId == tenantId)
            .OrderBy(d => d.Type).ThenBy(d => d.UploadedAt)
            .ToListAsync(cancellationToken);
        return [.. documents.Select(TenantDocumentDto.From)];
    }

    public async Task<TenantDocumentDto> UploadAsync(
        Guid tenantId, TenantActor actor, TenantDocumentType type, IFormFile file, CancellationToken cancellationToken)
    {
        await guard.EnsureCanEditAsync(tenantId, actor, cancellationToken);
        if (file.Length is 0 or > MaxBytes)
        {
            throw IdentityException.Invalid("Dosya boş olamaz ve en fazla 10 MB olabilir.");
        }

        var (contentType, extension) = await DetectAsync(file, cancellationToken);
        var replaced = type == TenantDocumentType.Other
            ? []
            : await Documents.Where(d => d.TenantId == tenantId && d.Type == type).ToListAsync(cancellationToken);
        if (type == TenantDocumentType.Other
            && await Documents.CountAsync(d => d.TenantId == tenantId && d.Type == type, cancellationToken) >= MaxOtherDocuments)
        {
            throw IdentityException.Invalid($"En fazla {MaxOtherDocuments} ek belge yüklenebilir.");
        }

        var document = new TenantDocument
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = type,
            FileName = SafeFileName(file.FileName, extension),
            ContentType = contentType,
            Size = file.Length,
            UploadedByUserId = actor.UserId,
            UploadedAt = clock.GetUtcNow(),
        };

        var path = PathOf(document);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using (var target = File.Create(path))
        {
            await file.CopyToAsync(target, cancellationToken);
        }

        Documents.RemoveRange(replaced);
        Documents.Add(document);
        await db.SaveChangesAsync(cancellationToken);
        replaced.ForEach(old => File.Delete(PathOf(old)));
        return TenantDocumentDto.From(document);
    }

    public async Task<TenantDocumentFile> OpenAsync(Guid tenantId, Guid documentId, TenantActor actor, CancellationToken cancellationToken)
    {
        await guard.EnsureCanEditAsync(tenantId, actor, cancellationToken, allowSuspended: true);
        var document = await FindAsync(tenantId, documentId, cancellationToken);
        var path = PathOf(document);
        return File.Exists(path)
            ? new TenantDocumentFile(File.OpenRead(path), document.ContentType, document.FileName)
            : throw IdentityException.NotFound("Belge dosyası bulunamadı.");
    }

    public async Task DeleteAsync(Guid tenantId, Guid documentId, TenantActor actor, CancellationToken cancellationToken)
    {
        await guard.EnsureCanEditAsync(tenantId, actor, cancellationToken);
        var status = await db.Set<Tenant>().Where(t => t.Id == tenantId).Select(t => t.Status).FirstAsync(cancellationToken);
        if (status == TenantStatus.Active)
        {
            throw IdentityException.Conflict("Onaylı şirketin belgesi silinemez; yerine yenisini yükleyin.");
        }

        var document = await FindAsync(tenantId, documentId, cancellationToken);
        Documents.Remove(document);
        await db.SaveChangesAsync(cancellationToken);
        File.Delete(PathOf(document));
    }

    private async Task<TenantDocument> FindAsync(Guid tenantId, Guid documentId, CancellationToken cancellationToken)
        => await Documents.FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId, cancellationToken)
            ?? throw IdentityException.NotFound("Belge bulunamadı.");

    private string PathOf(TenantDocument document)
        => Path.Combine(Root, document.TenantId.ToString("N"), document.Id.ToString("N") + Path.GetExtension(document.FileName));

    // İçerik türü uzantıya ya da istemcinin bildirdiğine değil dosyanın ilk baytlarına göre belirlenir.
    private static async Task<(string ContentType, string Extension)> DetectAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var header = new byte[8];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        ReadOnlySpan<byte> bytes = header.AsSpan(0, read);
        if (bytes.StartsWith("%PDF"u8))
        {
            return ("application/pdf", ".pdf");
        }

        if (bytes.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]))
        {
            return ("image/jpeg", ".jpg");
        }

        if (bytes.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47]))
        {
            return ("image/png", ".png");
        }

        throw IdentityException.Invalid("Yalnızca PDF, JPG ya da PNG yüklenebilir.");
    }

    private static string SafeFileName(string original, string extension)
    {
        var name = Path.GetFileNameWithoutExtension(original);
        var safe = new string([.. name.Where(c => !Path.GetInvalidFileNameChars().Contains(c))]).Trim();
        return (safe.Length == 0 ? "belge" : safe[..Math.Min(safe.Length, 100)]) + extension;
    }
}

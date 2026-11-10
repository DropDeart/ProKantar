using BaseForge.Core.CQRS;
using System.ComponentModel.DataAnnotations;
using BaseForge.Core.Entities;
using BaseForge.Core.Exceptions;
using ValidationException = BaseForge.Core.Exceptions.ValidationException;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Platform.Custom.Controllers;
using Platform.Custom.Validation;
using Platform.Data;

namespace Platform.Entities
{
    /// <summary>Tanıtım sitesinden gelen demo talebi (elle yazıldı; generate edilmez).</summary>
    public sealed class DemoRequest : BaseEntity
    {
        /// <summary>Ad soyad.</summary>
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        /// <summary>Firma adı.</summary>
        [MaxLength(200)]
        public string Company { get; set; } = string.Empty;

        /// <summary>Cep telefonu (5XXXXXXXXX).</summary>
        [MaxLength(10)]
        public string Phone { get; set; } = string.Empty;

        /// <summary>E-posta.</summary>
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        /// <summary>İlgilendiği paket (ör. isletme).</summary>
        [MaxLength(40)]
        public string? Plan { get; set; }

        /// <summary>Not.</summary>
        [MaxLength(1000)]
        public string? Message { get; set; }

        /// <summary>Talebi gönderen IP (kötüye kullanım incelemesi için).</summary>
        [MaxLength(64)]
        public string? IpAddress { get; set; }

        /// <summary>Yönetici ele aldı mı?</summary>
        public DateTimeOffset? HandledAt { get; set; }
    }
}

namespace Platform.Custom.DemoRequests
{
    using Platform.Entities;

    /// <summary>Demo talepleri tablosu (idempotent, açılışta).</summary>
    public static class DemoRequestSchema
    {
        /// <summary>İdempotent DDL.</summary>
        public const string Sql = """
            CREATE TABLE IF NOT EXISTS "DemoRequests" (
                "Id" uuid NOT NULL CONSTRAINT "PK_DemoRequests" PRIMARY KEY,
                "FullName" character varying(100) NOT NULL,
                "Company" character varying(200) NOT NULL,
                "Phone" character varying(10) NOT NULL,
                "Email" character varying(256) NOT NULL,
                "Plan" character varying(40) NULL,
                "Message" character varying(1000) NULL,
                "IpAddress" character varying(64) NULL,
                "HandledAt" timestamp with time zone NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "CreatedBy" text NULL,
                "IsDeleted" boolean NOT NULL,
                "DeletedAt" timestamp with time zone NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_DemoRequests_CreatedAt" ON "DemoRequests" ("CreatedAt" DESC);
            """;

        /// <summary>Uç başına istek sınırı politikası (IP başına).</summary>
        public const string RateLimitPolicy = "demo-requests";
    }

    /// <summary>Demo talebi gövdesi. <see cref="Website"/> botlar için tuzaktır (insan doldurmaz; doluysa sessizce yok sayılır).</summary>
    public sealed record DemoRequestInput(string? FullName, string? Company, string? Phone, string? Email, string? Plan, string? Message, string? Website);

    /// <summary>Demo talebi görünümü (yönetici).</summary>
    public sealed record DemoRequestDto(
        Guid Id, string FullName, string Company, string Phone, string Email, string? Plan, string? Message, DateTimeOffset CreatedAt, DateTimeOffset? HandledAt);

    /// <summary>Demo talepleri liste sorgusu (sayfa, arama, yalnız açık olanlar).</summary>
    public sealed class DemoRequestListQuery : PagedRequest
    {
        /// <summary><c>true</c>: ele alınmamışlar, <c>false</c>: ele alınmışlar, boş: hepsi.</summary>
        public bool? Open { get; set; }
    }

    /// <summary>Sayfalı demo talepleri; <c>OpenCount</c> ele alınmayı bekleyen toplam (filtreden bağımsız).</summary>
    public sealed record DemoRequestPage(IReadOnlyList<DemoRequestDto> Items, int TotalCount, int Page, int PageSize, int OpenCount);

    /// <summary>Tanıtım sitesinin demo talebi (giriş istemez, IP başına sınırlı) ve yönetici listesi.</summary>
    [Route("api/demo-requests")]
    public sealed class DemoRequestsController(PlatformDbContext db) : CallerController
    {
        /// <summary>Demo talebi bırakır.</summary>
        [AllowAnonymous]
        [EnableRateLimiting(DemoRequestSchema.RateLimitPolicy)]
        [HttpPost]
        public async Task<IActionResult> Create(DemoRequestInput input, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(input);
            if (!string.IsNullOrEmpty(input.Website))
            {
                return Accepted();
            }

            var fullName = Required(input.FullName, nameof(input.FullName), "Adınızı ve soyadınızı girin.", 3, 100);
            var company = Required(input.Company, nameof(input.Company), "Firma adını girin.", 2, 200);
            var phone = Phones.Key(input.Phone ?? string.Empty);
            if (phone.Length != 10 || phone[0] is < '2' or > '5')
            {
                throw new ValidationException(nameof(input.Phone), "Alan koduyla 10 haneli bir telefon girin.");
            }

            var email = (input.Email ?? string.Empty).Trim();
            if (email.Length is < 5 or > 256 || !new EmailAddressAttribute().IsValid(email))
            {
                throw new ValidationException(nameof(input.Email), "Geçerli bir e-posta girin.");
            }

            db.Set<DemoRequest>().Add(new DemoRequest
            {
                FullName = fullName,
                Company = company,
                Phone = phone,
                Email = email,
                Plan = Optional(input.Plan, 40),
                Message = Optional(input.Message, 1000),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            });
            await db.SaveChangesAsync(cancellationToken);
            return Accepted();
        }

        /// <summary>
        /// Demo talepleri, sayfalı (en yeni önce): <c>?page=&amp;pageSize=&amp;search=&amp;open=true</c>.
        /// Arama: ad, firma, e-posta, telefon. <c>OpenCount</c> filtreden bağımsız, ele alınmayı bekleyen toplam talep.
        /// </summary>
        [Authorize(Roles = "SuperAdmin")]
        [HttpGet]
        public async Task<ActionResult<DemoRequestPage>> List([FromQuery] DemoRequestListQuery query, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);
            var all = db.Set<DemoRequest>().AsNoTracking();
            var rows = all;
            if (query.Open is { } open)
            {
                rows = rows.Where(r => (r.HandledAt == null) == open);
            }

            if (LikePattern.ContainsOrNull(query.Search) is { } pattern)
            {
                rows = rows.Where(r => EF.Functions.ILike(r.FullName, pattern, LikePattern.EscapeCharacter)
                    || EF.Functions.ILike(r.Company, pattern, LikePattern.EscapeCharacter)
                    || EF.Functions.ILike(r.Email, pattern, LikePattern.EscapeCharacter)
                    || EF.Functions.ILike(r.Phone, pattern, LikePattern.EscapeCharacter));
            }

            var items = await rows.OrderByDescending(r => r.CreatedAt)
                .Skip(query.Skip).Take(query.PageSize)
                .Select(r => new DemoRequestDto(r.Id, r.FullName, r.Company, r.Phone, r.Email, r.Plan, r.Message, r.CreatedAt, r.HandledAt))
                .ToListAsync(cancellationToken);
            return Ok(new DemoRequestPage(items, await rows.CountAsync(cancellationToken), query.Page, query.PageSize,
                await all.CountAsync(r => r.HandledAt == null, cancellationToken)));
        }

        /// <summary>Talebi ele alındı olarak işaretler.</summary>
        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("{id:guid}/handled")]
        public async Task<IActionResult> Handled(Guid id, CancellationToken cancellationToken)
        {
            var row = await db.Set<DemoRequest>().FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
                ?? throw new NotFoundException(nameof(DemoRequest), id);
            row.HandledAt ??= DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        private static string Required(string? value, string field, string message, int min, int max)
        {
            var text = (value ?? string.Empty).Trim();
            return text.Length >= min && text.Length <= max ? text : throw new ValidationException(field, message);
        }

        private static string? Optional(string? value, int max)
        {
            var text = value?.Trim();
            return string.IsNullOrEmpty(text) ? null : text.Length <= max ? text : text[..max];
        }
    }
}

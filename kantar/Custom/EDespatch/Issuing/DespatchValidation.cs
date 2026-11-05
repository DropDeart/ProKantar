using BaseForge.Core.Exceptions;
using Kantar.Entities;

namespace Kantar.Custom.EDespatch.Issuing;

/// <summary>
/// <see cref="DespatchRules"/> hatalarını API alan anahtarlarına çevirir: tek şoför <c>Driver.*</c>, taraf adresleri
/// düz (<c>Customer.Address.City</c> → <c>Customer.City</c>); teslim adresi <c>DeliveryAddress.*</c> kalır.
/// </summary>
public static class DespatchValidation
{
    private static readonly string[] Parties = ["Supplier", "Customer", "Station", "Carrier"];

    /// <summary>Belgenin API anahtarlı hataları.</summary>
    public static IReadOnlyList<DespatchRuleError> Check(DespatchDocument document)
        => DespatchRules.Check(document).Select(e => e with { Field = Normalize(e.Field) }).ToList();

    /// <summary>Hata varsa alan bazında 400.</summary>
    public static void EnsureValid(DespatchDocument document) => ThrowIfAny(Check(document));

    /// <summary>Liste boş değilse <see cref="ValidationException"/>.</summary>
    public static void ThrowIfAny(IReadOnlyList<DespatchRuleError> errors)
    {
        if (errors.Count > 0)
        {
            throw new ValidationException(errors
                .GroupBy(e => e.Field, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray(), StringComparer.Ordinal));
        }
    }

    /// <summary>Alan adını API anahtarına çevirir.</summary>
    public static string Normalize(string field)
    {
        if (field.StartsWith("Drivers[0]", StringComparison.Ordinal))
        {
            return "Driver" + field["Drivers[0]".Length..];
        }

        if (field == "Drivers")
        {
            return "Driver";
        }

        foreach (var party in Parties)
        {
            var prefix = party + ".Address.";
            if (field.StartsWith(prefix, StringComparison.Ordinal))
            {
                return party + "." + field[prefix.Length..];
            }
        }

        return field;
    }
}

/// <summary>Tartımın irsaliye kesilebilir durumda olup olmadığı.</summary>
public static class DespatchEligibility
{
    /// <summary>Kesilmiş irsaliye hata mesajı.</summary>
    public const string AlreadyIssued = "Bu tartıma irsaliye kesilmiş.";

    /// <summary>Yalnız tamamlanmış çıkış tartımı, geçerli irsaliyesi yoksa.</summary>
    public static IReadOnlyList<DespatchRuleError> Check(Weighing weighing, bool hasValidWaybill)
    {
        ArgumentNullException.ThrowIfNull(weighing);
        var errors = new List<DespatchRuleError>();
        if (weighing.Direction != WeighingDirection.Outbound)
        {
            errors.Add(new("Weighing.Direction", "İrsaliye yalnız çıkış tartımına kesilir."));
        }

        if (weighing.Status != WeighingStatus.Completed)
        {
            errors.Add(new("Weighing.Status", "İrsaliye çıkış tartımı tamamlanınca kesilir."));
        }

        if (hasValidWaybill)
        {
            errors.Add(new("Weighing", AlreadyIssued));
        }

        return errors;
    }
}

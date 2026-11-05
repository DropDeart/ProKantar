namespace Kantar.Custom.EDespatch.Providers;

/// <summary>GİB'e ait sabit posta kutusu etiketleri (tek yerde tutulur).</summary>
public static class GibAliases
{
    /// <summary>
    /// e-İrsaliye mükellefi olmayan alıcılar için GİB varsayılan posta kutusu. Belge buraya gider, mal kâğıt
    /// çıktısıyla taşınır. DOĞRULANMALI: canlıya geçmeden önce entegratör ve güncel GİB kılavuzundan teyit edin.
    /// </summary>
    public const string DefaultDespatchReceiver = "urn:mail:irsaliyepk@gib.gov.tr";
}

using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Kantar.Custom.EDespatch;

/// <summary>UBL belgesi üreten bileşen (test ve sağlayıcı değişimi için arayüz).</summary>
public interface IUblDespatchBuilder
{
    /// <summary>Belgeyi UTF-8 UBL-TR 1.2.1 DespatchAdvice XML'ine çevirir.</summary>
    byte[] Build(DespatchDocument document);
}

/// <summary>
/// <see cref="DespatchDocument"/> → GİB UBL-TR 1.2.1 DespatchAdvice (TEMELIRSALIYE / SEVK). Belge imzasız üretilir:
/// <c>ExtensionContent</c> boş kalır, mali mühür imzasını entegratör koyar. Alan sırası UBL-TR XSD'sini izler.
/// </summary>
public sealed class UblDespatchBuilder : IUblDespatchBuilder
{
    private static readonly XNamespace Root = "urn:oasis:names:specification:ubl:schema:xsd:DespatchAdvice-2";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Ext = "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2";

    /// <inheritdoc />
    public byte[] Build(DespatchDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var issued = TurkeyTime.ToLocal(document.IssuedAt);
        var despatched = TurkeyTime.ToLocal(document.DespatchedAt);

        var root = new XElement(Root + "DespatchAdvice",
            new XAttribute(XNamespace.Xmlns + "cac", Cac),
            new XAttribute(XNamespace.Xmlns + "cbc", Cbc),
            new XAttribute(XNamespace.Xmlns + "ext", Ext),
            new XElement(Ext + "UBLExtensions", new XElement(Ext + "UBLExtension", new XElement(Ext + "ExtensionContent"))),
            Cb("UBLVersionID", "2.1"),
            Cb("CustomizationID", "TR1.2.1"),
            Cb("ProfileID", "TEMELIRSALIYE"),
            Cb("ID", document.DocumentNo),
            Cb("CopyIndicator", "false"),
            Cb("UUID", document.Ettn.ToString("D").ToUpperInvariant()),
            Cb("IssueDate", Date(issued)),
            Cb("IssueTime", Time(issued)),
            Cb("DespatchAdviceTypeCode", "SEVK"),
            Notes(document),
            Cb("LineCountNumeric", document.Lines.Count.ToString(CultureInfo.InvariantCulture)),
            Signature(document.Supplier),
            new XElement(Cac + "DespatchSupplierParty", Party(document.Supplier, document.Station)),
            new XElement(Cac + "DeliveryCustomerParty", Party(document.Customer, null)),
            Shipment(document, despatched),
            document.Lines.Select((line, i) => Line(line, i + 1)));

        var settings = new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true };
        using var stream = new MemoryStream();
        using (var writer = XmlWriter.Create(stream, settings))
        {
            new XDocument(new XDeclaration("1.0", "UTF-8", null), root).Save(writer);
        }

        return stream.ToArray();
    }

    private static IEnumerable<XElement> Notes(DespatchDocument document)
    {
        if (!string.IsNullOrWhiteSpace(document.WeighingTicketNo))
        {
            yield return Cb("Note", $"Kantar fişi {document.WeighingTicketNo.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(document.Note))
        {
            yield return Cb("Note", document.Note.Trim());
        }
    }

    private static XElement Signature(DespatchParty supplier)
        => new(Cac + "Signature",
            new XElement(Cbc + "ID", new XAttribute("schemeID", "VKN_TCKN"), supplier.TaxId.Trim()),
            new XElement(Cac + "SignatoryParty", Identification(supplier.TaxId), Address("PostalAddress", supplier.Address)),
            new XElement(Cac + "DigitalSignatureAttachment",
                new XElement(Cac + "ExternalReference", Cb("URI", "#Signature"))));

    private static XElement Party(DespatchParty party, DespatchSite? site)
    {
        var isPerson = TaxIdChecks.Classify(party.TaxId) == TaxIdKind.Tckn;
        var person = isPerson ? PersonName.Split(party) : null;
        return new XElement(Cac + "Party",
            Identification(party.TaxId),
            new XElement(Cac + "PartyName", Cb("Name", party.Name.Trim())),
            Address("PostalAddress", party.Address),
            site is null ? null : new XElement(Cac + "PhysicalLocation", Cb("ID", site.Name.Trim()), Address("Address", site.Address)),
            string.IsNullOrWhiteSpace(party.TaxOffice)
                ? null
                : new XElement(Cac + "PartyTaxScheme", new XElement(Cac + "TaxScheme", Cb("Name", party.TaxOffice.Trim()))),
            Contact(party),
            person is { } p ? new XElement(Cac + "Person", Cb("FirstName", p.First), Cb("FamilyName", p.Family)) : null);
    }

    private static XElement? Contact(DespatchParty party)
    {
        if (string.IsNullOrWhiteSpace(party.Phone) && string.IsNullOrWhiteSpace(party.Email))
        {
            return null;
        }

        return new XElement(Cac + "Contact", Optional("Telephone", party.Phone), Optional("ElectronicMail", party.Email));
    }

    private static XElement Identification(string taxId)
    {
        var id = taxId.Trim();
        return new XElement(Cac + "PartyIdentification",
            new XElement(Cbc + "ID", new XAttribute("schemeID", id.Length == 11 ? "TCKN" : "VKN"), id));
    }

    private static XElement Address(string name, DespatchAddress address)
        => new(Cac + name,
            Optional("StreetName", address.Street),
            Cb("CitySubdivisionName", address.District?.Trim() ?? string.Empty),
            Cb("CityName", address.City?.Trim() ?? string.Empty),
            Optional("PostalZone", address.PostalCode),
            new XElement(Cac + "Country", Cb("Name", address.Country.Trim())));

    private static XElement Shipment(DespatchDocument document, DateTimeOffset despatched)
    {
        var stage = new XElement(Cac + "ShipmentStage",
            new XElement(Cac + "TransportMeans",
                new XElement(Cac + "RoadTransport",
                    new XElement(Cbc + "LicensePlateID", new XAttribute("schemeID", "PLAKA"), DespatchRules.NormalizePlate(document.Plate)))),
            document.Drivers.Select(d => new XElement(Cac + "DriverPerson",
                Cb("FirstName", d.FirstName.Trim()),
                Cb("FamilyName", d.FamilyName.Trim()),
                Cb("Title", "Şoför"),
                Cb("NationalityID", d.NationalId.Trim()))));

        var delivery = new XElement(Cac + "Delivery",
            Address("DeliveryAddress", document.DeliveryAddress),
            document.Carrier is { } carrier ? Carrier(carrier) : null,
            new XElement(Cac + "Despatch", Cb("ActualDespatchDate", Date(despatched)), Cb("ActualDespatchTime", Time(despatched))));

        var trailer = string.IsNullOrWhiteSpace(document.TrailerPlate)
            ? null
            : new XElement(Cac + "TransportHandlingUnit",
                new XElement(Cac + "TransportEquipment",
                    new XElement(Cbc + "ID", new XAttribute("schemeID", "DORSEPLAKA"), DespatchRules.NormalizePlate(document.TrailerPlate))));

        return new XElement(Cac + "Shipment", Cb("ID", "1"), stage, delivery, trailer);
    }

    private static XElement Carrier(DespatchCarrier carrier)
        => new(Cac + "CarrierParty",
            Identification(carrier.TaxId),
            new XElement(Cac + "PartyName", Cb("Name", carrier.Name.Trim())),
            Address("PostalAddress", carrier.Address));

    private static XElement Line(DespatchLine line, int number)
    {
        var id = number.ToString(CultureInfo.InvariantCulture);
        return new XElement(Cac + "DespatchLine",
            Cb("ID", id),
            new XElement(Cbc + "DeliveredQuantity",
                new XAttribute("unitCode", DespatchUnitCodes.Of(line.Unit)),
                line.Quantity.ToString("0.########", CultureInfo.InvariantCulture)),
            new XElement(Cac + "OrderLineReference", Cb("LineID", id)),
            new XElement(Cac + "Item",
                Cb("Name", line.Name.Trim()),
                string.IsNullOrWhiteSpace(line.Code) ? null : new XElement(Cac + "SellersItemIdentification", Cb("ID", line.Code.Trim()))));
    }

    private static XElement Cb(string name, string value) => new(Cbc + name, value);

    private static XElement? Optional(string name, string? value)
        => string.IsNullOrWhiteSpace(value) ? null : Cb(name, value.Trim());

    private static string Date(DateTimeOffset value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Time(DateTimeOffset value) => value.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
}

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LiventaTransfer.API.Common;

/// <summary>
/// ASP.NET model binding / System.Text.Json deserializasyon hatalarını (varsayılan olarak İngilizce
/// ve teknik gelen mesajları) kullanıcıya anlamlı Türkçe mesajlara çevirir.
/// </summary>
public static partial class ModelBindingErrorFormatter
{
    public static List<string> Format(ModelStateDictionary modelState)
    {
        var messages = new List<string>();

        foreach (var (key, entry) in modelState)
        {
            foreach (var error in entry.Errors)
                messages.Add(Translate(key, error.ErrorMessage, error.Exception));
        }

        var distinct = messages
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct()
            .ToList();

        return distinct.Count > 0
            ? distinct
            : new List<string> { "Gönderilen veri geçersiz." };
    }

    private static string Translate(string key, string? rawMessage, Exception? exception)
    {
        var raw = rawMessage ?? string.Empty;
        var field = FriendlyField(key);

        // Tip dönüşümü / geçersiz JSON değeri
        if (exception is System.Text.Json.JsonException ||
            raw.Contains("could not be converted", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("is not a valid", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("is invalid", StringComparison.OrdinalIgnoreCase))
        {
            return field is null
                ? "Gönderilen veride geçersiz bir değer var."
                : $"{field} alanına geçersiz bir değer girildi.";
        }

        // Zorunlu alan / boş gövde
        if (raw.Contains("required", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("non-empty request body", StringComparison.OrdinalIgnoreCase))
        {
            return field is null
                ? "İstek gövdesi (body) zorunludur ve boş olamaz."
                : $"{field} alanı zorunludur.";
        }

        // JSON gövdesi hiç ayrıştırılamadı
        if (raw.Contains("JSON", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("Unexpected", StringComparison.OrdinalIgnoreCase))
        {
            return "Gönderilen veri okunamadı; istek biçimi (JSON) geçersiz.";
        }

        // Zaten Türkçe/özel bir mesajsa olduğu gibi koru (ASCII dışı karakter varsa Türkçe sayarız).
        if (raw.Any(c => c > 127))
            return raw;

        return field is null
            ? "Geçersiz istek."
            : $"{field} alanı geçersiz.";
    }

    /// <summary>
    /// "$.stops[0].salePrice" gibi bir JSON yolunu "Durak 1 - Satış fiyatı" gibi okunur bir başlığa çevirir.
    /// </summary>
    private static string? FriendlyField(string? key)
    {
        if (string.IsNullOrWhiteSpace(key) || key == "$")
            return null;

        var path = key.StartsWith("$.") ? key[2..] : key.TrimStart('$', '.');
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var parts = new List<string>();
        foreach (var seg in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            var m = SegmentRegex().Match(seg);
            var name = m.Success ? m.Groups["name"].Value : seg;

            if (m.Success && m.Groups["idx"].Success && int.TryParse(m.Groups["idx"].Value, out var idx))
                parts.Add($"{CollectionItemLabel(name)} {idx + 1}");
            else
                parts.Add(Label(name));
        }

        return parts.Count == 0 ? null : string.Join(" - ", parts);
    }

    private static string CollectionItemLabel(string name) => name.ToLowerInvariant() switch
    {
        "stops" => "Durak",
        "passengerids" => "Yolcu",
        "jobids" => "İş",
        "mergedjobids" => "İş",
        "transfers" => "Transfer",
        _ => Label(name)
    };

    private static string Label(string name) => name.ToLowerInvariant() switch
    {
        // Ortak
        "name" => "Ad",
        "fullname" => "Ad soyad",
        "phone" => "Telefon",
        "email" => "E-posta",
        "notes" => "Not",
        "address" => "Adres",
        "isactive" => "Aktiflik",
        // İş
        "jobdate" => "İş tarihi",
        "jobtime" => "İş saati",
        "jobtype" => "İş tipi",
        "routedescription" => "Güzergah açıklaması",
        "extrainfo" => "Ek bilgi",
        "sourceemail" => "Kaynak e-posta",
        "vehicleownerid" => "Araç sahibi",
        "vehicleid" => "Araç",
        "driverid" => "Şoför",
        "purchaseprice" => "Alış fiyatı",
        "extracost" => "Ek maliyet",
        "stops" => "Duraklar",
        "newstatus" => "Yeni durum",
        "changereason" => "Değişiklik nedeni",
        "jobids" => "İş listesi",
        // Durak
        "customerid" => "Müşteri",
        "passengerids" => "Yolcular",
        "pickuplocationid" => "Alış lokasyonu",
        "dropofflocationid" => "Bırakış lokasyonu",
        "pickupaddress" => "Alış adresi",
        "dropoffaddress" => "Bırakış adresi",
        "flightcode" => "Uçuş kodu",
        "saleprice" => "Satış fiyatı",
        // Müşteri
        "customertype" => "Müşteri tipi",
        "taxnumber" => "Vergi numarası",
        "taxoffice" => "Vergi dairesi",
        "tckimlikno" => "TC Kimlik No",
        // Şoför
        "licensenumber" => "Ehliyet numarası",
        "defaultvehicleid" => "Varsayılan araç",
        // Lokasyon
        "shortcode" => "Kısa kod",
        "latitude" => "Enlem",
        "longitude" => "Boylam",
        "locationtype" => "Lokasyon tipi",
        // Kullanıcı / auth
        "username" => "Kullanıcı adı",
        "password" => "Parola",
        "firstname" => "Ad",
        "lastname" => "Soyad",
        "role" => "Rol",
        "branchid" => "Şube",
        _ => Humanize(name)
    };

    /// <summary>Bilinmeyen camelCase alan adını "salePrice" → "Sale price" gibi okunur hale getirir.</summary>
    private static string Humanize(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;
        var spaced = CamelCaseRegex().Replace(name, "$1 $2");
        return char.ToUpperInvariant(spaced[0]) + spaced[1..];
    }

    [GeneratedRegex(@"^(?<name>[^\[]+)(\[(?<idx>\d+)\])?$")]
    private static partial Regex SegmentRegex();

    [GeneratedRegex(@"(\p{Ll})(\p{Lu})")]
    private static partial Regex CamelCaseRegex();
}

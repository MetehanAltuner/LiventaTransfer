using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace LiventaTransfer.Application.Common;

public static class NameFormatter
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>
    /// Kişi adını Türkçe (tr-TR) kurallarına göre title case yapar: her kelimenin ilk harfi
    /// büyük, kalanı küçük olur ("kenan ozturk" -> "Kenan Ozturk", "ismail" -> "İsmail",
    /// "IŞIL" -> "Işıl"). Baştaki/sondaki boşluklar atılır, kelimeler arası fazla boşluklar
    /// tek boşluğa indirilir. Tireli kelimelerin her parçası ayrı değerlendirilir
    /// ("ayşe-nur" -> "Ayşe-Nur"). Null veya boş girdi olduğu gibi döner.
    /// </summary>
    [return: NotNullIfNotNull(nameof(name))]
    public static string? ToTitleCase(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(' ', words.Select(FormatWord));
    }

    private static string FormatWord(string word)
    {
        if (!word.Contains('-'))
            return Capitalize(word);

        // Tireli isimler: her parça ayrı title case yapılır ("ayşe-nur" -> "Ayşe-Nur").
        return string.Join('-', word.Split('-').Select(Capitalize));
    }

    private static string Capitalize(string part)
    {
        if (part.Length == 0)
            return part;

        // TextInfo.ToTitleCase tamamen büyük yazılmış kelimeleri kısaltma sayıp dokunmadığı
        // için ("IŞIL" -> "IŞIL") dönüşüm elle yapılıyor: önce tr-TR ile küçült, sonra ilk
        // harfi tr-TR ile büyüt ('i' -> 'İ', 'ı' -> 'I').
        var lower = part.ToLower(Turkish);
        return char.ToUpper(lower[0], Turkish) + lower[1..];
    }
}

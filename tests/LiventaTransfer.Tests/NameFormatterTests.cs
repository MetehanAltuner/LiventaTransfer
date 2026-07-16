using LiventaTransfer.Application.Common;

namespace LiventaTransfer.Tests;

public class NameFormatterTests
{
    [Fact]
    public void KucukHarfliAdSoyad_TitleCaseYapilir()
    {
        Assert.Equal("Kenan Ozturk", NameFormatter.ToTitleCase("kenan ozturk"));
    }

    [Fact]
    public void NoktasizI_TurkceBuyukIYapilir()
    {
        // tr-TR: 'i' -> 'İ' (invariant culture 'I' üretirdi)
        Assert.Equal("İsmail", NameFormatter.ToTitleCase("ismail"));
    }

    [Fact]
    public void TamamenBuyukYazilmisAd_TurkceKucultulur()
    {
        // tr-TR: 'I' -> 'ı', 'Ş' -> 'ş'; ilk harf 'I' kalır
        Assert.Equal("Işıl", NameFormatter.ToTitleCase("IŞIL"));
    }

    [Fact]
    public void BuyukHarfliAd_IlgazOrnegi()
    {
        // tr-TR: "ILGAZ" -> "Ilgaz" ('I' -> 'ı' küçülür, ilk harf 'I' kalır)
        Assert.Equal("Ilgaz", NameFormatter.ToTitleCase("ILGAZ"));
    }

    [Fact]
    public void KarisikBuyukKucuk_Normallesir()
    {
        Assert.Equal("Ayşe Yılmaz", NameFormatter.ToTitleCase("aYŞe yIlmaz"));
    }

    [Fact]
    public void FazlaBosluklar_TekBoslugaIndirilir()
    {
        Assert.Equal("Kenan Ozturk", NameFormatter.ToTitleCase("  kenan   ozturk  "));
    }

    [Fact]
    public void TireliAd_HerParcaTitleCaseYapilir()
    {
        Assert.Equal("Ayşe-Nur Demir", NameFormatter.ToTitleCase("ayşe-nur demir"));
    }

    [Fact]
    public void Null_OlduguGibiDoner()
    {
        Assert.Null(NameFormatter.ToTitleCase(null));
    }

    [Fact]
    public void BosString_OlduguGibiDoner()
    {
        Assert.Equal(string.Empty, NameFormatter.ToTitleCase(string.Empty));
    }

    [Fact]
    public void SadeceBosluk_OlduguGibiDoner()
    {
        Assert.Equal("   ", NameFormatter.ToTitleCase("   "));
    }

    [Fact]
    public void UcKelimeliAd_HepsiTitleCaseYapilir()
    {
        Assert.Equal("Mehmet Ali Kaya", NameFormatter.ToTitleCase("mehmet ALİ kaya"));
    }

    [Fact]
    public void ZatenDuzgunAd_Degismez()
    {
        Assert.Equal("Kenan Ozturk", NameFormatter.ToTitleCase("Kenan Ozturk"));
    }

    [Fact]
    public void TekHarfliKelime_Buyutulur()
    {
        Assert.Equal("A B", NameFormatter.ToTitleCase("a b"));
    }
}

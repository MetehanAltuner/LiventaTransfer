using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiventaTransfer.Application.Common;

/// <summary>
/// Nullable değer tiplerinde (decimal?, long?, int?, bool?, DateOnly?, ...) deserializasyonu
/// hoşgörülü hale getirir:
/// <list type="bullet">
/// <item>Boş string ("" / sadece boşluk) → null</item>
/// <item>String olarak gelen sayı/tarih ("150", "150,50", "2026-06-20") → uygun değere çevrilir</item>
/// </list>
/// Böylece frontend boş ya da string bir sayı gönderse bile istek deserializasyon aşamasında patlamaz.
/// Yalnızca Nullable&lt;T&gt; (değer tipi) alanlarına uygulanır; zorunlu (non-nullable) alanlar etkilenmez.
/// </summary>
public sealed class LenientNullableConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        var underlying = Nullable.GetUnderlyingType(typeToConvert);
        if (underlying is null)
            return false;

        return underlying.IsPrimitive            // int, long, short, byte, bool, double, float, ...
            || underlying == typeof(decimal)
            || underlying == typeof(DateOnly)
            || underlying == typeof(TimeOnly)
            || underlying == typeof(DateTime)
            || underlying == typeof(DateTimeOffset)
            || underlying == typeof(Guid);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var underlying = Nullable.GetUnderlyingType(typeToConvert)!;
        var converterType = typeof(LenientNullableConverter<>).MakeGenericType(underlying);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private sealed class LenientNullableConverter<T> : JsonConverter<T?> where T : struct
    {
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return null;

                case JsonTokenType.String:
                {
                    var str = reader.GetString();
                    if (string.IsNullOrWhiteSpace(str))
                        return null;
                    return ParseString(str.Trim());
                }

                default:
                    // Sayı / bool gibi normal token'lar: yerleşik (built-in) dönüşümü kullan.
                    // T (non-nullable) bu factory'ye uymadığından sonsuz döngü oluşmaz.
                    return JsonSerializer.Deserialize<T>(ref reader, options);
            }
        }

        public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                JsonSerializer.Serialize(writer, value.Value, options);
            else
                writer.WriteNullValue();
        }

        private static T ParseString(string s)
        {
            var t = typeof(T);
            var ci = CultureInfo.InvariantCulture;

            object parsed =
                t == typeof(decimal) ? decimal.Parse(NormalizeNumber(s), NumberStyles.Any, ci)
                : t == typeof(double) ? double.Parse(NormalizeNumber(s), NumberStyles.Any, ci)
                : t == typeof(float) ? float.Parse(NormalizeNumber(s), NumberStyles.Any, ci)
                : t == typeof(int) ? int.Parse(s, NumberStyles.Integer, ci)
                : t == typeof(long) ? long.Parse(s, NumberStyles.Integer, ci)
                : t == typeof(short) ? short.Parse(s, NumberStyles.Integer, ci)
                : t == typeof(byte) ? byte.Parse(s, NumberStyles.Integer, ci)
                : t == typeof(bool) ? ParseBool(s)
                : t == typeof(Guid) ? Guid.Parse(s)
                : t == typeof(DateOnly) ? DateOnly.Parse(s, ci, DateTimeStyles.None)
                : t == typeof(TimeOnly) ? TimeOnly.Parse(s, ci, DateTimeStyles.None)
                : t == typeof(DateTime) ? DateTime.Parse(s, ci, DateTimeStyles.RoundtripKind)
                : t == typeof(DateTimeOffset) ? DateTimeOffset.Parse(s, ci, DateTimeStyles.RoundtripKind)
                : throw new JsonException($"Desteklenmeyen tip: {t.Name}");

            return (T)parsed;
        }

        /// <summary>Ondalık ayırıcı olarak virgül kullanılmışsa noktaya çevirir (örn. "150,50" → "150.50").</summary>
        private static string NormalizeNumber(string s)
        {
            s = s.Trim();
            if (s.Contains(',') && !s.Contains('.'))
                s = s.Replace(',', '.');
            return s;
        }

        private static bool ParseBool(string s) => s.ToLowerInvariant() switch
        {
            "1" or "true" or "yes" or "evet" => true,
            "0" or "false" or "no" or "hayır" => false,
            _ => bool.Parse(s)
        };
    }
}

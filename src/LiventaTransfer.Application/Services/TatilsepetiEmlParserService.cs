using System.Globalization;
using System.Text.RegularExpressions;
using LiventaTransfer.Application.DTOs.EmlImport;
using MimeKit;

namespace LiventaTransfer.Application.Services;

public sealed class TatilsepetiEmlParserService
{
    private static readonly DateOnly InvalidDate = new(1900, 1, 1);

    public async Task<EmlParseResultDto> ParseAsync(Stream emlStream, CancellationToken ct)
    {
        var message = await MimeMessage.LoadAsync(emlStream, ct);

        var textBody = message.TextBody ?? StripHtml(message.HtmlBody ?? string.Empty);
        var lines = textBody.Split('\n').Select(l => l.Trim()).ToList();

        var senderEmail = message.From.Mailboxes.FirstOrDefault()?.Address ?? string.Empty;
        var subject = message.Subject ?? string.Empty;

        // İlk maili bul - en alttaki mail zincirindeki orijinal mail
        // "From: Mukaddes" ile başlayan blok öncesi orijinal mail body
        var firstMailBody = ExtractFirstMailBody(lines);

        var customerName = ParseCustomerNameFromSubject(subject);
        var passenger = ParsePassenger(firstMailBody);
        var transfers = ParseTransfers(firstMailBody);

        return new EmlParseResultDto
        {
            SenderEmail = senderEmail,
            Subject = subject,
            CustomerName = customerName,
            Passenger = passenger,
            Transfers = transfers,
            RawEmailBody = string.Join("\n", firstMailBody)
        };
    }

    private static List<string> ExtractFirstMailBody(List<string> lines)
    {
        // Mail zincirinde en alttaki (ilk gönderilen) maili bul
        // "Sent:" veya "From:" pattern'i ile mail sınırlarını tespit et
        // Son mailin body'sini al
        var mailBoundaries = new List<int>();
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].StartsWith("*From:*", StringComparison.OrdinalIgnoreCase) ||
                lines[i].StartsWith("From:", StringComparison.OrdinalIgnoreCase))
            {
                // Bir sonraki satırda "Sent:" varsa bu bir mail header
                if (i + 1 < lines.Count && (lines[i + 1].StartsWith("*Sent:*", StringComparison.OrdinalIgnoreCase) ||
                                              lines[i + 1].StartsWith("Sent:", StringComparison.OrdinalIgnoreCase)))
                {
                    mailBoundaries.Add(i);
                }
            }
        }

        if (mailBoundaries.Count == 0)
            return lines;

        // Son mail sınırı = ilk gönderilen mail
        var lastBoundary = mailBoundaries[^1];

        // Header'ları atla (From, Sent, To, Cc, Subject satırları)
        var bodyStart = lastBoundary;
        for (int i = lastBoundary; i < lines.Count && i < lastBoundary + 10; i++)
        {
            if (lines[i].StartsWith("*Subject:*", StringComparison.OrdinalIgnoreCase) ||
                lines[i].StartsWith("Subject:", StringComparison.OrdinalIgnoreCase))
            {
                bodyStart = i + 1;
                break;
            }
        }

        // Signature'a kadar olan kısmı al
        var bodyEnd = lines.Count;
        for (int i = bodyStart; i < lines.Count; i++)
        {
            if (lines[i].Contains("Saygılarımla", StringComparison.OrdinalIgnoreCase))
            {
                bodyEnd = i;
                break;
            }
        }

        return lines.Skip(bodyStart).Take(bodyEnd - bodyStart).ToList();
    }

    private static string ParseCustomerNameFromSubject(string subject)
    {
        // Pattern: "FW: HAVELSAN - Umut Kıyak" veya "HAVELSAN - Umut Kıyak"
        // "RE:" ve "FW:" prefix'lerini temizle
        var cleaned = Regex.Replace(subject, @"^(RE:\s*|FW:\s*|Fwd:\s*)+", "", RegexOptions.IgnoreCase).Trim();

        // " - " ile ayır, ilk kısım müşteri adı
        var dashIndex = cleaned.IndexOf(" - ", StringComparison.Ordinal);
        if (dashIndex > 0)
            return cleaned[..dashIndex].Trim();

        return cleaned;
    }

    private static EmlPassengerDto ParsePassenger(List<string> lines)
    {
        string fullName = string.Empty;
        string? phone = null;
        string? email = null;

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            // "Ad Soyad" satırından sonra ": Umut KIYAK"
            if (line.StartsWith("Ad Soyad", StringComparison.OrdinalIgnoreCase))
            {
                fullName = ExtractValueAfterColon(line, lines, i);
            }

            // Telefon: tek başına duran 05xx ile başlayan numara veya "Telefon" label'ı
            if (string.IsNullOrEmpty(phone) && Regex.IsMatch(line, @"^0[5]\d{9}$"))
            {
                phone = line;
            }

            // İletişim bilgilerindeki Eposta
            if (line.StartsWith("Eposta", StringComparison.OrdinalIgnoreCase) ||
                line.Equals("*Eposta*", StringComparison.OrdinalIgnoreCase))
            {
                var val = ExtractValueAfterColon(line, lines, i);
                if (!string.IsNullOrWhiteSpace(val) && val.Contains('@'))
                    email = val;
            }

            // İletişim bilgilerindeki Telefon
            if ((line.StartsWith("Telefon", StringComparison.OrdinalIgnoreCase) ||
                 line.Equals("*Telefon*", StringComparison.OrdinalIgnoreCase)) &&
                string.IsNullOrEmpty(phone))
            {
                var val = ExtractValueAfterColon(line, lines, i);
                if (!string.IsNullOrWhiteSpace(val))
                    phone = val.Replace(" ", "");
            }
        }

        return new EmlPassengerDto
        {
            FullName = fullName,
            Phone = phone,
            Email = email
        };
    }

    private static List<EmlTransferDto> ParseTransfers(List<string> lines)
    {
        // Gidiş ve Dönüş lokasyonlarını parse et
        var pickupAddresses = new List<string?>();
        var dropoffAddresses = new List<string?>();

        // Uçuş bilgileri
        var flightCodes = new List<string?>();
        var departureTimes = new List<string?>();
        var arrivalTimes = new List<string?>();
        var routes = new List<string?>();

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            // Alınış Lokasyonu - sıralı olarak gidiş ve dönüş gelir
            if (Regex.IsMatch(line, @"Al[ıi]n[ıi][sş] Lokasyonu", RegexOptions.IgnoreCase))
            {
                pickupAddresses.Add(NullIfEmpty(ExtractValueAfterColon(line, lines, i)));
            }

            if (Regex.IsMatch(line, @"B[ıi]rak[ıi]l[ıi][sş] Lokasyonu", RegexOptions.IgnoreCase))
            {
                dropoffAddresses.Add(NullIfEmpty(ExtractValueAfterColon(line, lines, i)));
            }

            if (Regex.IsMatch(line, @"U[çc]u[sş] Numaras[ıi]$", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(line, @"\*U[çc]u[sş] Numaras[ıi]\*$", RegexOptions.IgnoreCase))
            {
                flightCodes.Add(NullIfEmpty(ExtractValueAfterColon(line, lines, i)));
            }

            if (Regex.IsMatch(line, @"Kalk[ıi][sş] Saati", RegexOptions.IgnoreCase))
            {
                departureTimes.Add(NullIfEmpty(ExtractValueAfterColon(line, lines, i)));
            }

            if (Regex.IsMatch(line, @"Var[ıi][sş] Saati", RegexOptions.IgnoreCase))
            {
                arrivalTimes.Add(NullIfEmpty(ExtractValueAfterColon(line, lines, i)));
            }

            if (Regex.IsMatch(line, @"^Y[öo]n$", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(line, @"^\*Y[öo]n\*$", RegexOptions.IgnoreCase))
            {
                routes.Add(NullIfEmpty(ExtractValueAfterColon(line, lines, i)));
            }
        }

        var transfers = new List<EmlTransferDto>();

        // Gidiş transferi (index 0)
        var gidisTransfer = BuildTransfer("Gidiş",
            pickupAddresses.ElementAtOrDefault(0),
            dropoffAddresses.ElementAtOrDefault(0),
            flightCodes.ElementAtOrDefault(0),
            departureTimes.ElementAtOrDefault(0),
            arrivalTimes.ElementAtOrDefault(0),
            routes.ElementAtOrDefault(0));

        if (gidisTransfer != null)
            transfers.Add(gidisTransfer);

        // Dönüş transferi (index 1)
        var donusTransfer = BuildTransfer("Dönüş",
            pickupAddresses.ElementAtOrDefault(1),
            dropoffAddresses.ElementAtOrDefault(1),
            flightCodes.ElementAtOrDefault(1),
            departureTimes.ElementAtOrDefault(1),
            arrivalTimes.ElementAtOrDefault(1),
            routes.ElementAtOrDefault(1));

        if (donusTransfer != null)
            transfers.Add(donusTransfer);

        return transfers;
    }

    private static EmlTransferDto? BuildTransfer(string direction,
        string? pickup, string? dropoff,
        string? flightCode, string? departureTimeStr, string? arrivalTimeStr,
        string? route)
    {
        // Lokasyonlar boşsa job oluşturma
        if (string.IsNullOrWhiteSpace(pickup) && string.IsNullOrWhiteSpace(dropoff))
            return null;

        var (flightDate, departureTime) = ParseDateTime(departureTimeStr);
        var (_, arrivalTime) = ParseDateTime(arrivalTimeStr);

        // 01.01.1900 tarihli kayıtlar henüz belli değil demek
        if (flightDate.HasValue && flightDate.Value == InvalidDate)
            return null;

        return new EmlTransferDto
        {
            Direction = direction,
            PickupAddress = pickup,
            DropoffAddress = dropoff,
            FlightCode = flightCode,
            FlightDate = flightDate,
            FlightDepartureTime = departureTime,
            FlightArrivalTime = arrivalTime,
            FlightRoute = route
        };
    }

    private static (DateOnly? date, TimeOnly? time) ParseDateTime(string? dateTimeStr)
    {
        if (string.IsNullOrWhiteSpace(dateTimeStr))
            return (null, null);

        // Format: "19.01.2026 19:00"
        var parts = dateTimeStr.Trim().Split(' ', 2);

        DateOnly? date = null;
        TimeOnly? time = null;

        if (parts.Length >= 1 && DateOnly.TryParseExact(parts[0], "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            date = d;

        if (parts.Length >= 2 && TimeOnly.TryParseExact(parts[1], "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
            time = t;

        return (date, time);
    }

    private static string ExtractValueAfterColon(string currentLine, List<string> lines, int currentIndex)
    {
        // Değer aynı satırda ":" dan sonra olabilir
        var colonIndex = currentLine.IndexOf(':');
        if (colonIndex >= 0)
        {
            var value = currentLine[(colonIndex + 1)..].Trim();
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        // Değer sonraki satırlarda olabilir - ": değer" formatında
        for (int j = currentIndex + 1; j < lines.Count && j <= currentIndex + 3; j++)
        {
            var nextLine = lines[j].Trim();
            if (string.IsNullOrEmpty(nextLine))
                continue;

            if (nextLine.StartsWith(':'))
            {
                return nextLine[1..].Trim();
            }

            // Satır zaten bir label ise dur
            if (nextLine.StartsWith('*') || nextLine.StartsWith("Al") || nextLine.StartsWith("Bı") ||
                nextLine.StartsWith("Uç") || nextLine.StartsWith("Ha") || nextLine.StartsWith("Ka") ||
                nextLine.StartsWith("Va") || nextLine.StartsWith("Yö"))
                break;
        }

        return string.Empty;
    }

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static string StripHtml(string html)
    {
        // Basit HTML → text dönüşümü
        var text = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</?(p|div|tr|td|th|table|tbody|thead)[^>]*>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<[^>]+>", "", RegexOptions.Singleline);
        text = System.Net.WebUtility.HtmlDecode(text);
        return text;
    }
}

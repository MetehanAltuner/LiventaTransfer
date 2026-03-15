using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Application.Common;

public static class EnumLabelHelper
{
    public static string GetLabel(JobStatus status) => status switch
    {
        JobStatus.Open => "Açık",
        JobStatus.Assigned => "Atandı",
        JobStatus.InProgress => "Devam Ediyor",
        JobStatus.Completed => "Tamamlandı",
        JobStatus.PendingInvoice => "Fatura Bekliyor",
        JobStatus.Invoiced => "Faturalandı",
        JobStatus.Cancelled => "İptal Edildi",
        _ => status.ToString()
    };

    public static string GetLabel(JobType type) => type switch
    {
        JobType.Transfer => "Transfer",
        JobType.DailyAllocation => "Günlük Tahsis",
        _ => type.ToString()
    };

    public static string GetLabel(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Draft => "Taslak",
        InvoiceStatus.Sent => "Gönderildi",
        InvoiceStatus.Paid => "Ödendi",
        InvoiceStatus.Cancelled => "İptal Edildi",
        _ => status.ToString()
    };

    public static string GetLabel(RecipientType type) => type switch
    {
        RecipientType.Driver => "Şoför",
        RecipientType.Customer => "Müşteri",
        RecipientType.Coordinator => "Koordinatör",
        RecipientType.Accountant => "Muhasebeci",
        _ => type.ToString()
    };

    public static string GetLabel(NotificationChannel channel) => channel switch
    {
        NotificationChannel.WhatsApp => "WhatsApp",
        NotificationChannel.SMS => "SMS",
        NotificationChannel.Email => "E-posta",
        NotificationChannel.InApp => "Uygulama İçi",
        _ => channel.ToString()
    };
}

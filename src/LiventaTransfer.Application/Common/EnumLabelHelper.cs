using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Application.Common;

public static class EnumLabelHelper
{
    public static string GetLabel(JobStatus status) => status switch
    {
        JobStatus.Open => "İş Açıkta",
        JobStatus.Assigned => "Detay Gönderildi",
        JobStatus.InProgress => "Devam Ediyor",
        JobStatus.Completed => "Tamamlandı",
        JobStatus.PendingInvoice => "Fatura Kesilecek",
        JobStatus.Invoiced => "Fatura Kesildi",
        JobStatus.Merged => "Birleştirildi",
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

    public static string GetLabel(CustomerType type) => type switch
    {
        CustomerType.Corporate => "Kurumsal",
        CustomerType.Individual => "Bireysel",
        _ => type.ToString()
    };

    public static string GetLabel(LocationType type) => type switch
    {
        LocationType.Airport => "Havalimanı",
        LocationType.TrainStation => "Tren İstasyonu",
        LocationType.BusTerminal => "Otogar",
        LocationType.Hotel => "Otel",
        LocationType.Office => "Ofis",
        LocationType.Residence => "Konut",
        LocationType.Other => "Diğer",
        _ => type.ToString()
    };

    public static string GetLabel(VehicleType type) => type switch
    {
        VehicleType.Sedan => "Sedan",
        VehicleType.Vito => "Vito",
        VehicleType.Sprinter => "Sprinter",
        VehicleType.Minibus => "Minibüs",
        VehicleType.Bus => "Otobüs",
        _ => type.ToString()
    };

    public static string GetLabel(DriverStage stage) => stage switch
    {
        DriverStage.NotStarted => "Başlanmadı",
        DriverStage.Contacted => "Yolcu ile İletişime Geçildi",
        DriverStage.Departed => "Yola Çıkıldı",
        DriverStage.PickedUp => "Yolcu Alındı",
        DriverStage.DroppedOff => "Yolcu Bırakıldı",
        _ => stage.ToString()
    };

    public static string GetLabel(UserRole role) => role switch
    {
        UserRole.Operations => "Operasyon Personeli",
        UserRole.Reservation => "Rezervasyon Personeli",
        UserRole.Driver => "Şoför",
        UserRole.Manager => "Müdür",
        UserRole.GeneralManager => "Genel Müdür",
        UserRole.Accounting => "Muhasebe",
        UserRole.Developer => "Developer",
        _ => role.ToString()
    };
}

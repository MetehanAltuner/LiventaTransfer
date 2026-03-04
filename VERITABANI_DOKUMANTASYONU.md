# Liventa Transfer - Veritabanı ve Sistem Dokümantasyonu

> **Turizm ERP Transfer Yönetim Sistemi**
> Veritabanı: PostgreSQL | Uygulama: .NET 10 + Entity Framework Core 9
> Tarih: Mart 2026

---

## İçindekiler

1. [Genel Bakış](#1-genel-bakış)
2. [İş Akışı ve Roller](#2-iş-akışı-ve-roller)
3. [Durum Yönetimi ve Renk Kodları](#3-durum-yönetimi-ve-renk-kodları)
4. [Excel ↔ Veritabanı Eşleştirmesi](#4-excel--veritabanı-eşleştirmesi)
5. [Veritabanı Şeması](#5-veritabanı-şeması)
6. [Tablo Detayları](#6-tablo-detayları)
   - [6.1 Branches (Şubeler)](#61-branches-şubeler)
   - [6.2 Users (Kullanıcılar)](#62-users-kullanıcılar)
   - [6.3 Customers (Müşteriler / Firmalar)](#63-customers-müşteriler--firmalar)
   - [6.4 Passengers (Yolcular)](#64-passengers-yolcular)
   - [6.5 VehicleOwners (Araç Sahipleri)](#65-vehicleowners-araç-sahipleri)
   - [6.6 Vehicles (Araçlar)](#66-vehicles-araçlar)
   - [6.7 Drivers (Şoförler)](#67-drivers-şoförler)
   - [6.8 Locations (Lokasyonlar)](#68-locations-lokasyonlar)
   - [6.9 Jobs (İşler / Transferler)](#69-jobs-işler--transferler)
   - [6.10 JobStatusHistories (İş Durum Geçmişi)](#610-jobstatushistories-iş-durum-geçmişi)
   - [6.11 TripLogs (Sefer Kayıtları)](#611-triplogs-sefer-kayıtları)
   - [6.12 JobNotes (İş Notları)](#612-jobnotes-iş-notları)
   - [6.13 Invoices (Faturalar)](#613-invoices-faturalar)
   - [6.14 InvoiceItems (Fatura Kalemleri)](#614-invoiceitems-fatura-kalemleri)
   - [6.15 Notifications (Bildirimler)](#615-notifications-bildirimler)
7. [Enum Değerleri](#7-enum-değerleri)
8. [İlişki Diyagramı](#8-ilişki-diyagramı)
9. [İndeksler ve Performans](#9-indeksler-ve-performans)
10. [Teknik Notlar](#10-teknik-notlar)

---

## 1. Genel Bakış

**Liventa Transfer**, turizm sektöründe faaliyet gösteren bir VIP transfer ve tahsisli araç yönetim sistemidir. Sistem, müşterilerden gelen transfer taleplerinin e-posta ile alınmasından, şoföre atamanın yapılmasına, seferin gerçekleştirilmesine ve faturanın kesilmesine kadar tüm süreçleri dijital ortamda yönetmeyi hedefler.

### Mevcut Durum (Excel Tabanlı Çalışma)

Şu anda operasyon, aylık Excel dosyaları üzerinden yürütülmektedir. Ocak 2026 dosyasında **488 aktif transfer kaydı** bulunmakta olup, her satır tek bir transfer işini temsil etmektedir. Bu Excel dosyasındaki sütunlar:

| Excel Sütunu | Açıklama | Örnek |
|---|---|---|
| **Tarih** | Transferin yapılacağı gün | 1/3/26 |
| **Saat** | Transfer saati | 06:45 |
| **Araç Sahibi** | Aracı sağlayan firma/kişi | Ertur, İnternasyonel, DC Grup |
| **Firma** | Hizmet verilen müşteri firma | Havelsan Tatil Sepeti, Meteksan |
| **Araç Plaka** | Kullanılacak araç | AE 5045, Sprinter, Vito |
| **Kaptan** | Atanan şoför | Çetin Aktaş, Atilla Yiğit |
| **Açıklama** | Güzergah (Alış - Bırakış noktası) | Batıkent - ESB |
| **EK BİLGİ** | Yolcu ad/soyad ve/veya kişi sayısı | Kamil KAHVECİ, 9 Kişi |

> **Not:** Excel'de **40 farklı müşteri firması**, **14 farklı araç sahibi**, **11 farklı araç plakası** ve **53 farklı şoför** tespit edilmiştir.

---

## 2. İş Akışı ve Roller

### 2.1 Süreç Akışı

```
┌─────────────┐     ┌──────────────────┐     ┌───────────────────┐     ┌──────────────┐     ┌──────────────┐
│  E-POSTA    │────▶│  REZERVASYONCU   │────▶│  KOORDİNATÖR      │────▶│    ŞOFÖR     │────▶│  MUHASEBECİ  │
│  (Talep)    │     │  (Excel/Sisteme  │     │  (Alperen)        │     │  (Sefer)     │     │  (Fatura)    │
│             │     │   giriş yapar)   │     │  (Atama & Fiyat)  │     │              │     │              │
└─────────────┘     └──────────────────┘     └───────────────────┘     └──────────────┘     └──────────────┘
```

### 2.2 Roller ve Sorumlulukları

| Rol | Sistem Karşılığı | Görevleri |
|---|---|---|
| **Rezervasyoncu** | `Reservationist (4)` | Müşteriden gelen e-postayı okur, transfer detaylarını sisteme/Excel'e girer. Tarih, saat, güzergah, yolcu bilgileri, uçuş kodu gibi bilgileri kaydeder. |
| **Koordinatör (Alperen)** | `Coordinator (3)` / `Manager (2)` | Karar verici roldür. Hangi şoförün hangi aracı süreceğini belirler. Alış ve satış fiyatını girer. Araç koordinasyonunu yapar. Değişiklikleri yönetir. Şoföre ve araç sahibine bilgi gönderir (1 gün önceden). |
| **Şoför** | `Driver (5)` | Transfer gününden 1 gün önce bilgilendirilir. Uçuş kodundan uçağın rötar durumunu kontrol eder. Yolcuyu alır ve bırakır. **Transfer** tipinde: Aldım/bıraktım bilgisi yeterlidir. **Tahsis** tipinde: Alış saati, bırakış saati, yapılan km bilgisini raporlar. Koordinatöre (Alperen) geri bildirim verir. |
| **Muhasebeci** | `Accountant (6)` | İş tamamlandığında (gitti-geldi bitmiş durumda) koordinatörden tüm detayları alır. Fatura kesim işlemlerini yapar. |
| **Yönetici** | `Admin (1)` | Sistem yönetimi, kullanıcı tanımlama, şube yönetimi. |
| **İzleyici** | `Viewer (7)` | Sadece okuma yetkisi olan kullanıcı. |

### 2.3 İş Türleri

| İş Türü | Enum | Açıklama | Şoför Raporu |
|---|---|---|---|
| **Transfer** | `Transfer (1)` | Noktadan noktaya tek seferlik taşıma (örn: Havalimanı → Otel). Gidiş-dönüş bilgisi açıklama alanından çıkartılır. | Aldım / Bıraktım yeterli |
| **Günlük Tahsis** | `DailyAllocation (2)` | Gün boyu bir müşteriye araç tahsis edilmesi. Detaylı saat ve km bilgisi gerekir. | Alış saati, bırakış saati, toplam km |

### 2.4 Önemli İş Kuralları

- **İşler değişkendir:** Bugün girilen bir iş, ertesi sabaha kadar değişebilir (saat, güzergah, araç, şoför değişikliği).
- **Ertur haricinde başka araçlar da var:** Araç filosu sadece şirket araçlarından ibaret değil; dış tedarikçilerden de araç temin edilir (İnternasyonel, DC Grup, Fibi, Ankaragrup vb.).
- **Müşteri bilgileri saklanır:** Firma bazlı müşteri takibi yapılır.
- **Alış-satış fiyatı takibi:** Her iş için hem müşteriye satış fiyatı hem de araç sahibine/şoföre ödenen alış fiyatı tutulur.
- **WhatsApp entegrasyonu:** Şoförlere ve araç sahiplerine WhatsApp üzerinden bildirim gönderimi planlanmaktadır.

---

## 3. Durum Yönetimi ve Renk Kodları

İşlerin yaşam döngüsü aşağıdaki durumlardan oluşur. Her durum, Excel'deki satır rengine karşılık gelir:

| Renk | Durum | Enum Değeri | Açıklama |
|---|---|---|---|
| ⬜ **Beyaz** | Açık | `Open (1)` | İş henüz açıkta, atama yapılmamış. Rezervasyoncu tarafından girilmiş ancak koordinatör henüz aksiyona geçmemiş. |
| 🟩 **Yeşil** | Atandı | `Assigned (2)` | Şoföre ve/veya araç sahibine detay bilgisi iletildi. Araç, şoför ve fiyat belirlenmiş durumda. |
| 🔵 **-** | Devam Ediyor | `InProgress (3)` | Şoför yolcuyu almış, sefer devam ediyor. |
| 🟨 **Sarı** | Tamamlandı | `Completed (4)` | Transfer/tahsis tamamlandı. Şoför geri bildirimini verdi. İş bitmiş ama henüz faturalanmamış. |
| 🟥 **Kırmızı** | Fatura Bekliyor | `PendingInvoice (5)` | Muhasebeci tarafından fatura kesilecek durumda. Tüm detaylar hazır. |
| 🔵 **Mavi** | Faturalandı | `Invoiced (6)` | Fatura kesilmiş, iş kapatılmış. |
| ⚫ **-** | İptal | `Cancelled (99)` | İş iptal edildi. |

### Durum Geçiş Akışı

```
Açık (Beyaz) ──▶ Atandı (Yeşil) ──▶ Devam Ediyor ──▶ Tamamlandı (Sarı) ──▶ Fatura Bekliyor (Kırmızı) ──▶ Faturalandı (Mavi)
   │                   │                                      │
   └──── İptal ◀───────┴──────────────────────────────────────┘
```

> Her durum değişikliği `JobStatusHistories` tablosunda kayıt altına alınır: Kim değiştirdi, ne zaman değiştirdi, eski/yeni durum ve varsa değişiklik sebebi.

---

## 4. Excel ↔ Veritabanı Eşleştirmesi

Mevcut Excel dosyasındaki her sütunun veritabanında nasıl karşılandığı:

| Excel Sütunu | DB Tablosu | DB Alanı | Açıklama |
|---|---|---|---|
| **Tarih** | `Jobs` | `JobDate` (date) | Transfer tarihi. Excel'de "1/3/26" formatında → DB'de `DateOnly` |
| **Saat** | `Jobs` | `JobTime` (time) | Transfer saati. Excel'de ondalık → DB'de `TimeOnly` |
| **Araç Sahibi** | `VehicleOwners` | `Name` | Araç sahibi firma/kişi adı. FK: `Jobs.VehicleOwnerId` |
| **Firma** | `Customers` | `Name` | Hizmet verilen müşteri firma. FK: `Jobs.CustomerId` |
| **Araç Plaka** | `Vehicles` | `Plate` + `VehicleType` | Araç plakası veya tipi. FK: `Jobs.VehicleId` |
| **Kaptan** | `Drivers` | `FullName` | Atanan şoför. FK: `Jobs.DriverId` |
| **Açıklama** | `Jobs` | `PickupLocationId` + `DropoffLocationId` + `PickupAddress` + `DropoffAddress` + `RouteDescription` | Güzergah bilgisi. "Batıkent - ESB" → Alış ve bırakış lokasyonlarına ayrıştırılır |
| **EK BİLGİ** | `Passengers` + `Jobs` | `Passenger.FullName` + `Jobs.PassengerCount` + `Jobs.ExtraInfo` | Yolcu adı ve kişi sayısı. Birden fazla yolcu varsa tirelerle ayrılır |
| *(Yok - Excel'de)* | `Jobs` | `SalePrice` | Müşteriye satış fiyatı |
| *(Yok - Excel'de)* | `Jobs` | `PurchasePrice` | Araç sahibine alış fiyatı |
| *(Yok - Excel'de)* | `Jobs` | `ExtraCost` | Ekstra maliyet |
| *(Yok - Excel'de)* | `Jobs` | `FlightCode` | Uçuş kodu (şoförün rötar kontrolü için) |
| *(Yok - Excel'de)* | `Jobs` | `Status` | Satır rengi (Beyaz/Yeşil/Sarı/Kırmızı/Mavi) |
| *(Yok - Excel'de)* | `Jobs` | `Notes` | Ekstra not alanı |

### Excel'de Olmayıp Sistemde Olan Önemli Alanlar

Veritabanı, Excel'deki basit yapının ötesinde aşağıdaki kritik bilgileri de takip eder:

- **Alış/Satış Fiyatı:** Her iş için müşteriye yapılan satış ve tedarikçiye ödenen alış fiyatı
- **Uçuş Kodu:** Şoförün havalimanı transferlerinde rötar kontrolü yapabilmesi için
- **Durum Takibi:** İşin hangi aşamada olduğu (renk kodlaması)
- **Sefer Logları:** Şoförün alış/bırakış saati, km bilgisi
- **Fatura Yönetimi:** Dönemsel faturalama
- **Bildirim Takibi:** WhatsApp/SMS gönderim kayıtları
- **Değişiklik Geçmişi:** Her durum değişikliğinin kaydı

---

## 5. Veritabanı Şeması

```
┌──────────────┐       ┌──────────────┐
│   Branches   │◀──────│    Users     │
│ (Şubeler)    │ 1───N │ (Kullanıcılar│
└──────────────┘       └──────┬───────┘
                              │ CreatedBy / AssignedBy
                              ▼
┌──────────────┐       ┌──────────────┐       ┌──────────────┐
│  Customers   │◀──────│    Jobs      │──────▶│  Locations   │
│ (Müşteriler) │ 1───N │ (İşler)     │ N───1 │ (Lokasyonlar)│
└──────┬───────┘       └──────┬───────┘       └──────────────┘
       │ 1───N                │
       ▼                      │ N───1          ┌──────────────┐
┌──────────────┐              ├───────────────▶│VehicleOwners │
│  Passengers  │              │                │(Araç Sahipleri│
│ (Yolcular)   │              │                └──────┬───────┘
└──────────────┘              │                       │ 1───N
                              │ N───1          ┌──────┴───────┐
                              ├───────────────▶│  Vehicles    │
                              │                │ (Araçlar)    │
                              │ N───1          └──────────────┘
                              ├───────────────▶┌──────────────┐
                              │                │   Drivers    │
                              │                │ (Şoförler)   │
                              │                └──────────────┘
                         ┌────┼────┬─────┐
                         ▼    ▼    ▼     ▼
                    ┌────────┐│┌───────┐┌──────────┐
                    │JobNotes│││TripLog││JobStatus  │
                    │(Notlar)│││(Sefer)││Histories  │
                    └────────┘│└───────┘│(Geçmiş)  │
                              │         └──────────┘
                         ┌────┘
                         ▼
                    ┌──────────┐     ┌──────────────┐
                    │ Invoices │────▶│ InvoiceItems │
                    │(Faturalar│ 1─N │(Fatura Kalem)│
                    └──────────┘     └──────────────┘

                    ┌──────────────┐
                    │Notifications │ (Bildirimler)
                    │ Job? + User? │
                    └──────────────┘
```

---

## 6. Tablo Detayları

> **Ortak Alanlar (BaseEntity):** Tüm tablolarda aşağıdaki alanlar bulunur:
>
> | Alan | Tip | Açıklama |
> |---|---|---|
> | `Id` | UUID | Benzersiz kayıt kimliği (otomatik üretilir) |
> | `CreatedAt` | timestamp with time zone | Kaydın oluşturulma zamanı |
> | `UpdatedAt` | timestamp with time zone | Kaydın son güncellenme zamanı |
> | `IsDeleted` | boolean | Soft delete (silinen kayıtlar false yerine bu alanla işaretlenir, veri kaybı yaşanmaz) |

---

### 6.1 Branches (Şubeler)

**Amaç:** Firmanın farklı şubelerini tanımlar. Her kullanıcı bir şubeye bağlıdır. Çok şubeli yapıda veri izolasyonu sağlar.

| Alan | Tip | Zorunlu | Açıklama |
|---|---|---|---|
| `Name` | varchar(200) | ✅ | Şube adı (örn: "Ankara Merkez", "İstanbul Ofis") |
| `Address` | varchar(500) | ❌ | Şubenin fiziksel adresi |
| `IsActive` | boolean | ✅ | Şubenin aktif olup olmadığı (varsayılan: true). Pasif şubeler listelemelerde görünmez |

**İş Mantığı:** Sistemde şu an 1 şube tanımlıdır. İlerleyen dönemde farklı şehirlerde operasyon açılması durumunda kullanılacaktır. Kullanıcılar şubeye bağlı çalışır; böylece her şube kendi operasyonunu görebilir.

---

### 6.2 Users (Kullanıcılar)

**Amaç:** Sistemi kullanan tüm personeli tanımlar. Her kullanıcının bir rolü vardır ve bu rol, kullanıcının sistemdeki yetkilerini belirler.

| Alan | Tip | Zorunlu | Açıklama |
|---|---|---|---|
| `Username` | varchar(100) | ✅ | Giriş için kullanılan benzersiz kullanıcı adı |
| `FirstName` | varchar(100) | ✅ | Ad |
| `LastName` | varchar(100) | ✅ | Soyad |
| `PasswordHash` | text | ✅ | Şifrelenmiş parola (düz metin olarak saklanmaz) |
| `Role` | integer | ✅ | Kullanıcı rolü (UserRole enum) |
| `BranchId` | UUID (FK → Branches) | ✅ | Kullanıcının bağlı olduğu şube |
| `IsActive` | boolean | ✅ | Aktif/pasif durumu (varsayılan: true) |

**İş Mantığı:**
- **Rezervasyoncu** maili okuyup sisteme giriş yapar → `CreatedByUserId` olarak Jobs tablosunda iz bırakır.
- **Koordinatör (Alperen)** atama yapar → `AssignedByUserId` olarak Jobs tablosunda iz bırakır.
- **Şoför** kullanıcısı, sefer loglarını ve bildirim alıcısı olarak kullanılır.
- **Muhasebeci** fatura kesim işlemlerini yürütür.
- Kullanıcı adı (`Username`) benzersizdir (unique index).

**Unique Index:** `IX_Users_Username` → Username

---

### 6.3 Customers (Müşteriler / Firmalar)

**Amaç:** Transfer hizmeti verilen müşteri firmaları ve bireysel müşterileri tanımlar. Excel'deki "Firma" sütununa karşılık gelir.

| Alan | Tip | Zorunlu | Açıklama |
|---|---|---|---|
| `Name` | varchar(300) | ✅ | Müşteri/firma adı (örn: "Havelsan Tatil Sepeti", "Meteksan", "Buz Hokeyi Federasyonu") |
| `CustomerType` | integer | ✅ | Müşteri tipi: Kurumsal (1) veya Bireysel (2) |
| `TaxNumber` | varchar(20) | ❌ | Vergi numarası (kurumsal müşteriler için, benzersiz) |
| `TaxOffice` | varchar(200) | ❌ | Vergi dairesi adı |
| `TcKimlikNo` | varchar(11) | ❌ | T.C. Kimlik No (bireysel müşteriler için) |
| `Phone` | varchar(20) | ❌ | İletişim telefon numarası |
| `Email` | varchar(200) | ❌ | E-posta adresi |
| `Address` | varchar(500) | ❌ | Adres bilgisi |
| `Notes` | varchar(2000) | ❌ | Müşteriyle ilgili genel notlar |
| `IsActive` | boolean | ✅ | Aktif/pasif durumu |

**İş Mantığı:**
- Excel'deki mevcut veriye göre **40 farklı müşteri firması** bulunmaktadır.
- Örnek firmalar: Havelsan Tatil Sepeti, HTR Sanayi Tatil Sepeti, SSB Tatil Sepeti, Kızılırmak Elektrik Üretim, Buz Hokeyi Federasyonu, Meteksan, ULAQ, Lidya Madencilik, FCC Travel vb.
- `CustomerType` alanı, Kurumsal müşteriler için `TaxNumber`/`TaxOffice`, bireysel müşteriler için `TcKimlikNo` kullanılmasını sağlar.
- Müşteri bilgileri saklanır ve tekrarlı girişlerde referans olarak kullanılır.

**Unique Index:** `IX_Customers_TaxNumber` → TaxNumber (WHERE TaxNumber IS NOT NULL)

---

### 6.4 Passengers (Yolcular)

**Amaç:** Transfer edilecek yolcuların bilgilerini tutar. Her yolcu bir müşteri firmasına bağlıdır. Excel'deki "EK BİLGİ" sütunundaki isim bilgilerine karşılık gelir.

| Alan | Tip | Zorunlu | Açıklama |
|---|---|---|---|
| `FullName` | varchar(200) | ✅ | Yolcunun adı soyadı (örn: "Kamil KAHVECİ", "ANDRII KICHA") |
| `Phone` | varchar(20) | ❌ | Yolcu telefon numarası |
| `Email` | varchar(200) | ❌ | Yolcu e-posta adresi |
| `Notes` | varchar(1000) | ❌ | Yolcuyla ilgili özel notlar (VIP tercihleri, alerjiler vb.) |
| `CustomerId` | UUID (FK → Customers) | ✅ | Yolcunun bağlı olduğu müşteri firma |
| `IsActive` | boolean | ✅ | Aktif/pasif durumu |

**İş Mantığı:**
- Excel'deki EK BİLGİ sütununda yolcu isimleri yer alır: "Kamil KAHVECİ", "Hakkı SOYDAN - Serkan TAŞKAZAN - Mehmet Ersan KAYKUSUZ" gibi.
- Birden fazla yolcu tire (-) ile ayrılır. Sistemde her biri ayrı `Passenger` kaydı olarak tutulur.
- Yolcu sayısı Jobs tablosundaki `PassengerCount` alanında tutulur.
- Yolcular müşteri firmasına bağlıdır; böylece "Havelsan'ın yolcuları", "Meteksan'ın yolcuları" şeklinde gruplanabilir.

---

### 6.5 VehicleOwners (Araç Sahipleri)

**Amaç:** Araçları sağlayan firma ve kişileri tanımlar. Kendi filosu (Ertur) ve dış tedarikçiler (İnternasyonel, DC Grup vb.) bu tabloda yer alır. Excel'deki "Araç Sahibi" sütununa karşılık gelir.

| Alan | Tip | Zorunlu | Açıklama |
|---|---|---|---|
| `Name` | varchar(200) | ✅ | Araç sahibi firma/kişi adı (örn: "Ertur", "İnternasyonel", "DC Grup") |
| `IsOwnFleet` | boolean | ✅ | Kendi filosu mu? (true: şirket aracı, false: dış tedarikçi) |
| `ContactPerson` | varchar(200) | ❌ | İrtibat kişisi adı |
| `Phone` | varchar(20) | ❌ | İletişim telefonu |
| `Email` | varchar(200) | ❌ | E-posta |
| `Notes` | varchar(1000) | ❌ | Araç sahibiyle ilgili notlar |
| `IsActive` | boolean | ✅ | Aktif/pasif durumu |

**İş Mantığı:**
- Veritabanında halihazırda **5 araç sahibi** tanımlıdır.
- Excel verilerine göre **14 farklı araç sahibi** mevcuttur:
  - **Kendi filosu:** Ertur (IsOwnFleet = true)
  - **Dış tedarikçiler:** İnternasyonel, DC Grup, Fibi, Ankaragrup, Netur, Muzaffer Yıldız, İsmail Karaaslan, Kemal Teke, Muhammed Demirci, Burak Yurttutan, Bekir Yıldırım, Sabri Kıraç
- `IsOwnFleet` alanı, şirketin kendi aracı ile dışarıdan kiralanan araçları ayırt etmek için kullanılır. Bu ayrım, maliyet hesaplamasında kritiktir.
- Koordinatör (Alperen), araç sahibine WhatsApp ile bilgi gönderir.

---

### 6.6 Vehicles (Araçlar)

**Amaç:** Kullanılabilecek araçların envanterini tutar. Her araç bir araç sahibine bağlıdır. Excel'deki "Araç Plaka" sütununa karşılık gelir.

| Alan | Tip | Zorunlu | Açıklama |
|---|---|---|---|
| `Plate` | varchar(20) | ✅ | Araç plakası (örn: "AE 5045", "AE 5047", "KT 5050") |
| `VehicleType` | integer | ✅ | Araç tipi (VehicleType enum) |
| `Brand` | varchar(100) | ❌ | Marka (örn: Mercedes, Volkswagen) |
| `Model` | varchar(100) | ❌ | Model (örn: Vito, Sprinter) |
| `Year` | integer | ❌ | Model yılı |
| `Capacity` | integer | ✅ | Yolcu kapasitesi (varsayılan: 4) |
| `VehicleOwnerId` | UUID (FK → VehicleOwners) | ✅ | Aracın sahibi |
| `IsActive` | boolean | ✅ | Aktif/pasif durumu |

**İş Mantığı:**
- Excel'de bazı araçlar plaka ile (AE 5045, AE 5047, KT 5050), bazıları ise tür ile (Sprinter, Vito, Binek) belirtilmiştir.
- Araç tipleri operasyondaki ihtiyaca göre belirlenir:
  - **Sedan (1):** Binek araç, 1-3 kişilik transferler
  - **Vito (2):** 4-7 kişilik VIP transferler
  - **Sprinter (3):** 8-15 kişilik grup transferleri (örn: 9 kişilik Buz Hokeyi Federasyonu transferi)
  - **Minibus (4):** 15-25 kişilik taşımalar
  - **Bus (5):** 25+ kişilik büyük grup taşımaları
- `Capacity` alanı, iş atamasında yolcu sayısı ile araç uyumluluğunu kontrol etmek için kullanılır.

---

### 6.7 Drivers (Şoförler)

**Amaç:** Transfer yapan şoförlerin bilgilerini tutar. Her şoför bir araç sahibine bağlıdır ve varsayılan bir aracı olabilir. Excel'deki "Kaptan" sütununa karşılık gelir.

| Alan | Tip | Zorunlu | Açıklama |
|---|---|---|---|
| `FullName` | varchar(200) | ✅ | Şoför adı soyadı (örn: "Çetin Aktaş", "Atilla Yiğit") |
| `Phone` | varchar(20) | ✅ | Telefon numarası (zorunlu - iletişim için kritik) |
| `WhatsAppPhone` | varchar(20) | ❌ | WhatsApp numarası (bildirim gönderimi için) |
| `LicenseNumber` | varchar(50) | ❌ | Ehliyet numarası |
| `VehicleOwnerId` | UUID (FK → VehicleOwners) | ✅ | Şoförün bağlı olduğu araç sahibi/firma |
| `DefaultVehicleId` | UUID (FK → Vehicles) | ❌ | Şoförün varsayılan olarak kullandığı araç |
| `IsActive` | boolean | ✅ | Aktif/pasif durumu |

**İş Mantığı:**
- Excel verilerine göre **53 farklı şoför** bulunmaktadır.
- `WhatsAppPhone` alanı, WhatsApp entegrasyonu için kritiktir. Şoföre 1 gün önceden transfer detayları WhatsApp ile gönderilir.
- `DefaultVehicleId` alanı, şoförün genelde kullandığı aracı belirtir; ancak her iş için farklı araç atanabilir (Jobs tablosundaki VehicleId).
- Şoför, uçuş kodundan uçak bilgisini (rötar, iptal) kontrol eder.
- Şoför süreci: Bilgilendirilme → Uçuş kontrolü → Yolcuyu alma → Yolcuyu bırakma → Koordinatöre geri bildirim.
- `VehicleOwnerId` ilişkisi sayesinde, hangi araç sahibinin hangi şoförleri olduğu takip edilir.

---

### 6.8 Locations (Lokasyonlar)

**Amaç:** Sık kullanılan alış ve bırakış noktalarını tanımlar. Havalimanları, oteller, ofisler gibi lokasyonlar burada tutulur. Excel'deki "Açıklama" sütunundaki güzergah noktalarına karşılık gelir.

| Alan | Tip | Zorunlu | Açıklama |
|---|---|---|---|
| `Name` | varchar(200) | ✅ | Lokasyon adı (örn: "ESB", "SAW", "IST", "Batıkent") |
| `ShortCode` | varchar(20) | ❌ | Kısa kod (örn: "ESB" → Esenboğa Havalimanı) |
| `Address` | varchar(500) | ❌ | Detaylı adres |
| `Latitude` | numeric(9,6) | ❌ | Enlem koordinatı |
| `Longitude` | numeric(9,6) | ❌ | Boylam koordinatı |
| `LocationType` | integer | ✅ | Lokasyon tipi (LocationType enum) |
| `IsActive` | boolean | ✅ | Aktif/pasif durumu |

**İş Mantığı:**
- Veritabanında halihazırda **6 lokasyon** tanımlıdır.
- Excel verilerindeki yaygın lokasyonlar: ESB (Esenboğa Havalimanı), SAW (Sabiha Gökçen), IST (İstanbul Havalimanı), ADB (Adnan Menderes), Batıkent, Eryaman, Çiğdem, Gölbaşı Aselsan, Keçiören, Bağlıca, Yaşamkent vb.
- Lokasyon tipleri:
  - **Airport (1):** Havalimanları (ESB, SAW, IST, ADB) - uçuş kodu kontrolü burada önemlidir
  - **Hotel (4):** Otel transferleri
  - **Office (5):** Ofis/fabrika transferleri (Aselsan, Meteksan, ULAQ ofisleri)
  - **Residence (6):** Ev/site adresleri (Batıkent, Eryaman, Çiğdem)
- Excel'deki "Açıklama" sütunundan (örn: "Batıkent - ESB") alış ve bırakış noktaları ayrıştırılarak `PickupLocation` ve `DropoffLocation` olarak kaydedilir.
- Koordinat bilgileri (Latitude/Longitude) ilerleyen dönemde harita entegrasyonu için kullanılabilir.

---

### 6.9 Jobs (İşler / Transferler)

**Amaç:** Sistemin **ana tablosu**. Her bir transfer veya tahsis işini temsil eder. Excel'deki her satır, bu tabloda bir kayda karşılık gelir. 29 sütun ile en kapsamlı tablodur.

| Alan | Tip | Zorunlu | Açıklama |
|---|---|---|---|
| `JobNumber` | varchar(50) | ✅ | Benzersiz iş numarası (otomatik üretilir, örn: "TRN-2026-001") |
| `JobDate` | date | ✅ | Transfer tarihi (Excel: "Tarih" sütunu) |
| `JobTime` | time | ✅ | Transfer saati (Excel: "Saat" sütunu) |
| `JobType` | integer | ✅ | İş tipi: Transfer (1) veya Günlük Tahsis (2) |
| `Status` | integer | ✅ | İşin mevcut durumu (JobStatus enum - renk koduna karşılık gelir) |
| `CustomerId` | UUID (FK → Customers) | ✅ | Müşteri firma (Excel: "Firma" sütunu) |
| `PassengerId` | UUID (FK → Passengers) | ❌ | Ana yolcu (Excel: "EK BİLGİ" sütunundaki ilk isim) |
| `PassengerCount` | integer | ✅ | Yolcu sayısı (varsayılan: 1, Excel: "9 Kişi" gibi bilgiler) |
| `PickupLocationId` | UUID (FK → Locations) | ❌ | Alış lokasyonu (Excel: "Açıklama" sütunundan - ilk nokta) |
| `DropoffLocationId` | UUID (FK → Locations) | ❌ | Bırakış lokasyonu (Excel: "Açıklama" sütunundan - ikinci nokta) |
| `PickupAddress` | varchar(500) | ❌ | Alış adresi (lokasyon tanımlı değilse serbest metin) |
| `DropoffAddress` | varchar(500) | ❌ | Bırakış adresi (lokasyon tanımlı değilse serbest metin) |
| `RouteDescription` | varchar(1000) | ❌ | Güzergah açıklaması (Excel: "Açıklama" sütununun ham hali, örn: "2 Alacaatlı - Yaşamkent - Bağlıca - ESB") |
| `FlightCode` | varchar(20) | ❌ | Uçuş kodu (şoförün uçak rötar/iptal kontrolü için, örn: "TK2134") |
| `ExtraInfo` | varchar(2000) | ❌ | Ek bilgi (Excel: "EK BİLGİ" sütununun ham hali) |
| `Notes` | varchar(2000) | ❌ | İşle ilgili dahili notlar (ekstra not alanı ihtiyacı) |
| `SourceEmail` | varchar(500) | ❌ | Talebin geldiği e-posta referansı |
| `VehicleOwnerId` | UUID (FK → VehicleOwners) | ❌ | Araç sahibi (Excel: "Araç Sahibi" sütunu) |
| `VehicleId` | UUID (FK → Vehicles) | ❌ | Atanan araç (Excel: "Araç Plaka" sütunu) |
| `DriverId` | UUID (FK → Drivers) | ❌ | Atanan şoför (Excel: "Kaptan" sütunu) |
| `SalePrice` | numeric(18,2) | ❌ | **Satış fiyatı** - müşteriye fatura edilecek tutar (TL) |
| `PurchasePrice` | numeric(18,2) | ❌ | **Alış fiyatı** - araç sahibine/şoföre ödenecek tutar (TL) |
| `ExtraCost` | numeric(18,2) | ❌ | Ekstra maliyet (otopark, köprü geçişi, bekleme ücreti vb.) |
| `CreatedByUserId` | UUID (FK → Users) | ✅ | Kaydı oluşturan kullanıcı (genelde Rezervasyoncu) |
| `AssignedByUserId` | UUID (FK → Users) | ❌ | Atamayı yapan kullanıcı (genelde Koordinatör/Alperen) |

**İş Mantığı:**
- **Ana tablo** - sistemin kalbidir. Excel'deki her satır burada bir kayıttır.
- `JobNumber` benzersizdir ve otomatik oluşturulur.
- `JobDate` üzerinde index vardır; tarihe göre sorgulama performansı kritiktir (günlük/haftalık/aylık listeleme).
- `Status` üzerinde index vardır; duruma göre filtreleme sık kullanılır (açık işler, fatura bekleyenler vb.).
- **Alış/Satış fiyatı** ayrımı, kâr/zarar analizini mümkün kılar: `SalePrice - PurchasePrice - ExtraCost = Kâr`.
- İşler değişkendir: Bugün girilen iş yarın sabah değişebilir. Bu yüzden `JobStatusHistories` ile her değişiklik kayıt altına alınır.
- `SourceEmail` alanı, talebin hangi e-postadan geldiğini referans tutar.
- İş, `CreatedByUserId` (Rezervasyoncu) tarafından oluşturulur, `AssignedByUserId` (Koordinatör) tarafından şoför/araç ataması yapılır.

**İndeksler:**
- `IX_Jobs_JobNumber` (UNIQUE) - İş numarasına göre hızlı arama
- `IX_Jobs_JobDate` - Tarihe göre listeleme
- `IX_Jobs_Status` - Duruma göre filtreleme
- Tüm FK alanlarında indeks mevcuttur

---

### 6.10 JobStatusHistories (İş Durum Geçmişi)

**Amaç:** Her işteki durum değişikliklerinin tam geçmişini tutar. Denetim izi (audit trail) sağlar. Kim, ne zaman, neden değiştirdi bilgisini kayıt altına alır.

| Alan | Tip | Zorunlu | Açıklama |
|---|---|---|---|
| `JobId` | UUID (FK → Jobs) | ✅ | İlgili iş |
| `OldStatus` | integer | ❌ | Eski durum (ilk oluşturmada null olabilir) |
| `NewStatus` | integer | ✅ | Yeni durum |
| `ChangedByUserId` | UUID (FK → Users) | ✅ | Değişikliği yapan kullanıcı |
| `ChangeReason` | varchar(500) | ❌ | Değişiklik sebebi (örn: "Müşteri iptal istedi", "Uçuş rötar yaptı") |
| `ChangedAt` | timestamp with time zone | ✅ | Değişiklik zamanı |

**İş Mantığı:**
- İşler sabit olmadığı için (bugün girilen iş sabaha değişebilir) her değişiklik burada kayıt altındadır.
- Örnek geçmiş: Open → Assigned (Alperen atama yaptı) → InProgress (Şoför yolcuyu aldı) → Completed (Sefer bitti) → PendingInvoice → Invoiced
- `ChangeReason` alanı özellikle iptal ve geri alma durumlarında neden bilgisini tutar.
- Jobs silindiğinde bu kayıtlar da cascade olarak silinir.

---

### 6.11 TripLogs (Sefer Kayıtları)

**Amaç:** Şoförün sefer sırasındaki operasyonel bilgilerini tutar. Şoförün "aldım/bıraktım" geri bildirimi burada kaydedilir. Özellikle **Günlük Tahsis** tipindeki işler için detaylı bilgi gereklidir.

| Alan | Tip | Zorunlu | Açıklama |
|---|---|---|---|
| `JobId` | UUID (FK → Jobs) | ✅ | İlgili iş |
| `DriverId` | UUID (FK → Drivers) | ✅ | Seferi yapan şoför |
| `PickupTime` | timestamp with time zone | ❌ | Yolcuyu alma zamanı (şoför bildirir) |
| `DropoffTime` | timestamp with time zone | ❌ | Yolcuyu bırakma zamanı (şoför bildirir) |
| `StartKm` | numeric(10,2) | ❌ | Başlangıç kilometre (tahsis tipi için) |
| `EndKm` | numeric(10,2) | ❌ | Bitiş kilometre (tahsis tipi için) |
| `WaitingMinutes` | integer | ❌ | Bekleme süresi (dakika) |
| `FlightStatus` | varchar(200) | ❌ | Uçuş durumu bilgisi (rötar, zamanında, iptal vb.) |
| `DriverNotes` | varchar(2000) | ❌ | Şoförün seferle ilgili notları |

**İş Mantığı:**
- **Transfer tipi** için: `PickupTime` ve `DropoffTime` yeterlidir ("aldım/bıraktım").
- **Tahsis tipi** için: `PickupTime`, `DropoffTime`, `StartKm`, `EndKm` bilgileri gereklidir. Toplam km = EndKm - StartKm.
- `FlightStatus` alanı, şoförün uçuş kodundan kontrol ettiği bilgiyi kaydeder (rötar 30dk, zamanında vb.).
- `WaitingMinutes` bekleme ücreti hesaplamasında kullanılabilir.
- Şoför, koordinatöre (Alperen) bu bilgileri iletir; koordinatör sisteme girer veya şoför mobil uygulama üzerinden direkt girer.
- Bir iş için birden fazla TripLog olabilir (gidiş ve dönüş ayrı log olarak kaydedilir).

---

### 6.12 JobNotes (İş Notları)

**Amaç:** Bir işe eklenen serbest metin notlarını tutar. Birden fazla kullanıcı not ekleyebilir. İletişim ve takip için kullanılır.

| Alan | Tip | Zorunlu | Açıklama |
|---|---|---|---|
| `JobId` | UUID (FK → Jobs) | ✅ | İlgili iş |
| `NoteText` | varchar(2000) | ✅ | Not içeriği |
| `CreatedByUserId` | UUID (FK → Users) | ✅ | Notu yazan kullanıcı |

**İş Mantığı:**
- Bir iş üzerinde birden fazla not olabilir (kronolojik sırayla).
- Örnek notlar: "Müşteri saat değişikliği istedi", "Şoför trafik yoğunluğu bildirdi", "Ekstra bekleme ücreti eklendi".
- İş akışında herhangi bir aşamada not eklenebilir.
- Kim ne zaman yazmış bilgisi `CreatedByUserId` ve `CreatedAt` ile takip edilir.

---

### 6.13 Invoices (Faturalar)

**Amaç:** Müşterilere kesilen faturaları yönetir. Muhasebeci, tamamlanmış işleri dönemsel olarak faturalar. Bir fatura birden fazla iş kalemini içerebilir.

| Alan | Tip | Zorunlu | Açıklama |
|---|---|---|---|
| `InvoiceNumber` | varchar(50) | ✅ | Benzersiz fatura numarası |
| `CustomerId` | UUID (FK → Customers) | ✅ | Faturanın kesildiği müşteri |
| `InvoiceDate` | date | ✅ | Fatura tarihi |
| `PeriodStart` | date | ✅ | Fatura dönem başlangıcı (örn: 1 Ocak) |
| `PeriodEnd` | date | ✅ | Fatura dönem sonu (örn: 31 Ocak) |
| `TotalAmount` | numeric(18,2) | ✅ | Ara toplam (KDV hariç) |
| `TaxAmount` | numeric(18,2) | ✅ | KDV tutarı |
| `GrandTotal` | numeric(18,2) | ✅ | Genel toplam (KDV dahil) |
| `InvoiceStatus` | integer | ✅ | Fatura durumu (InvoiceStatus enum) |
| `Notes` | varchar(2000) | ❌ | Fatura açıklaması/notları |

**İş Mantığı:**
- Muhasebeci, koordinatörden (Alperen) final olan iş bilgilerini aldığında fatura sürecini başlatır.
- Bir fatura, belirli bir dönemdeki (PeriodStart - PeriodEnd) tüm tamamlanmış işleri kapsar.
- Fatura akışı: `Draft (1)` → `Sent (2)` → `Paid (3)` veya `Cancelled (99)`.
- İlgili işlerin durumu fatura kesildiğinde `Invoiced (6)` olarak güncellenir (Excel'de mavi renk).
- Fatura kesilecek işler kırmızı (PendingInvoice), kesilmiş olanlar mavi (Invoiced) ile gösterilir.

**Unique Index:** `IX_Invoices_InvoiceNumber` → InvoiceNumber

---

### 6.14 InvoiceItems (Fatura Kalemleri)

**Amaç:** Bir faturanın alt kalemlerini tutar. Her kalem bir iş (Job) ile ilişkilidir. Faturadaki her bir transfer işinin detayını gösterir.

| Alan | Tip | Zorunlu | Açıklama |
|---|---|---|---|
| `InvoiceId` | UUID (FK → Invoices) | ✅ | Bağlı olduğu fatura |
| `JobId` | UUID (FK → Jobs) | ✅ | İlgili iş/transfer |
| `Description` | varchar(500) | ✅ | Kalem açıklaması (örn: "03.01.2026 - Batıkent → ESB - 1 kişi") |
| `Amount` | numeric(18,2) | ✅ | Kalem tutarı (TL) |

**İş Mantığı:**
- Her fatura kalemi, tek bir iş kaydına karşılık gelir.
- `Description` alanı, faturada görünecek açıklama satırıdır (tarih, güzergah, yolcu bilgisi).
- `Amount`, genellikle ilgili Job'un `SalePrice` değerine eşittir.
- Bir faturadaki tüm kalemlerin toplamı, `Invoices.TotalAmount` değerini verir.

---

### 6.15 Notifications (Bildirimler)

**Amaç:** Şoförlere, müşterilere ve diğer personele gönderilen bildirimleri takip eder. WhatsApp, SMS, e-posta ve uygulama içi bildirim kanallarını destekler.

| Alan | Tip | Zorunlu | Açıklama |
|---|---|---|---|
| `JobId` | UUID (FK → Jobs) | ❌ | İlgili iş (opsiyonel - genel bildirimler için null) |
| `RecipientType` | integer | ✅ | Alıcı tipi (RecipientType enum) |
| `RecipientPhone` | varchar(20) | ❌ | Alıcı telefon numarası (WhatsApp/SMS için) |
| `RecipientUserId` | UUID (FK → Users) | ❌ | Alıcı kullanıcı (sistem içi bildirimler için) |
| `Channel` | integer | ✅ | Bildirim kanalı (NotificationChannel enum) |
| `Message` | varchar(2000) | ✅ | Bildirim mesaj içeriği |
| `SentAt` | timestamp with time zone | ❌ | Gönderilme zamanı |
| `IsDelivered` | boolean | ✅ | İletildi mi? |
| `DeliveredAt` | timestamp with time zone | ❌ | İletilme zamanı |

**İş Mantığı:**
- **WhatsApp entegrasyonu** temel iletişim kanalıdır:
  - **Şoföre:** 1 gün önceden transfer detayları gönderilir (nereden nereye, saat, yolcu bilgisi, uçuş kodu).
  - **Araç sahibine:** Araç tahsis bilgisi gönderilir.
  - **Koordinatöre:** Şoförden geri bildirim geldiğinde bildirim oluşur.
- Alıcı tipleri:
  - `Driver (1)` - Şoför
  - `Customer (2)` - Müşteri firma
  - `Coordinator (3)` - Koordinatör
  - `Accountant (4)` - Muhasebeci
- `IsDelivered` ve `DeliveredAt` alanları, mesajın başarılı iletilip iletilmediğini takip eder.
- İletilemeyen bildirimlerin tekrar gönderilmesi için kuyruk mekanizması planlanabilir.

---

## 7. Enum Değerleri

### 7.1 CustomerType (Müşteri Tipi)

| Değer | Ad | Açıklama |
|---|---|---|
| 1 | Corporate | Kurumsal müşteri (TaxNumber/TaxOffice kullanır) |
| 2 | Individual | Bireysel müşteri (TcKimlikNo kullanır) |

### 7.2 UserRole (Kullanıcı Rolü)

| Değer | Ad | Açıklama |
|---|---|---|
| 1 | Admin | Sistem yöneticisi - tam yetki |
| 2 | Manager | Yönetici - operasyonel kararlar |
| 3 | Coordinator | Koordinatör (Alperen) - atama, fiyat, araç yönetimi |
| 4 | Reservationist | Rezervasyoncu - iş girişi |
| 5 | Driver | Şoför - sefer bilgileri, geri bildirim |
| 6 | Accountant | Muhasebeci - fatura işlemleri |
| 7 | Viewer | İzleyici - sadece okuma |

### 7.3 VehicleType (Araç Tipi)

| Değer | Ad | Kapasite | Kullanım |
|---|---|---|---|
| 1 | Sedan | 1-3 kişi | Binek araç, tekli VIP transferler |
| 2 | Vito | 4-7 kişi | Küçük grup VIP transferleri |
| 3 | Sprinter | 8-15 kişi | Orta boy grup taşımaları |
| 4 | Minibus | 15-25 kişi | Büyük grup taşımaları |
| 5 | Bus | 25+ kişi | Çok büyük grup/etkinlik transferleri |

### 7.4 LocationType (Lokasyon Tipi)

| Değer | Ad | Açıklama |
|---|---|---|
| 1 | Airport | Havalimanı (ESB, SAW, IST, ADB) - uçuş kodu kontrolü |
| 2 | TrainStation | Tren istasyonu (YHT garları) |
| 3 | BusTerminal | Otobüs terminali |
| 4 | Hotel | Otel |
| 5 | Office | Ofis / Fabrika / İş yeri |
| 6 | Residence | Konut / Site / Mahalle |
| 99 | Other | Diğer |

### 7.5 JobType (İş Tipi)

| Değer | Ad | Açıklama |
|---|---|---|
| 1 | Transfer | Noktadan noktaya tek seferlik transfer |
| 2 | DailyAllocation | Günlük araç tahsisi (detaylı km/saat raporlama gerektirir) |

### 7.6 JobStatus (İş Durumu)

| Değer | Ad | Renk | Açıklama |
|---|---|---|---|
| 1 | Open | ⬜ Beyaz | İş açıkta, henüz atama yapılmamış |
| 2 | Assigned | 🟩 Yeşil | Şoföre/araç sahibine detay atılmış |
| 3 | InProgress | - | Sefer devam ediyor |
| 4 | Completed | 🟨 Sarı | Tamamlandı, fatura bekleniyor |
| 5 | PendingInvoice | 🟥 Kırmızı | Faturası kesilecek |
| 6 | Invoiced | 🔵 Mavi | Fatura kesilmiş |
| 99 | Cancelled | - | İptal edildi |

### 7.7 InvoiceStatus (Fatura Durumu)

| Değer | Ad | Açıklama |
|---|---|---|
| 1 | Draft | Taslak |
| 2 | Sent | Gönderildi |
| 3 | Paid | Ödendi |
| 99 | Cancelled | İptal |

### 7.8 RecipientType (Alıcı Tipi)

| Değer | Ad | Açıklama |
|---|---|---|
| 1 | Driver | Şoför |
| 2 | Customer | Müşteri |
| 3 | Coordinator | Koordinatör |
| 4 | Accountant | Muhasebeci |

### 7.9 NotificationChannel (Bildirim Kanalı)

| Değer | Ad | Açıklama |
|---|---|---|
| 1 | WhatsApp | WhatsApp mesajı (birincil kanal) |
| 2 | SMS | SMS mesajı |
| 3 | Email | E-posta |
| 4 | InApp | Uygulama içi bildirim |

---

## 8. İlişki Diyagramı

### Tablolar Arası İlişkiler (Foreign Keys)

```
Branches ◀─────────── Users (BranchId)
                         │
                         ├── Jobs.CreatedByUserId
                         ├── Jobs.AssignedByUserId
                         ├── JobNotes.CreatedByUserId
                         ├── JobStatusHistories.ChangedByUserId
                         └── Notifications.RecipientUserId

Customers ◀──────────── Passengers (CustomerId)
    │
    ├── Jobs.CustomerId
    └── Invoices.CustomerId

VehicleOwners ◀──────── Vehicles (VehicleOwnerId)
    │                       │
    │                       ├── Drivers.DefaultVehicleId
    │                       └── Jobs.VehicleId
    │
    ├── Drivers.VehicleOwnerId
    └── Jobs.VehicleOwnerId

Drivers ◀────────────── Jobs.DriverId
    │
    └── TripLogs.DriverId

Locations ◀──────────── Jobs.PickupLocationId
    │
    └────────────────── Jobs.DropoffLocationId

Jobs ◀───────────────── JobStatusHistories (JobId) [CASCADE]
  │
  ├── TripLogs (JobId) [CASCADE]
  ├── JobNotes (JobId) [CASCADE]
  ├── InvoiceItems (JobId) [RESTRICT]
  └── Notifications (JobId) [SET NULL]

Invoices ◀──────────── InvoiceItems (InvoiceId) [CASCADE]
```

### Silme Davranışları

| İlişki | Silme Davranışı | Açıklama |
|---|---|---|
| Job → StatusHistories, TripLogs, JobNotes | **CASCADE** | İş silinirse alt kayıtlar da silinir |
| Job → InvoiceItems | **RESTRICT** | Faturası olan iş silinemez |
| Job → Notifications | **SET NULL** | İş silinirse bildirim kalır, referans null olur |
| Job → Passenger, Locations, Vehicle, VehicleOwner, Driver | **SET NULL** | İlişkili kayıt silinirse referans null olur |
| Job → Customer, CreatedByUser | **RESTRICT** | Müşterisi veya oluşturanı olan iş silinemez |
| Driver → DefaultVehicle | **SET NULL** | Araç silinirse şoförün varsayılan aracı null olur |
| All others | **RESTRICT** | Bağımlı kayıt varsa silme engellenir |

---

## 9. İndeksler ve Performans

### Unique İndeksler

| İndeks | Tablo | Sütun | Açıklama |
|---|---|---|---|
| `IX_Users_Username` | Users | Username | Kullanıcı adı benzersizliği |
| `IX_Customers_TaxNumber` | Customers | TaxNumber (WHERE NOT NULL) | Vergi no benzersizliği (koşullu) |
| `IX_Jobs_JobNumber` | Jobs | JobNumber | İş numarası benzersizliği |
| `IX_Invoices_InvoiceNumber` | Invoices | InvoiceNumber | Fatura numarası benzersizliği |

### Sorgu Performans İndeksleri

| İndeks | Tablo | Sütun | Kullanım Senaryosu |
|---|---|---|---|
| `IX_Jobs_JobDate` | Jobs | JobDate | Günlük/aylık iş listesi sorgulama |
| `IX_Jobs_Status` | Jobs | Status | Duruma göre filtreleme (açık işler, fatura bekleyenler) |
| `IX_Jobs_CustomerId` | Jobs | CustomerId | Müşteriye göre iş listesi |
| `IX_Jobs_DriverId` | Jobs | DriverId | Şoföre göre iş listesi |
| `IX_Jobs_VehicleId` | Jobs | VehicleId | Araca göre iş listesi |
| `IX_Jobs_VehicleOwnerId` | Jobs | VehicleOwnerId | Araç sahibine göre iş listesi |
| `IX_Jobs_PickupLocationId` | Jobs | PickupLocationId | Lokasyona göre sorgulama |
| `IX_Jobs_DropoffLocationId` | Jobs | DropoffLocationId | Lokasyona göre sorgulama |
| `IX_Jobs_AssignedByUserId` | Jobs | AssignedByUserId | Atayan kişiye göre sorgulama |
| `IX_Jobs_CreatedByUserId` | Jobs | CreatedByUserId | Oluşturan kişiye göre sorgulama |
| `IX_Jobs_PassengerId` | Jobs | PassengerId | Yolcuya göre sorgulama |

### Diğer FK İndeksleri

Tüm foreign key sütunlarında otomatik indeks oluşturulmuştur (EF Core convention). Toplam **47 indeks** bulunmaktadır.

---

## 10. Teknik Notlar

### 10.1 Soft Delete

Tüm tablolarda `IsDeleted` alanı mevcuttur. Kayıt silindiğinde veritabanından fiziksel olarak kaldırılmaz; `IsDeleted = true` yapılır. EF Core global query filter ile `IsDeleted = false` olan kayıtlar otomatik olarak filtrelenir. Bu sayede:
- Yanlışlıkla silinen veriler kurtarılabilir
- Geçmiş kayıtlar referans bütünlüğü için korunur
- Denetim izleri bozulmaz

### 10.2 Otomatik Zaman Damgaları

`SaveChangesAsync` override edilmiştir:
- **Yeni kayıt:** `Id` (GUID), `CreatedAt` ve `UpdatedAt` otomatik doldurulur
- **Güncelleme:** `UpdatedAt` otomatik güncellenir

### 10.3 Teknoloji Yığını

| Bileşen | Teknoloji |
|---|---|
| Runtime | .NET 10.0 |
| ORM | Entity Framework Core 9 |
| Veritabanı | PostgreSQL (Npgsql provider) |
| Mimari | Katmanlı mimari (API → Application → Domain → Infrastructure) |

### 10.4 Mevcut Veri Durumu

| Tablo | Kayıt Sayısı |
|---|---|
| Branches | 1 |
| Users | 1 |
| VehicleOwners | 5 |
| Locations | 6 |
| Diğer tablolar | 0 (henüz operasyonel veri girilmemiş) |

### 10.5 Bağlantı Bilgileri

```
Host=localhost
Port=5432
Database=liventa_transfer
Username=liventa_user
```

---

> **Bu dokümantasyon**, `liventa_transfer` veritabanının canlı incelemesi ve kaynak kodundaki Entity Framework Core entity yapılarının analizi sonucunda oluşturulmuştur. Excel dosyası (`01 - Ocak (4).xlsx`) verileriyle eşleştirme yapılmış ve iş akışı kullanıcı gereksinimleri doğrultusunda açıklanmıştır.

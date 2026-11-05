# Dijital-Cuzdan-P0010-
# 🚀 SecureWallet (Dijital Cüzdan Projesi)

SecureWallet, modern web ve mobil platformlar için tasarlanmış, güvenli ve ölçeklenebilir bir dijital cüzdan ve para transferi yönetim sistemidir. Bu repository, projenin backend altyapısını ve API servislerini barındırır.

---

## 🛠️ Kullanılan Teknolojiler

* **Platform:** .NET 8 SDK (Sürüm: `8.0.427`)
* **Veritabanı:** PostgreSQL
* **ORM:** Entity Framework Core (EF Core) 8.0
* **Mimari / Desen:** Code-First, Repository & Service Pattern, IDesignTimeDbContextFactory
* **Versiyon Kontrol:** Git & GitHub (`develop` ve `main` dalları)

---

## 📂 Proje Mimarisi ve Klasör Yapısı

```text
SecureWallet.Api/
│
├── Controllers/       # API Uç Noktaları (Endpoints)
├── Data/              # AppDbContext ve Design-Time Factory sınıfları
├── Migrations/        # EF Core Veritabanı Migration dosyaları
├── Models/            # Veritabanı Varlıkları (User, Wallet, Transaction, AuditLog)
├── Services/          # İş Mantığı (Business Logic) katmanı
├── appsettings.json   # Veritabanı ve ortam yapılandırmaları
├── global.json        # .NET SDK sürüm kilitleme dosyası
└── Program.cs         # Servis kayıtları ve uygulama pipeline ayarları

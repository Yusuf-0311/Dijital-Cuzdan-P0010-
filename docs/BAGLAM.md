# SecureWallet — Proje Bağlamı (devir notu)

> Bu dosya, projeye yeni bir yapay zeka sohbetinde (ya da yeni bir ekip üyesiyle) devam ederken
> bağlamı tek seferde aktarmak için tutulur. Yeni bir sohbete başlarken bu dosyanın tamamını ilk
> mesaj olarak yapıştırın. **Önemli bir karar alındığında veya bir PR birleştiğinde güncelleyin.**
>
> Son güncelleme: 22 Kasım 2026 (PR #31 ve Dependabot ayarları açıkken)

---

## 1. Proje

- **Ad:** Dijital Cüzdan / SecureWallet
- **Ders:** Yapay Zeka ile Yazılım Geliştirme
- **Ekip:** Yusuf (GitHub: `Yusuf-0311`), Kadir (GitHub: `ktemiz28`)
- **Repo:** `github.com/Yusuf-0311/Dijital-Cuzdan-P0010-` — varsayılan branch `develop`
- **Teslim:** **31 Aralık 2026.** Kod için hedef **24 Aralık** (özellik dondurma), son hafta README, `develop → main` ve sunum hazırlığı.
- **Ürün:** Bakiye görüntüleme, para yatırma/çekme, kullanıcılar arası transfer, fatura ödeme ve kayıtlı ödemeler, işlem geçmişi ve dekont. Son ürün bir **web sitesi** olacak (mobil uygulama yok).

### Hocanın değerlendirme ölçütleri
Ekip içi iş dağılımının dengeli olması, yapay zeka kullanımının belgelenmesi, gerçekçi commit geçmişi
ve en az 3 PR, SonarCloud ile ölçülen teknik borç, güvenlik, mimariye uygunluk, okunabilirlik,
test edilebilirlik, README, veritabanı tasarımı ve **25 dakikalık sunum** (sunumda kod hakkında
sorular sorulacak; herkes kendi yazdığı kodu açıklayabilmeli).

---

## 2. Teknoloji

- .NET 8, ASP.NET Core Web API, EF Core 8 (code-first) + Npgsql
- PostgreSQL 15 (Docker container)
- Kimlik doğrulama: `PasswordHasher<User>` (PBKDF2, kullanıcı başına tuz) + JWT (HmacSha256, 15 dk, `sub` claim'i)
- Test: xUnit + Testcontainers.PostgreSql (gerçek Postgres, migration'lar test başında uygulanır) + coverlet
- CI: GitHub Actions ("Derleme ve testler"), CodeQL, SonarCloud (Quality Gate: yeni kodda ≥ %80 kapsam), Dependabot
- Arayüz: **henüz başlanmadı.** Öneri Blazor WebAssembly (karar Kadir'in). Tasarım taslağı hazır (Giriş, Ana sayfa, Transfer ekranları — Claude'daki "SecureWallet Arayüz Tasarımı" tuvali).

### Klasör yapısı
```
Controllers/   AuthController, WalletController, TransferController, PaymentController
Data/          AppDbContext (CHECK kısıtları, indeksler, Biller seed verisi)
Dtos/          İstek/yanıt modelleri (AuthDtos, WalletDtos, PaymentDtos)
Extensions/    Servis kayıtları, CORS, JWT, rate limit, ServiceResult → HTTP eşlemesi
Middleware/    GlobalExceptionHandler (stack trace sızdırmaz, ProblemDetails döner)
Migrations/    InitialCreate, WalletSchemaHardening, FixWalletCurrencyLength, AddBillPayments, AddAccountLockout
Models/        User, Wallet, Transaction, Biller, SavedPayment, AuditLog
Services/      İş mantığı (WalletService, TransferService, PaymentService, AuthService, TokenService, AuditService, DailyLimit)
tests/SecureWallet.Api.Tests/
  Infrastructure/  PostgresFixture, TestData, AuthTestData, ControllerTestHelper
  Unit/            Veritabanı gerektirmeyen testler
  Integration/     Gerçek Postgres ile çalışan testler
```

---

## 3. Kimin ne yaptığı

| Alan | Sahibi | Durum |
|---|---|---|
| Veritabanı mimarisi, migration'lar, kısıtlar | Yusuf | Tamamlandı |
| Cüzdan (bakiye, yatır/çek, özet, geçmiş) | Yusuf | Tamamlandı |
| Transfer (alıcı arama, maskeli isim, son alıcılar) | Yusuf | Tamamlandı |
| Fatura ödeme, kayıtlı ödemeler | Yusuf | Tamamlandı |
| Testler altyapısı, CI, SonarCloud, CodeQL, CORS | Yusuf | Tamamlandı |
| Kayıt / giriş / JWT / Swagger Authorize (PR #27) | Kadir | Tamamlandı |
| Hesap kilidi, şifre değiştirme, giriş istek sınırı (PR #28) | Kadir | Tamamlandı |
| Refresh token | Kadir | **Sırada** |
| Web arayüzü | Kadir (ağırlıklı) | Başlanmadı |
| Docker (API + DB + arayüz tek komut), yapay zeka özelliği | Kadir | Planlandı |
| Günlük işlem limiti (PR #31) | Yusuf | Onay bekliyor |
| Dependabot / Sonar ayarları | Yusuf | Onay bekliyor |
| PDF dekont | Yusuf | **Devam ediyor** |
| Health check / loglama | Yusuf | Planlandı |
| Değiştirilemez kayıt defteri (ledger), otomatik ödeme talimatı | — | Zaman kalırsa |

---

## 4. Değiştirilmeyecek kurallar

### Güvenlik
- **Gizli bilgiler repoya asla girmez.** Veritabanı şifresi ve `Jwt:Key` sadece `dotnet user-secrets` içinde (ileride Docker için `.env`, o da `.gitignore`'da). `appsettings.json`'da `Jwt:Key` yoktur.
- GitHub token'ları dosyalara veya sohbetlere yapıştırılmaz.
- Şifre/anahtar görünen ekran görüntüsü paylaşılmaz.

### Git ve dürüstlük
- **Herkes sadece kendi işini, kendi GitHub hesabından commit'ler.** Birinin kodu diğerinin adıyla commit'lenmez.
- **Commit tarihleri değiştirilmez**, geçmiş sahte oluşturulmaz.
- Her iş ayrı branch'te (`feat/...`), `develop`'a PR ile girer.
- `develop` korumalı (ruleset `develop-koruma`): PR + **1 onay** + "Derleme ve testler" yeşil olmadan birleşmez.
- Kırmızı kontrolle PR birleştirilmez.
- PR açıklaması repodaki şablona göre doldurulur (Ne değişti / Neden / Nasıl test edildi / Yapay zeka kullanımı).
- İnceleme yorumları gerçek olmalı: kodu okuyup anlayarak, mümkünse branch'i çekip testleri çalıştırarak.
- Yeni dosya düzenlerken PowerShell değil VS Code kullanılır (Türkçe karakter kodlaması bozulmasın).

---

## 5. Mimari kararlar (sunumda sorulabilir)

- **ServiceResult<T> deseni:** Servisler exception fırlatmaz; `ErrorCode` döner, controller `ToActionResult` ile HTTP koduna çevirir:
  `Validation/SelfTransfer → 400`, `InvalidCredentials → 401`, `NotFound → 404`, `Conflict/Duplicate → 409`,
  `InsufficientFunds/CurrencyMismatch/LimitExceeded → 422`, `AccountLocked → 423`, diğer → 500. Hatalar `ProblemDetails` olarak döner.
- **Eşzamanlılık (para):** `Wallet.Version` PostgreSQL `xmin` ile iyimser kilit; çakışmada `ConcurrencyRetry` 3 kez dener. Her işlem tek `SaveChanges` ile atomik.
- **Eşzamanlılık (giriş sayacı):** Hatalı giriş sayacı `ExecuteUpdateAsync` ile tek SQL UPDATE içinde atomik artırılır (paralel isteklerle sınır aşılamaz).
- **Veritabanı kısıtları:** `Balance ≥ 0`, `Amount > 0`, işlem türü–cüzdan kuralı, ödeme–kurum kuralı (CHECK); benzersiz indeksler; `ON DELETE RESTRICT`.
- **Zamanlama saldırısı koruması:** Kullanıcı yoksa da sahte hash ile doğrulama yapılır; "e-posta yok" ve "şifre yanlış" aynı mesajı döner.
- **Hesap kilidi:** 5 hatalı giriş → 15 dk kilit (423). Kilitliyken doğru şifre de reddedilir. Başarılı girişte sayaç sıfırlanır.
- **Günlük çıkış limiti:** Para çekme + giden transfer toplamı günde 100.000 TRY (`WalletLimits:DailyOutgoingLimit`, `ValidateOnStart`). Gün Türkiye saatiyle 00:00'da sıfırlanır. Kontrol `ConcurrencyRetry` içinde yapılır; eşzamanlı isteklerde `xmin` çakışması yeniden denemeye yol açar ve limit güncel toplamla yeniden hesaplanır. Durum: `GET /api/wallet/limits`.
- **İstek sınırı:** Para işlemleri kullanıcı başına dakikada 10 (`money` politikası, Yusuf). Giriş/kayıt/şifre değiştirme IP başına dakikada 10 (`auth` politikası, Kadir). Aşılınca 429.
- **Şifre değiştirme:** Mevcut şifre yanlışsa 401 değil **400** (arayüz oturumu kapatmasın diye).
- **Eksik alan koruması:** Önemli sayısal alanlarda `[JsonRequired]` (gönderilmeyen `Amount` sessizce 0 olmasın).
- **CORS:** `WebClient` politikası, izinli adresler `Cors:AllowedOrigins` ayarından. Geliştirmede `http://localhost:5173` (Vite varsayılanı) — Blazor seçilirse kendi portuyla değiştirilecek.
- **Middleware sırası:** ExceptionHandler → (dev) Swagger → HttpsRedirection → CORS → Authentication → Authorization → RateLimiter → Controllers.

### Bilinçli tavizler
- "Hesap kilitli" mesajı e-postanın kayıtlı olduğunu belli eder (sadece 5 hatalı denemeden sonra). Kayıtlı olmayan e-postada kilit mesajı dönmez.
- Biri başkasının e-postasıyla bilerek 5 hatalı deneme yapıp hesabı 15 dk kilitleyebilir (hizmet engelleme). IP sınırı yavaşlatır; tam çözüm CAPTCHA / e-postayla kilit açma, kapsam dışı. **README zorluklar bölümüne eklenecek (Kadir).**
- Fatura ödemeleri günlük limite sayılmaz (para sabit kurumlara gider). Saldırgan kendi abone numarasına fatura ödetebilir; kapsam dışı bırakıldı.
- Dependabot PR'larında SonarCloud atlanır (GitHub bu PR'lara gizli anahtar vermez); NuGet'te büyük sürüm güncellemeleri önerilmez (proje .NET 8 LTS).
- Şifre değişse de eski JWT'ler en fazla 15 dk geçerli kalır → refresh token ile çözülecek.
- Reverse proxy (Docker + nginx) arkasında IP adresi proxy'ninki olur → Docker aşamasında `ForwardedHeaders` gerekecek.

---

## 6. Geliştirme ortamı

- Yusuf: Ubuntu. Kadir: Windows (PowerShell, Docker Desktop, VS Code + C# Dev Kit).
- Kurulum özeti:
  ```
  git clone ... && git checkout develop
  docker run --name securewallet-db -e POSTGRES_PASSWORD=<şifre> -e POSTGRES_DB=securewallet -p 5432:5432 -d postgres:15
  dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=securewallet;Username=postgres;Password=<şifre>"
  dotnet user-secrets set "Jwt:Key" "<en az 32 karakterlik rastgele değer>"
  dotnet tool install --global dotnet-ef --version 8.0.*
  dotnet ef database update
  dotnet test tests/SecureWallet.Api.Tests
  dotnet run   → /swagger
  ```
- Bilgisayarda ayrıca kurulu bir PostgreSQL varsa 5432 portu çakışır; Docker'ı `-p 5433:5432` ile başlatıp bağlantıda `Port=5433` kullanın (bu ayar sadece kendi user-secrets'ınızda kalır).
- Test yazarken: `[Collection(PostgresCollection.Name)]`, `PostgresFixture`, `TestData.*`, `AuthTestData.CreateAuthService(db)`, controller testleri için `ControllerTestHelper.WithUser(...)`. Her deneme ayrı `DbContext` ile yapılır.
- DTO'lar: Auth istekleri (`RegisterRequest`, `LoginRequest`, `ChangePasswordRequest`) **sınıf** → `new X { A = ... }`. Yanıtlar ve cüzdan istekleri **record**.

---

## 7. Şu anki durum (22 Kasım 2026)

- `develop`'ta 151 test geçiyor. `feat/daily-limit` (PR #31) ile 169'a çıkıyor; Kadir Windows'ta da çalıştırdı, hepsi geçti.
- Açık PR'lar: #31 günlük limit, Dependabot/Sonar ayarları (ikisi de Kadir'in onayını bekliyor). Birleşince büyük sürüm Dependabot PR'ları (#23, #24, #30) kapatılacak, diğerlerine `@dependabot rebase` yazılacak.
- Birleşen PR'lar: #27 (kayıt/giriş/JWT, Kadir), #28 (hesap güvenliği, Kadir) ve öncesinde Yusuf'un cüzdan/transfer/ödeme/CI PR'ları.
- `main` henüz SonarCloud'da analiz edilmedi; rozetler teslimden önce açılacak `develop → main` PR'ıyla hesaplanacak.

## 8. Takvim

| Tarih | Kadir | Yusuf |
|---|---|---|
| 16–22 Kasım | README zorluk satırı, refresh token | Günlük işlem limiti ✔, Dependabot temizliği, PDF dekont |
| 23 Kasım–13 Aralık | Web arayüzü, sayfa sayfa (her sayfa ayrı PR) | PDF dekont, health/loglama, arayüz PR incelemeleri |
| 14–20 Aralık | Docker, yapay zeka özelliği | Uçtan uca test, hata düzeltme, README |
| 21–31 Aralık | 24 Aralık özellik dondurma → `develop → main`, README, sunum provası | Aynı |

---

## 9. Yapay zeka ile çalışma notu

Yeni sohbette bu dosyayı yapıştırdıktan sonra şunu da ekleyin:

> Ben [Kadir / Yusuf]'um. Sadece kendi işlerim üzerinde çalışıyorum ve kendi hesabımdan commit atıyorum.
> Kodu benim dosyalarıma birebir uyacak şekilde yazabilmen için önce ilgili dosyaların güncel halini
> isteyebilirsin. Her değişikliğin neden yapıldığını açıkla; sunumda bu kodu ben anlatacağım.

Yapay zekanın önerdiği kod satır satır okunur, testlerle doğrulanır ve PR açıklamasının
"Yapay zeka kullanımı" bölümüne ne için kullanıldığı yazılır.

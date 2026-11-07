# Yerel Kurulum

Şifreler repoya girmez. Her geliştirici kendi makinesinde aşağıdaki adımları uygular.

## 1) Veritabanını başlat
```bash
cp .env.example .env        # sonra .env içindeki POSTGRES_PASSWORD'ü değiştir
docker compose down         # eski (şifresi repoda olan) konteyner varsa temizler
docker compose up -d
```

## 2) Bağlantı dizesini user-secrets'a yaz
`<SIFRE>` yerine .env'deki POSTGRES_PASSWORD değerini yaz:
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=SecureWalletDB;Username=wallet_admin;Password=<SIFRE>"
```

## 3) Migration uygula
```bash
dotnet ef database update
```

> JWT gizli anahtarı da aynı şekilde user-secrets ile tutulmalı:
> `dotnet user-secrets set "Jwt:Key" "<en az 32 karakterlik rastgele anahtar>"`

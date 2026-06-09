# Toplu Mesajlaşma Platformu

ASP.NET Core Web API (.NET 7) + SQL Server + SignalR ile geliştirilmiş toplu mesajlaşma platformu.

## Özellikler

- **Web Arayüzü (SPA)** — `wwwroot` içinde, vanilla JS + SignalR ile entegre
- **ASP.NET Identity** tabanlı kullanıcı yönetimi
- **JWT** ile kimlik doğrulama / yetkilendirme
- **Admin** kullanıcısı:
  - Tüm üyelerle birebir mesajlaşma
  - Mesaj grupları oluşturma
  - Gruplara toplu mesaj gönderme
  - Grup üyelerini yönetme
- **SignalR** ile gerçek zamanlı mesaj bildirimleri
- **Swagger UI** ile API dokümantasyonu

## Teknoloji Yığını

| Katman | Teknoloji |
|--------|-----------|
| Backend | ASP.NET Core Web API .NET 7 |
| Veritabanı | SQL Server (LocalDB) |
| ORM | Entity Framework Core 7 |
| Kimlik | ASP.NET Identity |
| Auth | JWT Bearer Token |
| Gerçek Zamanlı | SignalR |
| Dok. | Swagger / OpenAPI |

## Kurulum

### 1. Gereksinimler
- .NET 7 SDK
- SQL Server / LocalDB

### 2. Veritabanı ayarı

`appsettings.json` dosyasındaki connection string'i düzenleyin:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MessagingPlatformDb;Trusted_Connection=True;"
}
```

### 3. Migration & Çalıştırma

```bash
cd MessagingPlatform
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet run
```

Uygulama çalıştıktan sonra:

| Adres | İçerik |
|-------|--------|
| `http://localhost:5000/` | **Web Arayüzü** (giriş + mesajlaşma paneli) |
| `http://localhost:5000/swagger` | Swagger API dokümantasyonu |

> Frontend, ayrı kurulum gerektirmez — ASP.NET uygulamasının içinde (`wwwroot`) yer alır ve API + SignalR ile entegre çalışır.

## Varsayılan Kullanıcılar (Seed Data)

| E-posta | Şifre | Rol |
|---------|-------|-----|
| admin@messaging.com | Admin123! | Admin |
| ali@example.com | User123! | User |
| ayse@example.com | User123! | User |
| mehmet@example.com | User123! | User |

## API Endpoint'leri

### Auth
| Method | Endpoint | Açıklama |
|--------|----------|----------|
| POST | `/api/auth/login` | Giriş yap |
| POST | `/api/auth/register` | Kayıt ol |

### Kullanıcılar
| Method | Endpoint | Yetki |
|--------|----------|-------|
| GET | `/api/users` | Admin |
| GET | `/api/users/members` | Admin |
| GET | `/api/users/{id}` | Authenticated |

### Mesajlar (Birebir)
| Method | Endpoint | Yetki |
|--------|----------|-------|
| POST | `/api/messages` | Admin |
| GET | `/api/messages` | Admin |
| GET | `/api/messages/conversation/{userId}` | Admin |
| GET | `/api/messages/my` | Authenticated |

### Gruplar
| Method | Endpoint | Yetki |
|--------|----------|-------|
| POST | `/api/groups` | Admin |
| GET | `/api/groups` | Admin |
| GET | `/api/groups/{id}` | Authenticated |
| POST | `/api/groups/{id}/members` | Admin |
| DELETE | `/api/groups/{id}/members/{userId}` | Admin |
| POST | `/api/groups/{id}/messages` | Admin |
| GET | `/api/groups/{id}/messages` | Authenticated |
| DELETE | `/api/groups/{id}` | Admin |

## SignalR Hub

**Endpoint:** `wss://localhost:5001/hubs/chat?access_token={jwt_token}`

### İstemci Olayları (dinle)
- `ReceiveMessage` — Birebir mesaj geldiğinde
- `ReceiveGroupMessage` — Grup mesajı geldiğinde

### Sunucuya Çağrı
- `JoinGroup(groupId)` — Gruba katıl
- `LeaveGroup(groupId)` — Gruptan ayrıl

# Toplu Mesajlaşma Platformu

ASP.NET Core Web API (.NET 7) + SQL Server + SignalR ile geliştirilmiş toplu mesajlaşma platformu.

> **Not:** Veritabanı bağlantısı varsayılan olarak **SQL Server Express** (`Server=.\SQLEXPRESS`) için ayarlıdır. Farklı bir SQL Server instance kullanıyorsanız (ör. `(localdb)\mssqllocaldb` veya `localhost`), `MessagingPlatform/appsettings.json` içindeki `Server` değerini kendi sunucunuza göre değiştirin. Veritabanı ilk çalıştırmada otomatik oluşturulur.

## Özellikler

- **Web Arayüzü (SPA)** — `wwwroot` içinde, vanilla JS + SignalR ile entegre
- **ASP.NET Identity** tabanlı kullanıcı yönetimi
- **JWT** ile kimlik doğrulama / yetkilendirme
- **Admin** kullanıcısı:
  - Tüm üyelerle birebir mesajlaşma
  - Mesaj grupları oluşturma
  - Gruplara toplu mesaj gönderme
  - Grup üyelerini yönetme (ekle / çıkar)
- **Üyeler**, kendilerine gelen mesajlara yöneticiye yanıt verebilir
- **Mesaj silme** — bir mesajı gönderen kişi veya Admin silebilir (birebir ve grup)
- **SignalR** ile gerçek zamanlı mesaj ve silme bildirimleri
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

## Kurulum ve Çalıştırma

### 1. Gereksinimler
- .NET 7 SDK
- SQL Server Express (veya LocalDB / başka bir SQL Server instance)

### 2. Veritabanı ayarı

Bağlantı dizesi `appsettings.json` içinde tanımlıdır, varsayılan olarak SQL Server Express kullanır:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=MessagingPlatformDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

> Kendi ortamınıza göre `Server` değerini değiştirebilirsiniz: LocalDB için `Server=(localdb)\\mssqllocaldb`, varsayılan instance için `Server=localhost`.

### 3. Çalıştırma

```bash
cd MessagingPlatform
dotnet run
```

Veritabanı **ilk çalıştırmada otomatik oluşturulur** (migration'lar uygulanır) ve örnek kullanıcılarla doldurulur. Ayrıca elle migration komutu çalıştırmanıza gerek yoktur.

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
| GET | `/api/users/admins` | Authenticated |
| GET | `/api/users/{id}` | Authenticated |

### Mesajlar (Birebir)
| Method | Endpoint | Yetki |
|--------|----------|-------|
| POST | `/api/messages` | Authenticated (üye yalnızca admine) |
| GET | `/api/messages/conversation/{userId}` | Authenticated |
| GET | `/api/messages/my` | Authenticated |
| DELETE | `/api/messages/{id}` | Gönderen veya Admin |

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
| DELETE | `/api/groups/{groupId}/messages/{messageId}` | Gönderen veya Admin |
| DELETE | `/api/groups/{id}` | Admin |

## SignalR Hub

**Endpoint:** `/hubs/chat?access_token={jwt_token}`

### İstemci Olayları (dinle)
- `ReceiveMessage` — Birebir mesaj geldiğinde
- `ReceiveGroupMessage` — Grup mesajı geldiğinde
- `MessageDeleted` — Birebir mesaj silindiğinde
- `GroupMessageDeleted` — Grup mesajı silindiğinde

### Sunucuya Çağrı
- `JoinGroup(groupId)` — Gruba katıl
- `LeaveGroup(groupId)` — Gruptan ayrıl

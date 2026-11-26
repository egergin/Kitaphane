# Architecture

# 1. Architecture Overview

Kitaphane frontend ve backend olarak ayrılmış bir web uygulaması olacaktır.

Temel mimari:

text

```
React + TypeScript
        |
        | REST API
        v
ASP.NET Core Web API
        |
        ├── Authentication (Identity + JWT)
        ├── Profile
        ├── Books
        ├── Tags
        ├── Statistics
        ├── Reading Goals
        ├── Google Books Integration ──> Google Books API
        └── Email (SMTP) ──────────────> Resend (prod) / Mailpit (local)
        |
        v
Entity Framework Core
        |
        v
PostgreSQL
```

Backend mümkün olduğunca katmanlı, maintainable ve test edilebilir olmalıdır.

Business logic ve presentation layer birbirinden ayrılmalıdır.

MVP olduğu için gereksiz abstraction ve katmanlardan kaçınılmalıdır.

# 2. Technology Stack

## Backend

- .NET 10
- ASP.NET Core Web API
- C#
- Entity Framework Core
- Npgsql
- ASP.NET Core Identity
- JWT Bearer authentication + refresh token
- REST API
- OpenAPI / Swagger

## Frontend

- React
- TypeScript
- Vite
- Tailwind CSS + shadcn/ui
- React Router
- TanStack Query
- react-hook-form + zod
- i18next (Türkçe + İngilizce)

## Database

- PostgreSQL

## External API

- Google Books API

## Email

- SMTP (production: Resend SMTP, local: Mailpit)

## Validation

- FluentValidation

## Testing

- xUnit
- Testcontainers (PostgreSQL)

## Infrastructure

- Docker
- Docker Compose

# 3. Backend Architecture

Önerilen backend organizasyonu:

text

```
src/
├── API/
├── Application/
├── Domain/
└── Infrastructure/
```

## API

HTTP request ve response yönetiminden sorumludur.

Sorumluluklar:

- Controllers / endpoints
- Authentication configuration
- Authorization
- Middleware
- OpenAPI / Swagger
- HTTP error responses
- Dependency injection composition

## Application

Uygulamanın use case ve business logic katmanıdır.

Sorumluluklar:

- Auth ve profile use case'leri (kayıt doğrulama, token üretimi orkestrasyonu)
- Book operations
- Tag operations
- Reading Goal operations
- Statistics calculations
- DTO mapping
- FluentValidation validators
- Dış servis contract'ları (Google Books, email sender)

Application katmanı database'e `IAppDbContext` arayüzü üzerinden erişir (DbSet'ler + `SaveChangesAsync`). Generic repository kullanılmaz; sorgular doğrudan EF Core LINQ ile servislerde yazılır.

Authenticated kullanıcı bilgisi `ICurrentUser` arayüzü ile alınır (API katmanında JWT claim'lerinden implemente edilir).

## Domain

Uygulamanın temel iş modellerini ve kurallarını içerir.

Sorumluluklar:

- Book
- Tag
- BookTag
- ReadingGoal
- Domain enums
- Domain-level business rules

ASP.NET Core Identity user modeli (`ApplicationUser`) Infrastructure katmanında tanımlanır. Domain entity'leri kullanıcıya yalnızca `UserId` (Guid) ile bağlanır; Identity tiplerine referans vermez. Foreign key ilişkisi Infrastructure'daki entity configuration'larda kurulur.

Status/tarih kuralları gibi kitap iş kuralları Book entity'si üzerindeki metotlarla (ör. `ChangeStatus`) uygulanır ve birim testleriyle doğrulanır.

## Infrastructure

Dış sistemler ve persistence işlemlerinden sorumludur.

Sorumluluklar:

- EF Core DbContext
- Entity configurations
- PostgreSQL integration
- Identity persistence
- Migrations
- JWT / refresh token üretimi
- Google Books HTTP client
- SMTP email sender
- External service implementations

# 4. Backend Project Structure

Önerilen solution organizasyonu:

text

```
backend/
├── Kitaphane.sln
├── src/
│   ├── Kitaphane.API/
│   │   ├── Controllers/
│   │   ├── Middleware/          (exception → error format)
│   │   ├── Extensions/          (service registration, auth, swagger)
│   │   ├── CurrentUser.cs       (ICurrentUser implementasyonu)
│   │   └── Program.cs
│   │
│   ├── Kitaphane.Application/
│   │   ├── Common/              (IAppDbContext, ICurrentUser, IEmailSender, hata tipleri, PagedResult)
│   │   ├── Auth/
│   │   ├── Profile/
│   │   ├── Books/
│   │   ├── Tags/
│   │   ├── ReadingGoals/
│   │   ├── Statistics/
│   │   └── GoogleBooks/
│   │   (her feature klasörü kendi DTO, validator ve service dosyalarını içerir)
│   │
│   ├── Kitaphane.Domain/
│   │   ├── Entities/
│   │   └── Enums/
│   │
│   └── Kitaphane.Infrastructure/
│       ├── Persistence/
│       │   ├── AppDbContext.cs
│       │   ├── Configurations/
│       │   └── Migrations/
│       ├── Identity/            (ApplicationUser, JWT/refresh token servisleri)
│       ├── GoogleBooks/
│       ├── Email/               (SMTP sender, e-posta şablonları)
│       └── DependencyInjection.cs
│
├── tests/
│   ├── Kitaphane.UnitTests/         (Domain kuralları, istatistik/hedef hesapları)
│   └── Kitaphane.IntegrationTests/  (Testcontainers PostgreSQL, ownership, auth akışları)
│
├── Dockerfile        (production: Cloud Run / yalnızca backend)
└── Dockerfile.dev    (docker compose)
```

Proje referansları:

text

```
API            → Application, Infrastructure
Infrastructure → Application, Domain
Application    → Domain
Domain         → (yok)
```

Bu yapı başlangıç önerisidir. Gereksiz dosya ve abstraction oluşturulmamalıdır.

Özellikle yalnızca gelecekte gerekebilir düşüncesiyle generic repository, generic handler veya gereksiz service katmanları eklenmemelidir.

# 5. Frontend Architecture

Frontend React + TypeScript + Vite kullanılarak geliştirilecektir.

Önerilen yapı:

text

```
frontend/
├── src/
│   ├── app/          (router, providers, query client)
│   ├── components/   (ui/ → shadcn component'leri, ortak component'ler)
│   ├── layouts/
│   ├── features/     (auth, books, google-books, tags, profile, statistics, reading-goals)
│   ├── pages/        (route bileşenleri; feature component'lerini birleştirir)
│   ├── lib/          (http client, utils)
│   └── locales/      (tr.json, en.json)
├── functions/api/    (Cloudflare Pages Function: /api/* → Cloud Run proxy)
├── .env.example
└── Dockerfile.dev    (docker compose; production'da Cloudflare Pages statik dosyaları sunar)
```

Production dağıtımı: `docs/deployment/deployment.md`.

Feature-based organization kullanılır. Detaylar: `frontend-design.md`.

Business logic ve UI mümkün olduğunca ayrılmalıdır.

Frontend API service layer üzerinden backend ile iletişim kurmalıdır.

Authentication state ve protected routes desteklenmelidir.

Detaylar: `frontend-design.md`

# 6. External Integration

Google Books API yalnızca backend üzerinden çağrılmalıdır.

Frontend doğrudan Google Books API ile iletişim kurmamalıdır.

Google Books API key gerekiyorsa environment variable olarak saklanmalıdır.

Google Books categories / subjects verileri kullanılmayacaktır.

E-posta gönderimi standart SMTP üzerinden yapılır; sağlayıcıya özel SDK kullanılmaz. Production'da Resend SMTP (`smtp.resend.com`), local development'ta Docker Compose içindeki Mailpit kullanılır. Gönderilen e-postalar:

- Kayıt doğrulama kodu
- Şifre sıfırlama kodu

# 7. Database Architecture

PostgreSQL kullanılacaktır.

Entity Framework Core ve Npgsql üzerinden veritabanı bağlantısı kurulacaktır.

Database schema EF Core migrations üzerinden yönetilmelidir.

Production ortamında manuel schema değişikliklerinden kaçınılmalıdır.

Database detayları: `database-design.md`

# 8. Configuration Management

Configuration değerleri environment variables üzerinden yönetilmelidir.

Örnek:

env

```
DATABASE_CONNECTION_STRING=
GOOGLE_BOOKS_API_KEY=

JWT_SECRET=
JWT_ISSUER=
JWT_AUDIENCE=
JWT_ACCESS_TOKEN_MINUTES=60
JWT_REFRESH_TOKEN_DAYS=7

SMTP_HOST=
SMTP_PORT=
SMTP_USERNAME=
SMTP_PASSWORD=
SMTP_FROM_EMAIL=
SMTP_FROM_NAME=Kitaphane

FRONTEND_URL=
CORS_ORIGINS=
```

Kök dizinde `.env.example` (backend + compose) ve `frontend/.env.example` bulunur.

Gerçek secret değerleri source code'a yazılmamalıdır.

Environment dosyaları git'e commit edilmemelidir.

`.gitignore` doğru yapılandırılmalıdır.

# 9. Docker Architecture

Local development için Docker Compose hazırlanmalıdır.

En az aşağıdaki servisler containerize edilebilir:

text

```
docker-compose.yml
    |
    ├── PostgreSQL
    ├── Backend   (dotnet watch, hot reload)
    ├── Frontend  (Vite dev server, /api proxy → Backend)
    └── Mailpit   (SMTP yakalayıcı + web arayüzü)
```

Docker Compose development modunda çalışır; kaynak kodu volume olarak bağlanır.

Development ortamında backend açılışta EF Core migration'larını otomatik uygular.

Frontend, Vite proxy sayesinde backend ile aynı origin üzerinden konuşur; refresh token cookie'si bu sayede sorunsuz çalışır.

Local development deneyimi mümkün olduğunca:

bash

```
docker compose up
```

komutuyla başlatılabilmelidir.

# 10. API Documentation

Backend OpenAPI / Swagger desteğine sahip olmalıdır.

API endpoint'leri ve request/response DTO'ları dokümante edilmelidir.

API tasarımı: `api-design.md`

# 11. Code Quality

Kod aşağıdaki prensiplere uygun olmalıdır:

- Clean code
- Maintainability
- SOLID
- Strong typing
- Async/await
- Separation of concerns
- Minimal abstraction
- Readability

MVP kapsamında overengineering yapılmamalıdır.

Kodun okunabilirliği abstraction sayısından daha önemlidir.

# 12. Development Approach

Geliştirme aşamalı olarak gerçekleştirilmelidir.

1. Project Setup (Docker, EF Core, error handling, Tailwind + shadcn/ui, Penpot tasarım token'ları, i18n)
2. Authentication (e-posta doğrulamalı kayıt, JWT + refresh, kodla şifre sıfırlama, TR/EN)
3. Books (tüm alanlarıyla entity, CRUD, status kuralları, ana sayfa)
4. Google Books
5. Tags
6. Book Details UI
7. Profile and Statistics
8. Reading Goals
9. Final UI Integration

Detaylar: `SRS.md` §7.

Her faz tamamlandığında ilgili işlevler doğrulanmalıdır (backend testleri + Swagger/UI üzerinden manuel kontrol).

Tüm uygulamayı tek seferde karmaşık şekilde geliştirmeye çalışma.

# 13. Documentation

Proje README dosyasında şu bilgiler bulunmalıdır:

- Project overview
- Requirements
- Installation
- Environment variables
- Database setup
- EF Core migrations
- Local development
- Docker usage
- Backend execution
- Frontend execution
- API documentation
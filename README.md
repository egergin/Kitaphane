# Kitaphane

Kitaphane, okuduğunuz, okumakta olduğunuz ve okumak istediğiniz kitapları kişisel olarak takip edebileceğiniz bir web uygulamasıdır.

Ürün ve teknik dokümanlar `docs/` altındadır:

| Doküman | İçerik |
|---|---|
| [docs/requirements/SRS.md](docs/requirements/SRS.md) | Gereksinimler ve geliştirme fazları |
| [docs/architecture/architecture.md](docs/architecture/architecture.md) | Mimari, klasör yapısı, konfigürasyon |
| [docs/authentication/authentication.md](docs/authentication/authentication.md) | Kimlik doğrulama ve yetkilendirme |
| [docs/database/database-design.md](docs/database/database-design.md) | Veritabanı tasarımı |
| [docs/api/api-design.md](docs/api/api-design.md) | REST API tasarımı |
| [docs/frontend/frontend-design.md](docs/frontend/frontend-design.md) | Frontend tasarımı |

## Teknolojiler

- **Backend:** .NET 10, ASP.NET Core Web API, EF Core + Npgsql, ASP.NET Core Identity, FluentValidation, OpenAPI/Swagger
- **Frontend:** React, TypeScript, Vite, Tailwind CSS + shadcn/ui, React Router, TanStack Query, react-hook-form + zod, i18next
- **Veritabanı:** PostgreSQL
- **E-posta:** SMTP (local: Mailpit, production: Resend)
- **Altyapı:** Docker, Docker Compose

## Gereksinimler

- Docker ve Docker Compose (önerilen çalışma şekli)
- Docker dışında çalıştırmak için: .NET 10 SDK, Node.js 24+, PostgreSQL 17
- EF Core migration komutları için: `dotnet tool install --global dotnet-ef`

## Proje Yapısı

```
Kitaphane/
├── backend/            .NET solution (API, Application, Domain, Infrastructure, tests)
├── frontend/           React + Vite uygulaması
├── docs/               Ürün ve teknik dokümanlar
├── docker-compose.yml  Local development ortamı
└── .env.example        Environment variable şablonu
```

## Environment Variables

Kök dizindeki şablonu kopyalayıp değerleri doldurun:

```bash
cp .env.example .env
```

| Değişken | Açıklama |
|---|---|
| `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` | PostgreSQL bilgileri (Docker Compose kullanır) |
| `DATABASE_CONNECTION_STRING` | Backend'i Docker dışında çalıştırırken kullanılan bağlantı cümlesi |
| `GOOGLE_BOOKS_API_KEY` | Google Books API anahtarı. Anahtarsız istekler Google tarafından reddedilebildiği için kitap araması için gereklidir. Google Cloud Console'da "Books API" etkinleştirilip bir API key oluşturulur. |
| `JWT_SECRET` | JWT imzalama anahtarı (en az 32 karakter, ör. `openssl rand -base64 48`) |
| `JWT_ISSUER`, `JWT_AUDIENCE` | JWT issuer / audience |
| `JWT_ACCESS_TOKEN_MINUTES` | Access token süresi (varsayılan 60) |
| `JWT_REFRESH_TOKEN_DAYS` | Refresh token süresi (varsayılan 7) |
| `AUTH_RATE_LIMIT_PER_MINUTE` | `/api/auth` için IP başına dakikalık istek sınırı (varsayılan 20) |
| `SMTP_HOST`, `SMTP_PORT`, `SMTP_USERNAME`, `SMTP_PASSWORD` | SMTP bilgileri |
| `SMTP_FROM_EMAIL`, `SMTP_FROM_NAME` | Gönderen adresi ve adı |
| `FRONTEND_URL` | Frontend adresi |
| `CORS_ORIGINS` | İzin verilen origin'ler (virgülle ayrılmış) |

Frontend için `frontend/.env.example` dosyasına bakın (`VITE_API_BASE_URL`, `API_PROXY_TARGET`).

`.env` dosyaları git'e commit edilmez.

## Docker ile Çalıştırma (önerilen)

```bash
docker compose up --build
```

| Servis | Adres |
|---|---|
| Frontend | http://localhost:5173 |
| API | http://localhost:5000 |
| Swagger UI | http://localhost:5000/swagger |
| Health check | http://localhost:5000/api/health |
| Mailpit (gelen e-postalar) | http://localhost:8025 |
| PostgreSQL | localhost:5432 |

- Backend `dotnet watch`, frontend Vite dev server ile çalışır; kod değişiklikleri otomatik yansır.
- Development ortamında backend açılışta EF Core migration'larını otomatik uygular.
- Frontend `/api` isteklerini Vite proxy üzerinden backend'e iletir.
- Frontend container'ı her açılışta `npm ci` çalıştırır. `package.json` değiştiğinde `docker compose restart frontend` yeterlidir.

Durdurmak için `docker compose down`, veritabanını da silmek için `docker compose down -v`.

## Docker Olmadan Çalıştırma

PostgreSQL ve (isteğe bağlı) Mailpit'i Docker ile başlatabilirsiniz:

```bash
docker compose up -d postgres mailpit
```

### Backend

```bash
cd backend
set -a; source ../.env; set +a   # environment variable'ları yükle
dotnet run --project src/Kitaphane.API
```

API `http://localhost:5000` adresinde çalışır.

### Frontend

```bash
cd frontend
npm install
npm run dev
```

Uygulama `http://localhost:5173` adresinde çalışır.

## Veritabanı ve Migration'lar

Şema EF Core migration'ları ile yönetilir. Migration dosyaları `backend/src/Kitaphane.Infrastructure/Persistence/Migrations` altındadır.

Yeni migration oluşturma (`backend/` dizininde):

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Kitaphane.Infrastructure \
  --startup-project src/Kitaphane.API \
  --output-dir Persistence/Migrations
```

Migration'ları elle uygulama:

```bash
dotnet ef database update --project src/Kitaphane.Infrastructure --startup-project src/Kitaphane.API
```

Development ortamında migration'lar uygulama açılışında otomatik uygulanır. Production ortamında bu komutlar ayrı bir adım olarak çalıştırılmalıdır.

## Testler

```bash
cd backend
dotnet test
```

Entegrasyon testleri Testcontainers ile geçici bir PostgreSQL container'ı başlatır; Docker'ın çalışıyor olması gerekir.

Frontend kontrolleri:

```bash
cd frontend
npm run lint
npm run build
```

## API Dokümantasyonu

Development ortamında:

- OpenAPI dokümanı: http://localhost:5000/openapi/v1.json
- Swagger UI: http://localhost:5000/swagger

Hata formatı ve endpoint'ler: [docs/api/api-design.md](docs/api/api-design.md).

## Production Deploy

Frontend Cloudflare Pages'te, backend Google Cloud Run'da (Frankfurt), veritabanı Neon'da (Frankfurt), e-posta Resend SMTP ile çalışır. Tarayıcı yalnızca Pages adresiyle konuşur; `/api/*` istekleri Pages Function (`frontend/functions/api`) ile Cloud Run'a iletilir.

- `backend/Dockerfile`: Cloud Run production imajı. `Dockerfile.dev` dosyaları yalnızca `docker compose` içindir.
- Adım adım kurulum, secret'lar ve güncelleme: [docs/deployment/deployment.md](docs/deployment/deployment.md).

